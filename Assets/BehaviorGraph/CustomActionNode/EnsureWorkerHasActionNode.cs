using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Ensure Worker Has Action",
    story : "Ensure worker has an action",
    category : "Action/Worker",
    id: "51854272815842f5a9f71dd81f39bb38"
)]
public partial class EnsureWorkerHasActionNode: Action
{
    protected override Status OnStart()
    {
        if (GameObject.TryGetComponent<WorkerAI>(out var workerAI))
        {
            if (workerAI.HasCurrentAction)
                return Status.Success;

            IActionSelector<WorkerActionContext, WorkerActionPlan> selector = GetActionSelector();
            if (selector == null)
                return Status.Failure;

            if (!selector.TrySelectAction(workerAI.Context, out WorkerActionPlan plan))
                return Status.Failure;

            workerAI.SetPlan(plan);
            return Status.Success;
        }

        return Status.Failure;
    }

    private IActionSelector<WorkerActionContext, WorkerActionPlan> GetActionSelector()
    {
        MonoBehaviour[] components = GameObject.GetComponents<MonoBehaviour>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] is IActionSelector<WorkerActionContext, WorkerActionPlan> selector)
                return selector;
        }

        return null;
    }
}
