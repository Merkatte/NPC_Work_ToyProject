using System.Collections.Generic;
public sealed class WorkerActionPlan
{
    private readonly Queue<IAction> _actions = new Queue<IAction>();
    private readonly List<IAction> _rentedActions = new List<IAction>();

    public IAction CurrentAction { get; private set; }
    public IReadOnlyList<IAction> RentedActions => _rentedActions;

    private WorkerActionPlan(params IAction[] actions)
    {
        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i] != null)
            {
                _actions.Enqueue(actions[i]);
                _rentedActions.Add(actions[i]);
            }
        }

        TryMoveNextAction();
    }

    public static WorkerActionPlan Create(params IAction[] actions)
    {
        return new WorkerActionPlan(actions);
    }

    public void RemoveRentedAction(IAction action)
    {
        _rentedActions.Remove(action);
    }

    public bool TryMoveNextAction()
    {
        if (_actions.Count <= 0)
        {
            CurrentAction = null;
            return false;
        }

        CurrentAction = _actions.Dequeue();
        return true;
    }
}
