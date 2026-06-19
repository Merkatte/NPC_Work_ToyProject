using UnityEngine;
using WorkerEnum;

public class MoveAction: IAction
{
    private Vector3 destination;
    private bool failed;
    private bool hasDestination;

    public ActionType ActionType => ActionType.Move;

    public void SetDestination(Vector3 destination)
    {
        this.destination = destination;
        hasDestination = true;
    }

    public void ClearDestination()
    {
        destination = default;
        hasDestination = false;
    }

    public void Start(WorkerActionContext context)
    {
        failed = false;
        Debug.Log("Worker Moving Start");
        if (context?.Mover == null || !TryGetDestination(context, out Vector3 destination))
        {
            failed = true;
            return;
        }

        context.Mover.StartMove(destination);
    }

    public ActionState Tick(WorkerActionContext context)
    {
        if (failed || context?.Mover == null)
            return ActionState.Failed;

        return context.Mover.TickMove();
    }

    public void Cancel(WorkerActionContext context)
    {
        context?.Mover?.Stop();
        ClearDestination();
    }

    private bool TryGetDestination(WorkerActionContext context, out Vector3 destination)
    {
        if (hasDestination)
        {
            destination = this.destination;
            return true;
        }

        destination = default;
        return false;
    }
}
