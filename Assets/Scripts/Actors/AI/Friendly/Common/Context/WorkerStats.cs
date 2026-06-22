using UnityEngine;

public class WorkerStats: ITickable
{
    private readonly float MIN_HUNGER_VAL = 0f;
    private readonly float MAX_HUNGER_VAL = 100f;
    private readonly float MIN_THIRST_VAL = 0f;
    private readonly float MAX_THIRST_VAL = 100f;
    private readonly float MIN_FATIGUE_VAL = 0f;
    private readonly float MAX_FATIGUE_VAL = 100f;
    
    private float _hunger;
    private float _thirst;
    private float _fatigue;
    
    public float Hunger
    {
        get => _hunger;
        private set => _hunger = Mathf.Clamp(value, MIN_HUNGER_VAL, MAX_HUNGER_VAL);
    }
    
    public float Thirst
    {
        get => _thirst;
        private set => _thirst = Mathf.Clamp(value, MIN_THIRST_VAL, MAX_THIRST_VAL);
    }

    public float Fatigue
    {
        get => _fatigue;
        private set => _fatigue = Mathf.Clamp(value, MIN_FATIGUE_VAL, MAX_FATIGUE_VAL);
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
