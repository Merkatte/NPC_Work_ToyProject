using WorkerEnum;

// Worker 욕구 임계 판정 공용 헬퍼.
// FarmerActionSelector와 GuardActionSelector가 동일 기준으로 욕구 우선순위를 판정한다.
// Thirst > Hunger > Fatigue 순으로 확인한다.
public static class WorkerNeedsPolicy
{
    public static bool TryGetNeededActionType(WorkerStats stats, float threshold, out ActionType actionType)
    {
        if (stats.Thirst >= threshold)
        {
            actionType = ActionType.Drink;
            return true;
        }

        if (stats.Hunger >= threshold)
        {
            actionType = ActionType.Eat;
            return true;
        }

        if (stats.Fatigue >= threshold)
        {
            actionType = ActionType.Rest;
            return true;
        }

        actionType = default;
        return false;
    }
}
