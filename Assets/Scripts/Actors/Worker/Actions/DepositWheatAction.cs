using UnityEngine;
using WorkerEnum;

public class DepositWheatAction : IAction
{
    private readonly WorkerActionResultStatEntry resultStatEntry;
    private float timer;

    public ActionType ActionType => ActionType.DepositWheat;

    public DepositWheatAction(WorkerActionResultStatEntry resultStatEntry)
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
        context.CarryStorage.DepositAllWheat();
        return ActionState.Success;
    }

    public void Cancel(WorkerActionContext context)
    {
    }
}
