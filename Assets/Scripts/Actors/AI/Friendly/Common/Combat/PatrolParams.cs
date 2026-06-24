using UnityEngine;

// PatrolAction에 주입하는 순찰 파라미터 묶음.
// 7개 파라미터를 struct로 묶어 WorkerActionSet 오버로드를 단순화한다.
public struct PatrolParams
{
    public CombatTargetHolder Holder;
    public Vector3 Anchor;
    public float PatrolRadius;
    public float ScanRadius;
    public LayerMask EnemyMask;
    public float IdleMin;
    public float IdleMax;
}
