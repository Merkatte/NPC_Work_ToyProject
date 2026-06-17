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
    // WorkerAI 한 곳만 의존한다. selector/WorkerActionSet 참조를 직접 탐색하지 않는다.
    protected override Status OnStart()
    {
        if (GameObject.TryGetComponent(out WorkerAI workerAI))
            return workerAI.TryEnsureCurrentAction() ? Status.Success : Status.Failure;

        return Status.Failure;
    }
}
