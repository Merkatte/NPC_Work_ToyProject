using UnityEngine;
using WorkerEnum;

// 가드 행동 결정 셀렉터.
// 우선순위: ① 전투(살아있는 타깃) → ② 위급 욕구 → ③ 예비 욕구 → ④ 순찰(이동+상시 탐지).
// 순찰에만 무작위성을 부여해 생동감을 만든다. 전투와 욕구는 확정적 우선순위.
// 탐지 결과는 CombatTargetHolder(소유)를 통해 PatrolAction/SeekAction → selector → Attack으로 전달한다.
public class GuardActionSelector : MonoBehaviour, IActionSelector<WorkerActionContext, WorkerActionPlan>, IWorkerActionSelectorSetup
{
    private const float CriticalThreshold = 70f;
    private const float PrepareThreshold = 60f;

    [SerializeField] private WorkerActionSet _actionSet;
    [SerializeField] private DestinationProvider _destinationProvider;

    [Header("Combat")]
    [SerializeField] private float _scanRadius = 6f;
    [SerializeField] private LayerMask _enemyMask;
    [SerializeField] private float _engageRange = 1.2f;

    [Header("Patrol")]
    [SerializeField] private Transform _patrolAnchor;
    [SerializeField] private float _patrolRadius = 2f;
    [SerializeField] private float _patrolIdleMin = 0.5f;
    [SerializeField] private float _patrolIdleMax = 1.5f;

    private readonly CombatTargetHolder _targetHolder = new CombatTargetHolder();
    private IAttackPower _attackPower;

    private bool _anchorCaptured;
    private Vector3 _capturedAnchor;

    private void Awake()
    {
        if (!_actionSet)
            TryGetComponent(out _actionSet);

        if (!_destinationProvider)
            TryGetComponent(out _destinationProvider);

        // 가드 자신의 IAttackPower는 루트 또는 부모에서 해석한다.
        _attackPower = GetComponentInParent<IAttackPower>();
        if (_attackPower == null)
            Debug.LogWarning($"{nameof(GuardActionSelector)}: no {nameof(IAttackPower)} found on guard.", this);
    }

    public void Init(WorkerActionSet actionSet)
    {
        _actionSet = actionSet;

        // Init 후 다시 시도(매니저가 init 전 Awake를 못 잡는 경우 대비).
        if (_attackPower == null)
            _attackPower = GetComponentInParent<IAttackPower>();
    }

    public void Init(DestinationProvider destinationProvider)
    {
        _destinationProvider = destinationProvider;
    }

    public bool TrySelectAction(WorkerActionContext context, out WorkerActionPlan plan)
    {
        plan = null;

        if (!_actionSet)
            return false;

        // ① 살아있는 전투 타깃이 있으면 공격 흐름으로.
        if (_targetHolder.HasLiveTarget)
            return TryCreateAttackPlan(context, out plan);

        // ② 위급 욕구 처리.
        if (WorkerNeedsPolicy.TryGetNeededActionType(context.Stats, CriticalThreshold, out ActionType criticalAction))
            return TryCreateNeedsRecoveryPlan(context, criticalAction, out plan);

        // ③ 예비 욕구 처리.
        if (WorkerNeedsPolicy.TryGetNeededActionType(context.Stats, PrepareThreshold, out ActionType prepareAction))
            return TryCreateNeedsRecoveryPlan(context, prepareAction, out plan);

        // ④ 순찰: 앵커 반경 내 무작위 이동 + 상시 탐지.
        return TryCreatePatrolPlan(context, out plan);
    }

    public bool ReturnAction(IAction action)
    {
        if (!_actionSet)
            return false;

        return _actionSet.ReturnAction(action);
    }

    private bool TryCreateAttackPlan(WorkerActionContext context, out WorkerActionPlan plan)
    {
        plan = null;

        if (_attackPower == null)
            return false;

        IDamageable target = _targetHolder.Target;
        Transform targetTransform = _targetHolder.TargetTransform;

        if (!_actionSet.TryGetAction(ActionType.Attack, target, _attackPower, out IAction attackAction))
            return false;

        Vector3 current = context.Transform.position;
        Vector3 targetPos = targetTransform.position;
        targetPos.z = current.z;

        if (Vector3.Distance(current, targetPos) > _engageRange)
        {
            if (!_actionSet.TryGetAction(ActionType.Move, targetPos, out IAction moveAction))
            {
                _actionSet.ReturnAction(attackAction);
                return false;
            }

            plan = WorkerActionPlan.Create(moveAction, attackAction);
            return true;
        }

        plan = WorkerActionPlan.Create(attackAction);
        return true;
    }

    private bool TryCreateNeedsRecoveryPlan(WorkerActionContext context, ActionType actionType, out WorkerActionPlan plan)
    {
        plan = null;

        if (!_actionSet.TryGetAction(actionType, out IAction action))
            return false;

        if (TryGetMoveDestination(context, actionType, out Vector3 destination))
        {
            if (!_actionSet.TryGetAction(ActionType.Move, destination, out IAction moveAction))
            {
                _actionSet.ReturnAction(action);
                return false;
            }

            plan = WorkerActionPlan.Create(moveAction, action);
            return true;
        }

        plan = WorkerActionPlan.Create(action);
        return true;
    }

    private bool TryGetMoveDestination(WorkerActionContext context, ActionType actionType, out Vector3 destination)
    {
        destination = default;

        if (!_destinationProvider)
            return false;

        if (!_destinationProvider.TryGetDestinationPosition(actionType, out destination))
            return false;

        Vector3 current = context.Transform.position;
        destination.z = current.z;

        return Vector3.Distance(current, destination) > context.Mover.StoppingDistance;
    }

    private bool TryCreatePatrolPlan(WorkerActionContext context, out WorkerActionPlan plan)
    {
        plan = null;

        PatrolParams patrolParams = new PatrolParams
        {
            Holder = _targetHolder,
            Anchor = GetPatrolAnchor(context),
            PatrolRadius = _patrolRadius,
            ScanRadius = _scanRadius,
            EnemyMask = _enemyMask,
            IdleMin = _patrolIdleMin,
            IdleMax = _patrolIdleMax,
        };

        if (!_actionSet.TryGetAction(ActionType.Patrol, patrolParams, out IAction patrolAction))
            return false;

        plan = WorkerActionPlan.Create(patrolAction);
        return true;
    }

    // _patrolAnchor가 지정되어 있으면 그 위치를, 없으면 첫 선택 시 가드 위치를 1회 캡처.
    private Vector3 GetPatrolAnchor(WorkerActionContext context)
    {
        if (_patrolAnchor != null)
            return _patrolAnchor.position;

        if (!_anchorCaptured)
        {
            _capturedAnchor = context.Transform.position;
            _anchorCaptured = true;
        }

        return _capturedAnchor;
    }
}
