using UnityEngine;
using WorkerEnum;

[RequireComponent(typeof(WorkerMovementStats))]
public class WorkerAI : MonoBehaviour
{
    [SerializeField] private MonoBehaviour actionSelectorSource;
    [SerializeField] private WorkerActionSet actionSet;
    [SerializeField] private DestinationProvider destinationProvider;
    [SerializeField] private WorkerMovementStats movementStats;
    [SerializeField] private TickManager tickManager;
    [SerializeField] private TickType statsTickType = TickType.Normal;

    private WorkerActionContext context;
    private WorkerActionPlan currentPlan;
    private bool currentActionStarted;
    private WorkerStats stats;
    private WorkerMover mover;
    private IActionSelector<WorkerActionContext, WorkerActionPlan> actionSelector;

    public WorkerActionContext Context => context;
    
    //TODO : Create Factory or Manager and Call this. Don`t forget to erase awake.
    public void Init(WorkerActionContext context) => this.context = context;

    //Temporary Method. will be erased when factory method call Init.
    private void Awake()
    {
        if (destinationProvider == null)
            TryGetComponent(out destinationProvider);

        if (actionSet == null)
            TryGetComponent(out actionSet);

        if (!movementStats)
            TryGetComponent(out movementStats);

        stats = new WorkerStats(0, 0, 0);
        mover = new WorkerMover(transform, movementStats);
        context = new WorkerActionContext(transform, mover, stats, movementStats, destinationProvider);
        actionSelector = ResolveActionSelector();
    }

    //private void OnEnable() => tickManager?.RegisterTick(statsTickType, stats);

    private void OnDisable()
    {
        tickManager?.UnregisterTick(stats);
        currentPlan?.CurrentAction?.Cancel(context);
        context?.ClearPlan(currentPlan);
        ReturnPlanActions(currentPlan);
        currentPlan = null;
        currentActionStarted = false;
    }

    private void Update()
    {
        stats?.Tick();
        
        if (!HasCurrentAction && actionSelector != null)
        {
            if (actionSelector.TrySelectAction(context, out WorkerActionPlan nextPlan))
                SetPlan(nextPlan);
        }

        if (HasCurrentAction)
            TickCurrentAction();
    }

    public bool HasCurrentAction => currentPlan?.CurrentAction != null;

    // WorkerAI only runs actions; selection and construction live outside this class.
    public void SetAction(IAction action)
    {
        SetPlan(WorkerActionPlan.Create(action));
    }

    public void SetPlan(WorkerActionPlan plan)
    {
        currentPlan?.CurrentAction?.Cancel(context);
        context.ClearPlan(currentPlan);
        ReturnPlanActions(currentPlan);

        currentPlan = plan;
        context.SetPlan(currentPlan);
        currentActionStarted = false;
    }

    public ActionState TickCurrentAction()
    {
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
        if (plan == null || !actionSet)
            return;

        for (int i = 0; i < plan.RentedActions.Count; i++)
            actionSet.ReturnAction(plan.RentedActions[i]);
    }

    private IActionSelector<WorkerActionContext, WorkerActionPlan> ResolveActionSelector()
    {
        if (actionSelectorSource != null)
        {
            if (actionSelectorSource is IActionSelector<WorkerActionContext, WorkerActionPlan> selector)
                return selector;

            Debug.LogWarning($"{actionSelectorSource.name} does not implement {nameof(IActionSelector<WorkerActionContext, WorkerActionPlan>)}.", this);
        }

        MonoBehaviour[] components = GetComponents<MonoBehaviour>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] is IActionSelector<WorkerActionContext, WorkerActionPlan> selector)
                return selector;
        }

        Debug.LogWarning($"{nameof(WorkerAI)} could not find an {nameof(IActionSelector<WorkerActionContext, WorkerActionPlan>)}.", this);
        return null;
    }
}
