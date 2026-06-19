using System.Collections.Generic;
using UnityEngine;
using WorkerEnum;

public class WorkerActionSet : MonoBehaviour, IActionSet<ActionType>
{
    [SerializeField] private int _initialPoolSize = 1;
    [SerializeField] private WorkerActionResultStatData _resultStatData;

    private readonly Dictionary<ActionType, Queue<IAction>> _availableActions = new Dictionary<ActionType, Queue<IAction>>();
    private readonly Dictionary<IAction, ActionType> _rentedActionTypes = new Dictionary<IAction, ActionType>();

    private void Awake()
    {
        RegisterPool(ActionType.Rest);
        RegisterPool(ActionType.Eat);
        RegisterPool(ActionType.Drink);
        RegisterPool(ActionType.Work);
        RegisterPool(ActionType.Move);
        RegisterPool(ActionType.DepositWheat);
    }

    public bool TryGetAction(ActionType actionType, out IAction action)
    {
        return TryRentAction(actionType, out action);
    }

    public bool TryGetResultStatEntry(ActionType actionType, out WorkerActionResultStatEntry entry)
    {
        if (!_resultStatData)
        {
            entry = default;
            return false;
        }

        return _resultStatData.TryGetEntry(actionType, out entry);
    }

    public bool TryGetAction(ActionType actionType, Vector3 destination, out IAction action)
    {
        return TryRentAction(actionType, destination, out action);
    }

    public bool TryRentAction(ActionType actionType, out IAction action)
    {
        action = null;

        if (!_availableActions.TryGetValue(actionType, out Queue<IAction> actions))
            return false;

        if (actions.Count <= 0 && TryCreateAction(actionType, out IAction createdAction))
            actions.Enqueue(createdAction);

        if (actions.Count <= 0)
            return false;

        action = actions.Dequeue();
        _rentedActionTypes[action] = actionType;
        return true;
    }

    public bool TryRentAction(ActionType actionType, Vector3 destination, out IAction action)
    {
        if (!TryRentAction(actionType, out action))
            return false;

        if (action is MoveAction moveAction)
        {
            moveAction.SetDestination(destination);
            return true;
        }

        ReturnAction(action);
        action = null;
        return false;
    }

    public bool ReturnAction(IAction action)
    {
        if (action == null)
            return false;

        if (!_rentedActionTypes.TryGetValue(action, out ActionType actionType))
            return false;

        _rentedActionTypes.Remove(action);
        ResetAction(action);
        _availableActions[actionType].Enqueue(action);
        return true;
    }

    private void RegisterPool(ActionType actionType)
    {
        if (!_availableActions.ContainsKey(actionType))
            _availableActions[actionType] = new Queue<IAction>();

        int poolSize = Mathf.Max(1, _initialPoolSize);
        for (int i = 0; i < poolSize; i++)
        {
            if (TryCreateAction(actionType, out IAction action))
                _availableActions[actionType].Enqueue(action);
        }
    }

    private bool TryCreateAction(ActionType actionType, out IAction action)
    {
        action = actionType switch
        {
            ActionType.Rest => TryGetResultStatEntry(actionType, out WorkerActionResultStatEntry restEntry)
                ? new RestAction(restEntry)
                : null,
            ActionType.Eat => TryGetResultStatEntry(actionType, out WorkerActionResultStatEntry eatEntry)
                ? new EatAction(eatEntry)
                : null,
            ActionType.Drink => TryGetResultStatEntry(actionType, out WorkerActionResultStatEntry drinkEntry)
                ? new DrinkAction(drinkEntry)
                : null,
            ActionType.Work => TryGetResultStatEntry(actionType, out WorkerActionResultStatEntry workEntry)
                ? new WorkAction(workEntry)
                : null,
            ActionType.DepositWheat => TryGetResultStatEntry(actionType, out WorkerActionResultStatEntry depositWheatEntry)
                ? new DepositWheatAction(depositWheatEntry)
                : null,
            ActionType.Move => new MoveAction(),
            _ => null
        };

        return action != null;
    }

    private void ResetAction(IAction action)
    {
        if (action is MoveAction moveAction)
            moveAction.ClearDestination();
    }
}
