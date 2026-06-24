using System.Collections.Generic;
using UnityEngine;
using WorkerEnum;

public class WorkerActionSet : MonoBehaviour
{
    [SerializeField] private int _initialPoolSize = 1;
    [SerializeField] private WorkerActionResultStatData _resultStatData;

    private readonly Dictionary<ActionType, Queue<IAction>> _availableActions = new Dictionary<ActionType, Queue<IAction>>();
    private readonly Dictionary<IAction, ActionType> _rentedActionTypes = new Dictionary<IAction, ActionType>();

    private void Awake()
    {
        // Move·Seek·Patrol은 stat 엔트리 없이 공용으로 사용하므로 코드에서 명시 등록.
        RegisterPool(ActionType.Move);
        RegisterPool(ActionType.Seek);
        RegisterPool(ActionType.Patrol);

        if (_resultStatData == null)
            return;

        foreach (ActionType actionType in _resultStatData.ActionTypes)
            RegisterPool(actionType);
    }

    public bool TryGetResultStatEntry(ActionType actionType, out WorkerActionResultStatEntry entry)
    {
        if (!_resultStatData)
        {
            entry = default;
            return false;
        }

        return _resultStatData.TryGetEntry(actionType, out entry);
    }

    public bool TryGetAction(ActionType actionType, out IAction action)
    {
        return TryRentAction(actionType, out action);
    }

    public bool TryGetAction(ActionType actionType, Vector3 destination, out IAction action)
    {
        if (!TryRentAction(actionType, out action))
            return false;

        if (action is MoveAction moveAction)
        {
            moveAction.SetDestination(destination);
            return true;
        }

        ReturnAction(action);
        action = null;
        return false;
    }

    public bool TryGetAction(ActionType actionType, IInventory inventory, out IAction action)
    {
        if (!TryRentAction(actionType, out action))
            return false;

        if (action is DepositWheatAction depositAction)
        {
            depositAction.SetTargetInventory(inventory);
            return true;
        }

        ReturnAction(action);
        action = null;
        return false;
    }

    public bool TryGetAction(ActionType actionType, CombatTargetHolder holder, float radius, LayerMask enemyMask, out IAction action)
    {
        if (!TryRentAction(actionType, out action))
            return false;

        if (action is SeekAction seekAction)
        {
            seekAction.SetScanParams(holder, radius, enemyMask);
            return true;
        }

        ReturnAction(action);
        action = null;
        return false;
    }

    public bool TryGetAction(ActionType actionType, PatrolParams patrolParams, out IAction action)
    {
        if (!TryRentAction(actionType, out action))
            return false;

        if (action is PatrolAction patrolAction)
        {
            patrolAction.SetPatrolParams(patrolParams);
            return true;
        }

        ReturnAction(action);
        action = null;
        return false;
    }

    public bool TryGetAction(ActionType actionType, IDamageable target, IAttackPower attackPower, out IAction action)
    {
        if (!TryRentAction(actionType, out action))
            return false;

        if (action is AttackAction attackAction)
        {
            attackAction.SetTarget(target);
            attackAction.SetAttackPower(attackPower);
            return true;
        }

        ReturnAction(action);
        action = null;
        return false;
    }

    public bool ReturnAction(IAction action)
    {
        if (action == null)
            return false;

        if (!_rentedActionTypes.TryGetValue(action, out ActionType actionType))
            return false;

        _rentedActionTypes.Remove(action);
        ResetAction(action);
        _availableActions[actionType].Enqueue(action);
        return true;
    }

    private bool TryRentAction(ActionType actionType, out IAction action)
    {
        action = null;

        if (!_availableActions.TryGetValue(actionType, out Queue<IAction> actions))
            return false;

        if (actions.Count <= 0 && TryCreateAction(actionType, out IAction createdAction))
            actions.Enqueue(createdAction);

        if (actions.Count <= 0)
            return false;

        action = actions.Dequeue();
        _rentedActionTypes[action] = actionType;
        return true;
    }

    private void RegisterPool(ActionType actionType)
    {
        // 중복 등록 방지: 데이터에 실수로 Move 등 공용 타입이 포함돼도 이중 풀 생성 안 함.
        if (_availableActions.ContainsKey(actionType))
            return;

        _availableActions[actionType] = new Queue<IAction>();

        int poolSize = Mathf.Max(1, _initialPoolSize);
        for (int i = 0; i < poolSize; i++)
        {
            if (TryCreateAction(actionType, out IAction action))
                _availableActions[actionType].Enqueue(action);
        }
    }

    private bool TryCreateAction(ActionType actionType, out IAction action)
    {
        action = actionType switch
        {
            ActionType.Rest => TryGetResultStatEntry(actionType, out WorkerActionResultStatEntry restEntry)
                ? new RestAction(restEntry)
                : null,
            ActionType.Eat => TryGetResultStatEntry(actionType, out WorkerActionResultStatEntry eatEntry)
                ? new EatAction(eatEntry)
                : null,
            ActionType.Drink => TryGetResultStatEntry(actionType, out WorkerActionResultStatEntry drinkEntry)
                ? new DrinkAction(drinkEntry)
                : null,
            ActionType.Work => TryGetResultStatEntry(actionType, out WorkerActionResultStatEntry workEntry)
                ? new WorkAction(workEntry)
                : null,
            ActionType.DepositWheat => TryGetResultStatEntry(actionType, out WorkerActionResultStatEntry depositWheatEntry)
                ? new DepositWheatAction(depositWheatEntry)
                : null,
            ActionType.Move => new MoveAction(),
            ActionType.Seek => new SeekAction(),
            ActionType.Patrol => new PatrolAction(),
            ActionType.Attack => TryGetResultStatEntry(actionType, out WorkerActionResultStatEntry attackEntry)
                ? new AttackAction(attackEntry)
                : null,
            _ => null
        };

        return action != null;
    }

    private void ResetAction(IAction action)
    {
        if (action is MoveAction moveAction)
            moveAction.ClearDestination();

        if (action is DepositWheatAction depositAction)
            depositAction.ClearTargetInventory();

        if (action is SeekAction seekAction)
            seekAction.ClearScanParams();

        if (action is PatrolAction patrolAction)
            patrolAction.ClearPatrolParams();

        if (action is AttackAction attackAction)
        {
            attackAction.ClearTarget();
            attackAction.ClearAttackPower();
        }
    }
}
