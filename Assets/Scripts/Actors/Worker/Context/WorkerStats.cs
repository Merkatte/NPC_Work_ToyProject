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
    
    public void Eat(float amount) => Hunger -= amount;
    public void Drink(float amount) => Thirst -= amount;
    public void Rest(float amount) => Fatigue -= amount;
    public void Work(float amount) => Fatigue += amount;

    public WorkerStats(float hunger, float thirst, float fatigue)
    {
        Hunger = hunger;
        Thirst = thirst;
        Fatigue = fatigue;
    }

    public void Tick()
    {
        Hunger = Mathf.Max(0f, Hunger + 1f);
        Thirst = Mathf.Max(0f, Thirst + 1f);
    }
    
    public void Work(float hunger, float thirst, float fatigue)
    {
        Hunger += hunger;
        Thirst += thirst;
        Fatigue += fatigue;
    }
}
