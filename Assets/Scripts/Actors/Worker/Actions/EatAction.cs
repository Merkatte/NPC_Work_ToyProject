using UnityEngine;
using WorkerEnum;

public class EatAction: IAction
{
    public ActionType ActionType => ActionType.Eat;

    private float _timer = 2f;
    public void Start(WorkerActionContext context)
    {
        _timer = 2f;
    }

    public ActionState Tick(WorkerActionContext context)
    {
        _timer -= Time.deltaTime;
        
        if (_timer > 0f)
            return ActionState.Running;
        
        context.Stats.Eat(50f);
        return ActionState.Success;
    }

    public void Cancel(WorkerActionContext context)
    {
        
    }
}
