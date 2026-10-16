using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;

[System.Serializable]
public class SerializableLevelData
{
    public SerializableMapData mapData;
    public PlayerStartPosition playerStart;
    public Dictionary<int, int> globalObjectCounts;
}
