using System.Collections.Generic;
using UnityEngine;
using WorkerEnum;

public class WarehouseInventory : MonoBehaviour, IInventory
{
    [SerializeField] private int _capacity = 50;

    private readonly Dictionary<ItemType, int> _quantities = new Dictionary<ItemType, int>();
    private int _totalCount;

    public int TotalCount => _totalCount;
    public int Capacity => _capacity;

    public bool TryAdd(ItemType itemType, int quantity, out int accepted)
    {
        if (quantity <= 0)
        {
            accepted = 0;
            return false;
        }

        int available = _capacity - _totalCount;
        accepted = Mathf.Min(quantity, available);

        if (accepted <= 0)
        {
            accepted = 0;
            return false;
        }

        if (!_quantities.ContainsKey(itemType))
            _quantities[itemType] = 0;

        _quantities[itemType] += accepted;
        _totalCount += accepted;
        return true;
    }

    public bool TryRemove(ItemType itemType, int quantity)
    {
        if (quantity <= 0)
            return false;

        if (!_quantities.TryGetValue(itemType, out int current) || current < quantity)
            return false;

        _quantities[itemType] -= quantity;
        _totalCount -= quantity;
        return true;
    }

    public int GetQuantity(ItemType itemType)
    {
        _quantities.TryGetValue(itemType, out int count);
        return count;
    }
}
