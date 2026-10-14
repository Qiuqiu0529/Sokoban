using UnityEngine;
using Sirenix.Serialization;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TileDataList_SO", menuName = "Scriptable Objects/TileDataList_SO")]
public class TileDataList_SO : SerializedScriptableObject
{
    public List<TileData> tileDataList=new List<TileData>();

    public TileData GetTileDetails(int tileID)
    {
        return tileDataList.Find(i => i.tileID == tileID);
    }


}
