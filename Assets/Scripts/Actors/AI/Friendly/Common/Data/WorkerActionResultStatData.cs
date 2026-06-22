using System.Collections.Generic;
using UnityEngine;
using WorkerEnum;

[CreateAssetMenu(
    fileName = "WorkerActionResultStatData",
    menuName = "Worker/Action Result Stat Data")]
public class WorkerActionResultStatData : ScriptableObject
{
    [SerializeField] private WorkerActionResultStatEntry[] _entries;

    public IEnumerable<ActionType> ActionTypes
    {
        get
        {
            if (_entries == null)
                yield break;

            foreach (WorkerActionResultStatEntry entry in _entries)
                yield return entry.ActionType;
        }
    }

    public bool TryGetEntry(ActionType actionType, out WorkerActionResultStatEntry entry)
    {
        if (_entries == null)
        {
            entry = default;
            return false;
        }

        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i].ActionType != actionType)
                continue;

            entry = _entries[i];
            return true;
        }

        entry = default;
        return false;
    }
}
