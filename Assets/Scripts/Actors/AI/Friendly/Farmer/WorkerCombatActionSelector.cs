using UnityEngine;

public class WorkerCombatActionSelector : MonoBehaviour, IActionSelector<WorkerActionContext, WorkerActionPlan>, IWorkerActionSelectorSetup
{
    [SerializeField] private WorkerActionSet _actionSet;
    [SerializeField] private Transform _target;
    [SerializeField] private float _engageRange = 1f;

    private void Awake()
    {
        if (!_actionSet)
            TryGetComponent(out _actionSet);
    }

    public void Init(WorkerActionSet actionSet)
    {
        _actionSet = actionSet;
    }

    public void Init(Transform target)
    {
        _target = target;
    }

    public void ClearTarget()
    {
        _target = null;
    }

    public bool TrySelectAction(WorkerActionContext context, out WorkerActionPlan plan)
    {
        plan = null;

        if (!_actionSet || !_target)
            return false;

        Vector3 current = context.Transform.position;
        Vector3 targetPosition = _target.position;
        targetPosition.z = current.z;

        if (Vector3.Distance(current, targetPosition) <= _engageRange)
            return false;

        if (!_actionSet.TryGetAction(WorkerEnum.ActionType.Move, targetPosition, out IAction moveAction))
            return false;

        plan = WorkerActionPlan.Create(moveAction);
        return true;
    }

    public bool ReturnAction(IAction action)
    {
        if (!_actionSet)
            return false;

        return _actionSet.ReturnAction(action);
    }
}
