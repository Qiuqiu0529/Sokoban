using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Sirenix.Utilities;
using Sirenix.Serialization;
using System.Linq;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : SerializedScriptableObject
{
    public List<MapData> insideWorld = new List<MapData>();
    public List<MapData> outsideWorld = new List<MapData>();

    [OdinSerialize]
    public PlayerStartPosition playerStart;

    [OdinSerialize]
    private Dictionary<int, int> globalObjectCounts = new Dictionary<int, int>();  // objID -> count

    [Button("Save Data")]
    public void SaveData() //need change, maybe save as json file 
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    public Dictionary<int, int> GetGlobalObjectCounts()
    {
        return globalObjectCounts;
    }

    public void SetGlobalObjectCounts(Dictionary<int, int> dict)
    {
        globalObjectCounts = dict;
    }


    public bool CanPlaceMoreObjects(ObjData objData)
    {
        if (objData.maxCount == -1) return true;  // 无限制

        // A unique object can be moved to a new cell; this does not create
        // another instance or increase its global count.
        if (objData.maxCount == 1 || objData.objType.HasFlag(ObjectType.Player)) return true;

        globalObjectCounts.TryGetValue(objData.objID, out int currentCount);
        return currentCount < objData.maxCount;
    }

    public void UpdateObjectCount(ObjData objData, bool isAdding)
    {
        if (!globalObjectCounts.ContainsKey(objData.objID))
        {
            globalObjectCounts[objData.objID] = 0;
        }

        if (isAdding)
        {
            globalObjectCounts[objData.objID]++;
        }
        else
        {
            globalObjectCounts[objData.objID] = Mathf.Max(0, globalObjectCounts[objData.objID] - 1);
        }
    }

    public void RemoveObjectOfTypeFromAllMaps(int objID)
    {
        foreach (var map in insideWorld)
        {
            RemoveObjectFromMap(map, objID);
        }
        foreach (var map in outsideWorld)
        {
            RemoveObjectFromMap(map, objID);
        }
    }

    private void RemoveObjectFromMap(MapData map, int objID)
    {
        var instances = map.GetAllObjectInstances().ToList();
        foreach (var instance in instances)
        {
            if (instance.objID == objID)
            {
                Vector3Int grid3D = new Vector3Int(instance.mainGrid.x, instance.mainGrid.y, instance.height);
                map.RemoveObject(grid3D);
                ObjData objData = map.GetObjectData(objID);
                if (objData != null)
                {
                    UpdateObjectCount(objData, false);
                }
            }
        }
    }

    public void ResetObjectCounts()
    {
        globalObjectCounts.Clear();

        foreach (var map in insideWorld)
        {
            foreach (var instance in map.GetAllObjectInstances())
            {
                ObjData objData = map.GetObjectData(instance.objID);
                if (objData != null)
                {
                    UpdateObjectCount(objData, true);
                }
            }
        }

        foreach (var map in outsideWorld)
        {
            foreach (var instance in map.GetAllObjectInstances())
            {
                ObjData objData = map.GetObjectData(instance.objID);
                if (objData != null)
                {
                    UpdateObjectCount(objData, true);
                }
            }
        }
    }


}

[System.Serializable]
public class PlayerStartPosition
{
    public string startMapID;
    public Vector2Int startGridPos;
}
