using UnityEngine;
using Sirenix.Serialization;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WallDataList_SO", menuName = "Scriptable Objects/WallDataList_SO")]
public class WallDataList_SO : SerializedScriptableObject
{

    public List<WallData> wallDataList = new List<WallData>();

    public WallData GetTileDetails(int ID)
    {
        return wallDataList.Find(i => i.wallID == ID);
    }
}
