using UnityEngine;
using WorkerEnum;

public class RestAction : IAction
{
    private float _timer;
    
    public ActionType ActionType => ActionType.Rest;
    public void Start(WorkerActionContext context)
    {
        _timer = 1.5f;
    }

    public ActionState Tick(WorkerActionContext context)
    {
        _timer -= Time.deltaTime;
        
        if (_timer > 0f)
            return ActionState.Running;

        context.Stats.Rest(50f);
        return ActionState.Success;
    }

    public void Cancel(WorkerActionContext context)
    {
    }
}
