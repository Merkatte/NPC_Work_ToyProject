using UnityEngine;

public sealed class CookActionSelector : MonoBehaviour, IActionSelector<WorkerActionContext, WorkerActionPlan>
{
    public bool TrySelectAction(WorkerActionContext context, out WorkerActionPlan plan)
    {
        plan = null;
        return false;
    }

    public bool ReturnAction(IAction action)
    {
        return false;
    }
}
