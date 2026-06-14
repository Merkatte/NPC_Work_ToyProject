using UnityEngine;
using UnityEngine.Serialization;
using WorkerEnum;

public class WorkerDefaultActionSelector : MonoBehaviour, IActionSelector<WorkerActionContext, WorkerActionPlan>, IWorkerActionSelectorSetup
{
    private const float CriticalThreshold = 70f;
    private const float PrepareThreshold = 60f;

    [FormerlySerializedAs("actionSet")]
    [SerializeField] private WorkerActionSet _actionSet;

    [FormerlySerializedAs("destinationProvider")]
    [SerializeField] private DestinationProvider _destinationProvider;

    private void Awake()
    {
        if (!_actionSet)
            TryGetComponent(out _actionSet);

        if (!_destinationProvider)
            TryGetComponent(out _destinationProvider);
    }

    public void Init(WorkerActionSet actionSet)
    {
        _actionSet = actionSet;
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

        if (TryGetNeededActionType(context, CriticalThreshold, out ActionType actionType))
            return TryCreatePlan(context, actionType, out plan);

        if (TryGetNeededActionType(context, PrepareThreshold, out actionType))
            return TryCreatePlan(context, actionType, out plan);

        return TryCreatePlan(context, ActionType.Work, out plan);
    }

    private bool TryGetNeededActionType(WorkerActionContext context, float threshold, out ActionType actionType)
    {
        if (context.Stats.Thirst >= threshold)
        {
            actionType = ActionType.Drink;
            return true;
        }

        if (context.Stats.Hunger >= threshold)
        {
            actionType = ActionType.Eat;
            return true;
        }

        if (context.Stats.Fatigue >= threshold)
        {
            actionType = ActionType.Rest;
            return true;
        }

        actionType = default;
        return false;
    }

    public bool ReturnAction(IAction action)
    {
        if (!_actionSet)
            return false;

        return _actionSet.ReturnAction(action);
    }

    private bool TryCreatePlan(
        WorkerActionContext context,
        ActionType actionType,
        out WorkerActionPlan plan)
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
}
