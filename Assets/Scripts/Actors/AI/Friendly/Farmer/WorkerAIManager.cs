using System;
using System.Collections.Generic;
using GameAnimation;
using UnityEngine;

public class WorkerAIManager : MonoBehaviour
{
    [SerializeField] private WorkerAI _workerPrefab;
    [SerializeField] private Transform _workerParent;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private WorkerInitialStats _initialStats = new WorkerInitialStats();
    [SerializeField] private WorkerInitialCarryStorage _initialCarryStorage = new WorkerInitialCarryStorage();
    [SerializeField] private WorkerSelectorType _initialSelectorType = WorkerSelectorType.Default;
    [SerializeField] private List<WorkerSelectorEntry> _selectorEntries = new List<WorkerSelectorEntry>();

    private readonly List<WorkerAI> workers = new List<WorkerAI>();
    private IAnimSet animSet;

    public IReadOnlyList<WorkerAI> Workers => workers;

    private void Awake()
    {
        animSet = new AnimSet();
    }

    private void Start()
    {
        SpawnInitialWorkers();
    }

    public WorkerAI SpawnWorker(Vector3 position)
    {
        return SpawnWorker(position, Quaternion.identity);
    }

    public WorkerAI SpawnWorker(Vector3 position, Quaternion rotation)
    {
        if (!_workerPrefab)
        {
            Debug.Log("No WorkerPrefab");
            return null;
        }

        WorkerAI worker = Instantiate(_workerPrefab, position, rotation, _workerParent);
        Debug.Log(worker);
        if (!TryInitializeWorker(worker))
        {
            Destroy(worker.gameObject);
            Debug.Log("Worker Init Fail");
            return null;
        }

        workers.Add(worker);
        return worker;
    }

    public bool UnregisterWorker(WorkerAI worker)
    {
        if (!worker)
            return false;

        return workers.Remove(worker);
    }

    private void SpawnInitialWorkers()
    {
        Debug.Log("Spawn Init Called");
        if (_spawnPoints == null)
        {
            Debug.Log("SpawnPoint is null");
            return;
        }

        Debug.Log(_spawnPoints.Length);

        foreach (var spawnPoint in _spawnPoints)
        {
            if (!spawnPoint)
            {
                Debug.Log("There is no SpawnPoints");
                continue;
            }

            SpawnWorker(spawnPoint.position, spawnPoint.rotation);
        }
    }

    private bool TryInitializeWorker(WorkerAI worker)
    {
        if (!worker)
        {
            Debug.Log(worker);
            return false;
        }

        if (!worker.TryGetComponent(out WorkerMovementStats movementStats))
        {
            Debug.LogWarning($"{nameof(WorkerAI)} prefab requires {nameof(WorkerMovementStats)}.", this);
            return false;
        }

        if (!TryCreateSelector(_initialSelectorType, worker.transform, out IActionSelector<WorkerActionContext, WorkerActionPlan> actionSelector))
        {
            Debug.LogWarning($"{nameof(WorkerAIManager)} could not create a {_initialSelectorType} selector.", this);
            return false;
        }

        SpriteRenderer spriteRenderer = worker.GetComponentInChildren<SpriteRenderer>();
        if (!spriteRenderer || spriteRenderer.transform == worker.transform)
        {
            Debug.LogWarning($"{nameof(WorkerAI)} prefab requires a child visual root with a {nameof(SpriteRenderer)}.", this);
            return false;
        }

        WorkerStats stats = new WorkerStats(
            _initialStats.Hunger,
            _initialStats.Thirst,
            _initialStats.Fatigue);
        WorkerCarryStorage carryStorage = new WorkerCarryStorage(
            _initialCarryStorage.InitialWheat,
            _initialCarryStorage.MaxWheat);
        WorkerMover mover = new WorkerMover(worker.transform, movementStats);
        IAnimPlayer animation = new ActorAnimationController(animSet, spriteRenderer.transform);
        WorkerActionContext context = new WorkerActionContext(
            worker.transform,
            mover,
            stats,
            movementStats,
            carryStorage,
            animation);

        worker.Init(context, actionSelector);
        return true;
    }

    private bool TryCreateSelector(
        WorkerSelectorType selectorType,
        Transform parent,
        out IActionSelector<WorkerActionContext, WorkerActionPlan> selector)
    {
        selector = null;

        for (int i = 0; i < _selectorEntries.Count; i++)
        {
            WorkerSelectorEntry entry = _selectorEntries[i];
            if (entry.SelectorType != selectorType)
                continue;

            MonoBehaviour selectorSource = entry.SelectorSource;
            if (!selectorSource)
                return false;

            MonoBehaviour selectorInstance = Instantiate(selectorSource, parent);
            selectorInstance.name = $"{selectorType}Selector";

            if (selectorInstance is IWorkerActionSelectorSetup setup)
            {
                if (!TryGetSelectorActionSet(selectorInstance, out WorkerActionSet actionSet))
                {
                    Destroy(selectorInstance.gameObject);
                    return false;
                }

                setup.Init(actionSet);
            }

            if (selectorInstance is IActionSelector<WorkerActionContext, WorkerActionPlan> actionSelector)
            {
                selector = actionSelector;
                return true;
            }

            Destroy(selectorInstance.gameObject);
            return false;
        }

        return false;
    }

    private bool TryGetSelectorActionSet(MonoBehaviour selectorInstance, out WorkerActionSet actionSet)
    {
        if (selectorInstance.TryGetComponent(out actionSet))
            return true;

        Debug.LogWarning($"{selectorInstance.name} requires {nameof(WorkerActionSet)} on the selector template.", this);
        return false;
    }
}

[Serializable]
public class WorkerSelectorEntry
{
    [SerializeField] private WorkerSelectorType _selectorType;
    [SerializeField] private MonoBehaviour _selectorSource;

    public WorkerSelectorType SelectorType => _selectorType;
    public MonoBehaviour SelectorSource => _selectorSource;
}

[Serializable]
public class WorkerInitialStats
{
    [SerializeField] private float _hunger;
    [SerializeField] private float _thirst;
    [SerializeField] private float _fatigue;

    public float Hunger => _hunger;
    public float Thirst => _thirst;
    public float Fatigue => _fatigue;
}

[Serializable]
public class WorkerInitialCarryStorage
{
    [SerializeField] private int _initialWheat;
    [SerializeField] private int _maxWheat = 5;

    public int InitialWheat => _initialWheat;
    public int MaxWheat => _maxWheat;
}
