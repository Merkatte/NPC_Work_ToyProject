using System;
using UnityEngine;

[Serializable]
public struct WorkerStatDelta
{
    [SerializeField] private float _hunger;
    [SerializeField] private float _thirst;
    [SerializeField] private float _fatigue;

    public float Hunger => _hunger;
    public float Thirst => _thirst;
    public float Fatigue => _fatigue;
}
