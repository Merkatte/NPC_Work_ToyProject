using UnityEngine;
using WorkerEnum;

// 가드 행동 결정 셀렉터.
// ① 살아있는 타깃이 있으면 사거리 내 Attack, 밖이면 Move→Attack.
// ② 타깃이 없으면 SeekAction으로 Physics 탐지.
// 탐지 결과는 CombatTargetHolder(소유)를 통해 Seek→selector→Attack으로 전달한다.
public class GuardActionSelector : MonoBehaviour, IActionSelector<WorkerActionContext, WorkerActionPlan>, IWorkerActionSelectorSetup
{
    [SerializeField] private WorkerActionSet _actionSet;
    [SerializeField] private float _scanRadius = 6f;
    [SerializeField] private LayerMask _enemyMask;
    [SerializeField] private float _engageRange = 1.2f;

    private readonly CombatTargetHolder _targetHolder = new CombatTargetHolder();
    private IAttackPower _attackPower;

    private void Awake()
    {
        if (!_actionSet)
            TryGetComponent(out _actionSet);

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

    public bool TrySelectAction(WorkerActionContext context, out WorkerActionPlan plan)
    {
        plan = null;

        if (!_actionSet)
            return false;

        // 살아있는 타깃이 있으면 공격 흐름으로.
        if (_targetHolder.HasLiveTarget)
            return TryCreateAttackPlan(context, out plan);

        // 타깃이 없으면 탐지.
        return TryCreateSeekPlan(out plan);
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

    private bool TryCreateSeekPlan(out WorkerActionPlan plan)
    {
        plan = null;

        if (!_actionSet.TryGetAction(ActionType.Seek, _targetHolder, _scanRadius, _enemyMask, out IAction seekAction))
            return false;

        plan = WorkerActionPlan.Create(seekAction);
        return true;
    }
}
