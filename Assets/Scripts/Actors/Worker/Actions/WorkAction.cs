using UnityEngine;
using WorkerEnum;

public class WorkAction : IAction
{
    private float timer;

    public ActionType ActionType => ActionType.Work;

    public void Start(WorkerActionContext context)
    {
        timer = 2f;
    }

    public ActionState Tick(WorkerActionContext context)
    {
        timer -= Time.deltaTime;

        if (timer > 0f)
            return ActionState.Running;

        context.Stats.Work(10f, 10f, 20f);
        return ActionState.Success;
    }

    public void Cancel(WorkerActionContext context)
    {
    }
}
