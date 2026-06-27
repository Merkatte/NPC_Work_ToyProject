using GameAnimation;
using UnityEngine;
using WorkerEnum;

// 순찰 액션. 앵커 반경 내 무작위 지점으로 이동하고 짧게 대기하는 한 leg를 실행한다.
// 매 tick EnemyScanner로 적을 스캔하며, 발견 시 holder에 기록하고 Success를 반환한다.
// 한 leg(이동→대기) 완료 시에도 Success를 반환해 selector가 재평가하도록 한다.
// 적 발견 vs leg 완료는 selector가 holder.HasLiveTarget으로 구분한다.
public class PatrolAction : IAction
{
    private const int ScanBufferSize = 10;

    private readonly EnemyScanner _scanner = new EnemyScanner(ScanBufferSize);

    private PatrolParams _params;
    private bool _hasParams;
    private bool _failed;

    private bool _idling;
    private float _idleTimer;
    private float _idleTarget;

    public ActionType ActionType => ActionType.Patrol;

    public void SetPatrolParams(PatrolParams patrolParams)
    {
        _params = patrolParams;
        _hasParams = true;
    }

    public void ClearPatrolParams()
    {
        _params = default;
        _hasParams = false;
    }

    public void Start(WorkerActionContext context)
    {
        _failed = false;
        _idling = false;
        _idleTimer = 0f;
        _idleTarget = 0f;

        if (!_hasParams || _params.Holder == null || context?.Mover == null)
        {
            _failed = true;
            return;
        }

        Vector2 offset = Random.insideUnitCircle * _params.PatrolRadius;
        Vector3 wanderPoint = _params.Anchor + new Vector3(offset.x, offset.y, 0f);
        wanderPoint.z = context.Transform.position.z;

        bool flipX = wanderPoint.x < context.Transform.position.x;
        context.Animation?.TryPlay(AnimType.Move, flipX);
        context.Mover.StartMove(wanderPoint);
    }

    public ActionState Tick(WorkerActionContext context)
    {
        if (_failed || !_hasParams || _params.Holder == null || context?.Mover == null)
            return ActionState.Failed;

        // 매 tick 적 스캔. 발견 시 이동 중단 후 즉시 Success로 교전 전환.
        if (_scanner.TryFindNearest(
                context.Transform.position, _params.ScanRadius, _params.EnemyMask,
                out IDamageable found, out Transform foundTransform))
        {
            _params.Holder.SetTarget(found, foundTransform);
            context.Mover.Stop();
            context.Animation?.Stop(AnimType.Move);
            return ActionState.Success;
        }

        if (_idling)
        {
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= _idleTarget)
                return ActionState.Success;  // 한 leg 완료, selector 재평가

            return ActionState.Running;
        }

        ActionState moveState = context.Mover.TickMove();
        if (moveState == ActionState.Running)
            return ActionState.Running;

        context.Animation?.Stop(AnimType.Move);

        if (moveState == ActionState.Failed)
            return ActionState.Failed;

        // 이동 완료(Success) 시에만 idle 단계로 전환.
        _idling = true;
        _idleTimer = 0f;
        _idleTarget = Random.Range(_params.IdleMin, _params.IdleMax);

        return ActionState.Running;
    }

    public void Cancel(WorkerActionContext context)
    {
        context?.Mover?.Stop();
        context?.Animation?.Stop(AnimType.Move);
        ClearPatrolParams();
    }
}
