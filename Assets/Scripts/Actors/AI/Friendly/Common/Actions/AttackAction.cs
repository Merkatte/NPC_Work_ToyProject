using UnityEngine;
using WorkerEnum;

public class AttackAction : IAction
{
    private readonly WorkerActionResultStatEntry resultStatEntry;
    private IDamageable target;
    private IAttackPower attackPower;
    private float timer;
    private bool failed;

    public ActionType ActionType => ActionType.Attack;

    public AttackAction(WorkerActionResultStatEntry resultStatEntry)
    {
        this.resultStatEntry = resultStatEntry;
    }

    public void SetTarget(IDamageable target)
    {
        this.target = target;
    }

    public void ClearTarget()
    {
        target = null;
    }

    public void SetAttackPower(IAttackPower attackPower)
    {
        this.attackPower = attackPower;
    }

    public void ClearAttackPower()
    {
        attackPower = null;
    }

    public void Start(WorkerActionContext context)
    {
        failed = false;
        timer = resultStatEntry.Duration;

        if (target == null)
        {
            Debug.LogWarning($"{nameof(AttackAction)}: no target injected. Action will fail.");
            failed = true;
            return;
        }

        if (attackPower == null)
        {
            Debug.LogWarning($"{nameof(AttackAction)}: no attack power injected. Action will fail.");
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

        int damage = attackPower.GetAttackPower();
        target.TakeDamage(damage);
        context.Stats.Apply(resultStatEntry.StatDelta);
        return ActionState.Success;
    }

    public void Cancel(WorkerActionContext context)
    {
    }
}
