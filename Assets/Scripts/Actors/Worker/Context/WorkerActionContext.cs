using UnityEngine;

public sealed class WorkerActionContext
{
    //public WorkerAI Owner { get; }
    public Transform Transform { get; }
    public WorkerMover Mover { get; }
    public WorkerStats Stats { get; }
    public WorkerMovementStats MovementStats { get; }
    public WorkerCarryStorage CarryStorage { get; }
    public WorkerActionPlan Plan { get; private set; }

    public WorkerActionContext(
        Transform transform,
        WorkerMover mover,
        WorkerStats stats,
        WorkerMovementStats movementStats,
        WorkerCarryStorage carryStorage)
    {
        //Owner = owner;
        Transform = transform;
        Mover = mover;
        Stats = stats;
        MovementStats = movementStats;
        CarryStorage = carryStorage;
    }

    public void SetPlan(WorkerActionPlan plan)
    {
        Plan = plan;
    }

    public void ClearPlan(WorkerActionPlan plan)
    {
        if (Plan == plan)
            Plan = null;
    }
}
