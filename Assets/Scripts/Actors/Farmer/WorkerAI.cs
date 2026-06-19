using UnityEngine;
using WorkerEnum;

public class WorkerAI : MonoBehaviour
{
    private WorkerActionContext context;
    private WorkerActionPlan currentPlan;
    private bool currentActionStarted;
    private IActionSelector<WorkerActionContext, WorkerActionPlan> actionSelector;

    public WorkerActionContext Context => context;
    public bool IsInitialized => context != null && actionSelector != null;

    public void Init(
        WorkerActionContext context,
        IActionSelector<WorkerActionContext, WorkerActionPlan> actionSelector)
    {
        this.context = context;
        this.actionSelector = actionSelector;
    }

    private void OnDisable()
    {
        currentPlan?.CurrentAction?.Cancel(context);
        context?.ClearPlan(currentPlan);
        ReturnPlanActions(currentPlan);
        currentPlan = null;
        currentActionStarted = false;
    }

    private void Update()
    {
        if (!IsInitialized)
            return;

        context.Stats.Tick();
        TryEnsureCurrentAction();

        if (HasCurrentAction)
            TickCurrentAction();
    }

    // 현재 action이 없으면 selector에 plan을 요청해 설정한다.
    // WorkerAI.Update() 자율 흐름과 Behavior Graph 브릿지 노드가 공유하는 단일 진입점.
    public bool TryEnsureCurrentAction()
    {
        if (!IsInitialized)
            return false;

        if (HasCurrentAction)
            return true;

        if (actionSelector.TrySelectAction(context, out WorkerActionPlan nextPlan))
            SetPlan(nextPlan);

        return HasCurrentAction;
    }

    public bool HasCurrentAction => currentPlan?.CurrentAction != null;

    // WorkerAI only runs actions; selection and construction live outside this class.
    // public void SetAction(IAction action)
    // {
    //     SetPlan(WorkerActionPlan.Create(action));
    // }

    public void SetPlan(WorkerActionPlan plan)
    {
        if (!IsInitialized)
            return;

        currentPlan?.CurrentAction?.Cancel(context);
        context.ClearPlan(currentPlan);
        ReturnPlanActions(currentPlan);

        currentPlan = plan;
        context.SetPlan(currentPlan);
        currentActionStarted = false;
    }

    public ActionState TickCurrentAction()
    {
        if (!IsInitialized)
            return ActionState.Failed;

        if (currentPlan?.CurrentAction == null)
            return ActionState.Failed;

        if (!currentActionStarted)
        {
            currentPlan.CurrentAction.Start(context);
            currentActionStarted = true;
        }

        ActionState state = currentPlan.CurrentAction.Tick(context);

        if (state == ActionState.Failed)
        {
            context.ClearPlan(currentPlan);
            ReturnPlanActions(currentPlan);
            currentPlan = null;
            currentActionStarted = false;
            return state;
        }

        if (state == ActionState.Success)
        {
            ReturnAction(currentPlan, currentPlan.CurrentAction);

            if (currentPlan.TryMoveNextAction())
            {
                currentActionStarted = false;
                return ActionState.Running;
            }

            context.ClearPlan(currentPlan);
            ReturnPlanActions(currentPlan);
            currentPlan = null;
            currentActionStarted = false;
        }

        return state;
    }

    private void ReturnPlanActions(WorkerActionPlan plan)
    {
        if (plan == null || actionSelector == null)
            return;

        while (plan.RentedActions.Count > 0)
            ReturnAction(plan, plan.RentedActions[0]);
    }

    private void ReturnAction(WorkerActionPlan plan, IAction action)
    {
        if (action == null || actionSelector == null)
            return;

        actionSelector.ReturnAction(action);
        plan?.RemoveRentedAction(action);
    }
}
