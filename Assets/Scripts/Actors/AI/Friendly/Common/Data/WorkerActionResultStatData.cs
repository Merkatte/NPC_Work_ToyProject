using UnityEngine;
using WorkerEnum;

[CreateAssetMenu(
    fileName = "WorkerActionResultStatData",
    menuName = "Worker/Action Result Stat Data")]
public class WorkerActionResultStatData : ScriptableObject
{
    [SerializeField] private WorkerActionResultStatEntry[] _entries;

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
