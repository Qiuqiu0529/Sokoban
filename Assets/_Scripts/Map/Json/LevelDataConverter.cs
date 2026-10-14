using System.Collections.Generic;
using UnityEngine;

public static class LevelDataConverter
{
    public static SerializableLevelData FromLevelData(LevelData level)
    {
        var serialized = new SerializableLevelData
        {
            insideWorld = new List<SerializableMapData>(),
            outsideWorld = new List<SerializableMapData>(),
            playerStart = level.playerStart,
            globalObjectCounts = level.GetGlobalObjectCounts()
        };

        foreach (var map in level.insideWorld)
        {
            serialized.insideWorld.Add(MapDataConverter.FromMapData(map));
        }

        foreach (var map in level.outsideWorld)
        {
            serialized.outsideWorld.Add(MapDataConverter.FromMapData(map));
        }

        return serialized;
    }

    public static void ApplyToLevelData(SerializableLevelData serialized, LevelData level)
    {
        level.insideWorld.Clear();
        level.outsideWorld.Clear();

        foreach (var sMap in serialized.insideWorld)
        {
            var newMap = ScriptableObject.CreateInstance<MapData>();
            MapDataConverter.ApplyToMapData(sMap, newMap);
            level.insideWorld.Add(newMap);
        }

        foreach (var sMap in serialized.outsideWorld)
        {
            var newMap = ScriptableObject.CreateInstance<MapData>();
            MapDataConverter.ApplyToMapData(sMap, newMap);
            level.outsideWorld.Add(newMap);
        }

        level.playerStart = serialized.playerStart;
        level.SetGlobalObjectCounts(serialized.globalObjectCounts);
    }
}
