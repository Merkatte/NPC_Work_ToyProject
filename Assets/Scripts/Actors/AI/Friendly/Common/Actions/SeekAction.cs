using UnityEngine;
using WorkerEnum;

// 정지 경계 액션. 제자리에서 EnemyScanner로 적을 탐지하며,
// 살아있는 IDamageable을 발견하면 holder에 기록하고 Success를 반환한다.
// 이동하지 않는다. 이동하며 탐지하는 순찰은 PatrolAction이 담당한다.
// 공격 명령을 직접 내리지 않는다. selector가 다음 프레임 Attack plan으로 전환한다.
public class SeekAction : IAction
{
    private const int ScanBufferSize = 10;

    private readonly EnemyScanner _scanner = new EnemyScanner(ScanBufferSize);

    private CombatTargetHolder _holder;
    private float _radius;
    private LayerMask _enemyMask;

    public ActionType ActionType => ActionType.Seek;

    public void SetScanParams(CombatTargetHolder holder, float radius, LayerMask enemyMask)
    {
        _holder = holder;
        _radius = radius;
        _enemyMask = enemyMask;
    }

    public void ClearScanParams()
    {
        _holder = null;
        _radius = 0f;
        _enemyMask = default;
    }

    public void Start(WorkerActionContext context)
    {
        // 탐지 시작 시 이전 타깃 초기화.
        // holder가 null이면 Tick에서 Failed로 처리한다.
    }

    public ActionState Tick(WorkerActionContext context)
    {
        if (_holder == null)
        {
            Debug.LogWarning($"{nameof(SeekAction)}: no holder injected. Action will fail.");
            return ActionState.Failed;
        }

        Vector2 pos = context.Transform.position;

        if (_scanner.TryFindNearest(pos, _radius, _enemyMask, out IDamageable nearest, out Transform nearestTransform))
        {
            _holder.SetTarget(nearest, nearestTransform);
            return ActionState.Success;
        }

        return ActionState.Running;
    }

    public void Cancel(WorkerActionContext context)
    {
        // 취소 시 별도 정리 없음. holder는 selector가 관리한다.
    }
}
