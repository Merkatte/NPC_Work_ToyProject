using UnityEngine;

public class WorkerStats: ITickable
{
    private const float MinStatVal = 0f;
    private const float MaxStatVal = 100f;
    
    private float _hunger;
    private float _thirst;
    private float _fatigue;
    
    public float Hunger
    {
        get => _hunger;
        private set => _hunger = Mathf.Clamp(value, MinStatVal, MaxStatVal);
    }

    public float Thirst
    {
        get => _thirst;
        private set => _thirst = Mathf.Clamp(value, MinStatVal, MaxStatVal);
    }

    public float Fatigue
    {
        get => _fatigue;
        private set => _fatigue = Mathf.Clamp(value, MinStatVal, MaxStatVal);
    }

    public WorkerStats(float hunger, float thirst, float fatigue)
    {
        Hunger = hunger;
        Thirst = thirst;
        Fatigue = fatigue;
    }

    public void Tick()
    {
        Hunger = Mathf.Max(0f, Hunger + Time.deltaTime);
        Thirst = Mathf.Max(0f, Thirst + Time.deltaTime);
    }
    
    public void Apply(WorkerStatDelta delta)
    {
        Hunger += delta.Hunger;
        Thirst += delta.Thirst;
        Fatigue += delta.Fatigue;
    }
}
