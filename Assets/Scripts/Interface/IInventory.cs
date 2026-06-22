using WorkerEnum;

public interface IInventory
{
    int TotalCount { get; }
    int Capacity { get; }

    bool TryAdd(ItemType itemType, int quantity, out int accepted);
    bool TryRemove(ItemType itemType, int quantity);
    int GetQuantity(ItemType itemType);
}
