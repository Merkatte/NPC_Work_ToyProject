using UnityEngine;
using WorkerEnum;

public class WorkerMover
{
    private readonly Transform target;
    private readonly WorkerMovementStats movementStats;
    private Vector3 targetPosition;
    private bool hasDestination;

    public float StoppingDistance => movementStats.StoppingDistance;
    
    public WorkerMover(Transform target, WorkerMovementStats movementStats)
    {
        this.target = target;
        this.movementStats = movementStats;
    }

    public void StartMove(Vector3 targetPosition)
    {
        this.targetPosition = targetPosition;
        hasDestination = true;
    }

    public ActionState TickMove()
    {
        if (target == null || movementStats == null || !hasDestination || movementStats.MoveSpeed <= 0f)
            return ActionState.Failed;

        Vector3 currentPosition = target.position;
        targetPosition.z = currentPosition.z;

        if (IsAtDestination(currentPosition))
        {
            Stop();
            return ActionState.Success;
        }

        target.position = Vector3.MoveTowards(
            currentPosition,
            targetPosition,
            movementStats.MoveSpeed * Time.deltaTime);

        if (!IsAtDestination(target.position))
            return ActionState.Running;

        Stop();
        return ActionState.Success;
    }

    public void Stop()
    {
        hasDestination = false;
    }

    public void MoveTo(Vector3 targetPos, Vector3 velocity)
    {
        if (target == null)
            return;

        targetPosition = targetPos;
        targetPosition.z = target.position.z;

        Vector3 nextPosition = target.position + velocity * Time.deltaTime;
        target.position = Vector3.Distance(nextPosition, targetPosition) <= velocity.magnitude * Time.deltaTime
            ? targetPosition
            : nextPosition;
    }

    private bool IsAtDestination(Vector3 currentPosition)
    {
        return Vector3.Distance(currentPosition, targetPosition) <= movementStats.StoppingDistance;
    }
}
