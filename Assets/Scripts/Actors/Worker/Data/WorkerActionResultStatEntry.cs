using System;
using UnityEngine;
using WorkerEnum;

[Serializable]
public struct WorkerActionResultStatEntry
{
    [SerializeField] private ActionType _actionType;
    [SerializeField] private float _duration;
    [SerializeField] private WorkerStatDelta _statDelta;

    public ActionType ActionType => _actionType;
    public float Duration => _duration;
    public WorkerStatDelta StatDelta => _statDelta;
}
