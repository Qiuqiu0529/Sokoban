using System.Collections.Generic;
using UnityEngine;

public static class LevelDataConverter
{
    public static SerializableLevelData FromLevelData(LevelData level)
    {
        var serialized = new SerializableLevelData
        {
            mapData = level.mapData != null ? MapDataConverter.FromMapData(level.mapData) : null,
            playerStart = level.playerStart,
            globalObjectCounts = level.GetGlobalObjectCounts()
        };
        return serialized;
    }

    public static void ApplyToLevelData(SerializableLevelData serialized, LevelData level)
    {
        level.mapData = null;
        if (serialized.mapData != null)
        {
            level.mapData = ScriptableObject.CreateInstance<MapData>();
            MapDataConverter.ApplyToMapData(serialized.mapData, level.mapData);
        }

        level.playerStart = serialized.playerStart;
        level.SetGlobalObjectCounts(serialized.globalObjectCounts ?? new Dictionary<int, int>());
    }
}
