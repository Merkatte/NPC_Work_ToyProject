using GameAnimation;
using UnityEngine;
using WorkerEnum;

public class WorkAction : IAction
{
    private readonly WorkerActionResultStatEntry resultStatEntry;
    private float timer;

    public ActionType ActionType => ActionType.Work;

    public WorkAction(WorkerActionResultStatEntry resultStatEntry)
    {
        this.resultStatEntry = resultStatEntry;
    }

    public void Start(WorkerActionContext context)
    {
        timer = resultStatEntry.Duration;
        context?.Animation?.TryPlay(AnimType.Work);
    }

    public ActionState Tick(WorkerActionContext context)
    {
        timer -= Time.deltaTime;

        if (timer > 0f)
            return ActionState.Running;

        context.Stats.Apply(resultStatEntry.StatDelta);
        context.CarryStorage.AddWheat(resultStatEntry.WheatDelta);
        context.Animation?.Stop(AnimType.Work);
        return ActionState.Success;
    }

    public void Cancel(WorkerActionContext context)
    {
        context?.Animation?.Stop(AnimType.Work);
    }
}
