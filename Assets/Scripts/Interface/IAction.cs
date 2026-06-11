using UnityEngine;
using WorkerEnum;

public interface IAction
{
    public ActionType ActionType { get; }
    
    void Start(WorkerActionContext context);
    ActionState Tick(WorkerActionContext context);
    void Cancel(WorkerActionContext context);
}
