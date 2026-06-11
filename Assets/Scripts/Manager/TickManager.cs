using System.Collections.Generic;
using UnityEngine;

public class TickManager : MonoBehaviour
{
    [SerializeField] [Range(0.1f, 5f)] private float slowtick = 1f;
    [SerializeField] [Range(0.1f, 5f)] private float normaltick = 0.5f;
    [SerializeField] [Range(0.1f, 5f)] private float fasttick = 0.1f;

    private float _slowTimer;
    private float _normalTimer;
    private float _fastTimer;
    
    private readonly HashSet<ITickable> _slowTickers = new HashSet<ITickable>();
    private readonly HashSet<ITickable> _normalTickers = new HashSet<ITickable>();
    private readonly HashSet<ITickable> _fastTickers = new HashSet<ITickable>();
    
    private readonly Dictionary<ITickable, TickType> _tickDict = new Dictionary<ITickable, TickType>();
    
    private readonly HashSet<ITickable> _removePendingQueue = new HashSet<ITickable>();
    private readonly List<ITickable> _tickBuffer = new List<ITickable>();

    private void Update()
    {
        RemovePendingTicks();

        float deltaTime = Time.deltaTime;

        TickGroup(ref _slowTimer, slowtick, _slowTickers, deltaTime);
        TickGroup(ref _normalTimer, normaltick, _normalTickers, deltaTime);
        TickGroup(ref _fastTimer, fasttick, _fastTickers, deltaTime);

        RemovePendingTicks();
    }

    public void RegisterTick(TickType tickType, ITickable tickable)
    {
        if (tickable == null)
            return;

        RemoveTickImmediately(tickable);

        GetTickers(tickType).Add(tickable);
        _tickDict[tickable] = tickType;
        _removePendingQueue.Remove(tickable);
    }

    public void UnregisterTick(ITickable tickable)
    {
        if (tickable == null)
            return;

        _removePendingQueue.Add(tickable);
    }
    

    private void RemovePendingTicks()
    {
        if (_removePendingQueue.Count == 0)
            return;

        foreach (ITickable tickable in _removePendingQueue)
        {
            RemoveTickImmediately(tickable);
        }

        _removePendingQueue.Clear();
    }

    private void RemoveTickImmediately(ITickable tickable)
    {
        if (!_tickDict.TryGetValue(tickable, out TickType tickType))
            return;

        GetTickers(tickType).Remove(tickable);
        _tickDict.Remove(tickable);
    }

    private HashSet<ITickable> GetTickers(TickType tickType)
    {
        return tickType switch
        {
            TickType.Slow => _slowTickers,
            TickType.Normal => _normalTickers,
            TickType.Fast => _fastTickers,
            _ => _normalTickers
        };
    }

    private void TickGroup(
        ref float timer,
        float interval,
        HashSet<ITickable> tickers,
        float deltaTime)
    {
        timer += deltaTime;

        if (timer < interval)
            return;

        timer = 0f;

        _tickBuffer.Clear();
        _tickBuffer.AddRange(tickers);

        for (int i = 0; i < _tickBuffer.Count; i++)
        {
            _tickBuffer[i].Tick();
        }
    }
}
