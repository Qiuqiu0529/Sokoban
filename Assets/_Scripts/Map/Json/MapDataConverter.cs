using System.Collections.Generic;
using UnityEngine;

public static class MapDataConverter
{
    public static SerializableMapData FromMapData(MapData map)
    {
        return new SerializableMapData
        {
            mapID = map.mapID,
            gridLength = map.gridLength,
            gridWidth = map.gridWidth,
            gridMin = map.gridMin,
            cellSize = map.cellSize,
            tileIDs = map.GetTileIDs(),
            tileRotations = map.GetTileRotations(),
            tileWalls = map.GetTileWalls(),
            objectInstances = map.GetObjectInstances(),
            gridToInstanceMap3D = map.GetGridToInstanceMap(),
            goalObjectMap3D=map.GetGoalInstanceMap()
        };
    }

    public static void ApplyToMapData(SerializableMapData serialized, MapData map)
    {
        map.mapID = serialized.mapID;
        map.gridLength = serialized.gridLength;
        map.gridWidth = serialized.gridWidth;
        map.gridMin = serialized.gridMin;
        map.cellSize = serialized.cellSize;

        map.SetTileIDs(serialized.tileIDs);
        map.SetTileRotations(serialized.tileRotations);
        map.SetTileWalls(serialized.tileWalls);
        map.SetObjectInstances(serialized.objectInstances);
        map.SetGridToInstanceMap(serialized.gridToInstanceMap3D);
        map.SetGoalInstanceMap(serialized.goalObjectMap3D);
    }
}
