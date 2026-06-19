using UnityEngine;

public sealed class WorkerCarryStorage
{
    private readonly int maxWheat;
    private int currentWheat;

    public int CurrentWheat => currentWheat;
    public int MaxWheat => maxWheat;
    public bool IsWheatFull => currentWheat >= maxWheat;

    public WorkerCarryStorage(int initialWheat, int maxWheat)
    {
        this.maxWheat = Mathf.Max(1, maxWheat);
        currentWheat = Mathf.Clamp(initialWheat, 0, this.maxWheat);
    }

    public int AddWheat(int amount)
    {
        if (amount <= 0)
            return 0;

        int before = currentWheat;
        currentWheat = Mathf.Min(maxWheat, currentWheat + amount);
        return currentWheat - before;
    }

    public int DepositAllWheat()
    {
        int depositedWheat = currentWheat;
        currentWheat = 0;
        return depositedWheat;
    }
}
