using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using WorkerEnum;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Move To Plan Destination",
    story: "Move worker to [Destination]",
    category: "Action/Worker",
    id: "a08d5d926d7b45d6a92801be189d67ef")]
public partial class MoveToPlanDestinationNode : Action
{
    [SerializeReference] public BlackboardVariable<Vector3> Destination;
    private WorkerAI workerAI;
    private WorkerActionSet actionSet;
    private IAction moveAction;
    private WorkerActionPlan plan;

    protected override Status OnStart()
    {
        if (!GameObject.TryGetComponent(out workerAI))
            return Status.Failure;

        if (!GameObject.TryGetComponent(out actionSet))
            return Status.Failure;

        if (!actionSet.TryGetAction(ActionType.Move, Destination.Value, out moveAction))
            return Status.Failure;

        plan = WorkerActionPlan.Create(moveAction);
        workerAI.Context.SetPlan(plan);
        moveAction.Start(workerAI.Context);

        return ToStatus(moveAction.Tick(workerAI.Context));
    }

    protected override Status OnUpdate()
    {
        if (moveAction == null)
            return Status.Failure;

        if (workerAI == null && !GameObject.TryGetComponent(out workerAI))
            return Status.Failure;

        return ToStatus(moveAction.Tick(workerAI.Context));
    }

    protected override void OnEnd()
    {
        moveAction?.Cancel(workerAI?.Context);
        workerAI?.Context?.ClearPlan(plan);
        ReturnPlanActions();
        moveAction = null;
        plan = null;
    }

    private Status ToStatus(ActionState state) => state switch
    {
        ActionState.Running => Status.Running,
        ActionState.Success => Status.Success,
        _ => Status.Failure
    };

    private void ReturnPlanActions()
    {
        if (!actionSet || plan == null)
            return;

        for (int i = 0; i < plan.RentedActions.Count; i++)
            actionSet.ReturnAction(plan.RentedActions[i]);
    }
}
