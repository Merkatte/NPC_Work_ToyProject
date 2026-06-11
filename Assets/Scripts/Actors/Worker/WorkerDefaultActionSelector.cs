using UnityEngine;
using WorkerEnum;

[RequireComponent(typeof(WorkerActionSet))]
public class WorkerDefaultActionSelector : MonoBehaviour, IActionSelector<WorkerActionContext, WorkerActionPlan>
{
    [SerializeField] private WorkerActionSet actionSet;

    private void Awake()
    {
        if (actionSet == null)
            TryGetComponent(out actionSet);
    }

    public bool TrySelectAction(WorkerActionContext context, out WorkerActionPlan plan)
    {
        plan = null;

        if (!actionSet)
            return false;

        if (context.Stats.Thirst >= 70f)
            return TryCreatePlan(context, ActionType.Drink, out plan);

        if (context.Stats.Hunger >= 70f)
            return TryCreatePlan(context, ActionType.Eat, out plan);

        if (context.Stats.Fatigue >= 70f)
            return TryCreatePlan(context, ActionType.Rest, out plan);

        return TryCreatePlan(context, ActionType.Work, out plan);
    }

    private bool TryCreatePlan(
        WorkerActionContext context,
        ActionType actionType,
        out WorkerActionPlan plan)
    {
        plan = null;

        if (!actionSet.TryGetAction(actionType, out IAction action))
            return false;

        if (TryGetMoveDestination(context, actionType, out Vector3 destination))
        {
            if (!actionSet.TryGetAction(ActionType.Move, destination, out IAction moveAction))
            {
                actionSet.ReturnAction(action);
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

        if (context.DestinationProvider == null)
            return false;

        if (!context.DestinationProvider.TryGetDestinationPosition(actionType, out destination))
            return false;

        Vector3 current = context.Transform.position;
        destination.z = current.z;

        return Vector3.Distance(current, destination) > context.Mover.StoppingDistance;
    }
}
