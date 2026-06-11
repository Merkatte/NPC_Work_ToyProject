using UnityEngine;
using WorkerEnum;

public class DrinkAction : IAction
{
    private float timer;

    public ActionType ActionType => ActionType.Drink;

    public void Start(WorkerActionContext context)
    {
        timer = 1.5f;
    }

    public ActionState Tick(WorkerActionContext context)
    {
        timer -= Time.deltaTime;

        if (timer > 0f)
            return ActionState.Running;

        context.Stats.Drink(50f);
        return ActionState.Success;
    }

    public void Cancel(WorkerActionContext context)
    {
    }
}
