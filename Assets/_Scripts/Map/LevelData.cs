using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Sirenix.Utilities;
using Sirenix.Serialization;
using System.Linq;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : SerializedScriptableObject
{
    public MapData mapData;

    [OdinSerialize]
    public PlayerStartPosition playerStart;

    [OdinSerialize]
    private Dictionary<int, int> globalObjectCounts = new Dictionary<int, int>();

    [Button("Save Data")]
    public void SaveData()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    public Dictionary<int, int> GetGlobalObjectCounts() => globalObjectCounts ??= new Dictionary<int, int>();

    public void SetGlobalObjectCounts(Dictionary<int, int> dict)
    {
        globalObjectCounts = dict ?? new Dictionary<int, int>();
    }

    public bool CanPlaceMoreObjects(ObjData objData)
    {
        if (objData.maxCount == -1) return true;
        if (objData.maxCount == 1 || objData.objType.HasFlag(ObjectType.Player)) return true;
        globalObjectCounts ??= new Dictionary<int, int>();
        globalObjectCounts.TryGetValue(objData.objID, out int currentCount);
        return currentCount < objData.maxCount;
    }

    public void UpdateObjectCount(ObjData objData, bool isAdding)
    {
        globalObjectCounts ??= new Dictionary<int, int>();
        if (!globalObjectCounts.ContainsKey(objData.objID))
            globalObjectCounts[objData.objID] = 0;

        globalObjectCounts[objData.objID] = isAdding
            ? globalObjectCounts[objData.objID] + 1
            : Mathf.Max(0, globalObjectCounts[objData.objID] - 1);
    }

    public void RemoveObjectOfTypeFromAllMaps(int objID)
    {
        if (mapData == null) return;
        var instances = mapData.GetAllObjectInstances().ToList();
        foreach (var instance in instances)
        {
            if (instance.objID != objID) continue;
            mapData.RemoveObject(instance.instanceID);
            ObjData objData = mapData.GetObjectData(objID);
            if (objData != null) UpdateObjectCount(objData, false);
        }
    }

    public void ResetObjectCounts()
    {
        globalObjectCounts ??= new Dictionary<int, int>();
        globalObjectCounts.Clear();
        if (mapData == null) return;

        foreach (var instance in mapData.GetAllObjectInstances())
        {
            ObjData objData = mapData.GetObjectData(instance.objID);
            if (objData != null) UpdateObjectCount(objData, true);
        }
    }
}

[System.Serializable]
public class PlayerStartPosition
{
    public Vector2Int startGridPos;
}
