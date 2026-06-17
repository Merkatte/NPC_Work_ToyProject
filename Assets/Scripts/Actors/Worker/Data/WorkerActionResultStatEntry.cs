using System;
using UnityEngine;
using WorkerEnum;

[Serializable]
public struct WorkerActionResultStatEntry
{
    [SerializeField] private ActionType _actionType;
    [SerializeField] private float _duration;
    [SerializeField] private WorkerStatDelta _statDelta;
    [SerializeField] private int _wheatDelta;

    public ActionType ActionType => _actionType;
    public float Duration => _duration;
    public WorkerStatDelta StatDelta => _statDelta;
    public int WheatDelta => _wheatDelta;
}
