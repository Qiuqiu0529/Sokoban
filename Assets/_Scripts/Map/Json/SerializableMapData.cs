using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Sirenix.Serialization;
using System;
[System.Serializable]

public class SerializableMapData
{
    public string mapID;
    public int gridLength;
    public int gridWidth;
    public float cellSize;
    public int[] tileIDs;
    public Dictionary<Vector2Int, Vector3> tileRotations;
    public Dictionary<Vector2Int, Dictionary<WallDirection, int>> tileWalls;
    public Dictionary<string, ObjDataInstance> objectInstances;
    public Dictionary<Vector3Int, string> gridToInstanceMap3D;
    public Dictionary<Vector3Int, string> goalObjectMap3D;
}
