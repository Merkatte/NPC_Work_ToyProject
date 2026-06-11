using System;
using Unity.Behavior;
using Unity.Properties;
using WorkerEnum;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Run Worker Current Action",
    story: "Run worker current action",
    category: "Action/Worker",
    id: "6e86f17af0244d9fad115e93da4ba6b8")]
public partial class RunWorkerCurrentActionNode : Action
{
    protected override Status OnUpdate()
    {
        if (GameObject.TryGetComponent<WorkerAI>(out var workerAI))
        {
            ActionState state = workerAI.TickCurrentAction();

            return state switch
            {
                ActionState.Running => Status.Running,
                ActionState.Success => Status.Success,
                _ => Status.Failure
            };
        }

        return Status.Failure;
    }
}
