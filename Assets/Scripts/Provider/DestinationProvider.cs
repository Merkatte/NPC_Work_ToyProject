using System;
using UnityEngine;
using WorkerEnum;

public class DestinationProvider : MonoBehaviour
{
    [SerializeField] private DestinationInfo[] _destinationInfos;

    public bool TryGetDestinationPosition(ActionType actionType, out Vector3 position)
    {
        for (int i = 0; i < _destinationInfos.Length; i++)
        {
            DestinationInfo info = _destinationInfos[i];

            if (info.ActionType != actionType || info.Destination == null)
                continue;

            position = info.Destination.transform.position;
            return true;
        }

        position = default;
        return false;
    }

    public bool TryGetInventory(ActionType actionType, out IInventory inventory)
    {
        for (int i = 0; i < _destinationInfos.Length; i++)
        {
            DestinationInfo info = _destinationInfos[i];

            if (info.ActionType != actionType || info.Destination == null)
                continue;

            return info.Destination.TryGetComponent(out inventory);
        }

        inventory = null;
        return false;
    }
}

[Serializable]
public class DestinationInfo
{
    public GameObject Destination;
    public ActionType ActionType;
}
