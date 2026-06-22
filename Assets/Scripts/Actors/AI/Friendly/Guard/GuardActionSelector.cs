using UnityEngine;

public class GuardActionSelector : MonoBehaviour, IActionSelector<WorkerActionContext, WorkerActionPlan>
{
    [SerializeField] private WallPerimeter _wallPerimeter;

    private void Awake()
    {
        if (!_wallPerimeter)
            TryGetComponent(out _wallPerimeter);
    }

    public void Init(WallPerimeter wallPerimeter)
    {
        _wallPerimeter = wallPerimeter;
    }

    public bool TrySelectAction(WorkerActionContext context, out WorkerActionPlan plan)
    {
        plan = null;

        if (!_wallPerimeter)
            return false;

        return false;
    }

    public bool ReturnAction(IAction action)
    {
        return false;
    }
}
