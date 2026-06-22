using UnityEngine;
using WorkerEnum;

public class DepositWheatAction : IAction
{
    private readonly WorkerActionResultStatEntry resultStatEntry;
    private IInventory targetInventory;
    private float timer;
    private bool failed;

    public ActionType ActionType => ActionType.DepositWheat;

    public DepositWheatAction(WorkerActionResultStatEntry resultStatEntry)
    {
        this.resultStatEntry = resultStatEntry;
    }

    public void SetTargetInventory(IInventory inventory)
    {
        targetInventory = inventory;
    }

    public void ClearTargetInventory()
    {
        targetInventory = null;
    }

    public void Start(WorkerActionContext context)
    {
        failed = false;
        timer = resultStatEntry.Duration;

        if (targetInventory == null)
        {
            Debug.LogWarning($"{nameof(DepositWheatAction)}: no target inventory injected. Action will fail.");
            failed = true;
        }
    }

    public ActionState Tick(WorkerActionContext context)
    {
        if (failed)
            return ActionState.Failed;

        timer -= Time.deltaTime;

        if (timer > 0f)
            return ActionState.Running;

        int carried = context.CarryStorage.CurrentWheat;
        targetInventory.TryAdd(ItemType.Wheat, carried, out int accepted);

        if (accepted <= 0)
            return ActionState.Failed;

        context.CarryStorage.RemoveWheat(accepted);
        context.Stats.Apply(resultStatEntry.StatDelta);
        return ActionState.Success;
    }

    public void Cancel(WorkerActionContext context)
    {
    }
}
