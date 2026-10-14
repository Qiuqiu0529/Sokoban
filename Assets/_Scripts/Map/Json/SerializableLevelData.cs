using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Sirenix.Serialization;
using System;

[System.Serializable]
public class SerializableLevelData
{
    public List<SerializableMapData> insideWorld = new();
    public List<SerializableMapData> outsideWorld = new();
    public PlayerStartPosition playerStart;
    public Dictionary<int, int> globalObjectCounts;
}
