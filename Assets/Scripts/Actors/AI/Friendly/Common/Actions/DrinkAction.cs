 
using UnityEngine;
using WorkerEnum;

public class DrinkAction : IAction
{
    private readonly WorkerActionResultStatEntry resultStatEntry;
    private float timer;

    public ActionType ActionType => ActionType.Drink;

    public DrinkAction(WorkerActionResultStatEntry resultStatEntry)
    {
        this.resultStatEntry = resultStatEntry;
    }

    public void Start(WorkerActionContext context)
    {
        timer = resultStatEntry.Duration;
    }

    public ActionState Tick(WorkerActionContext context)
    {
        timer -= Time.deltaTime;

        if (timer > 0f)
            return ActionState.Running;

        context.Stats.Apply(resultStatEntry.StatDelta);
        return ActionState.Success;
    }

    public void Cancel(WorkerActionContext context)
    {
    }
}
