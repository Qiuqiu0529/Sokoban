using UnityEngine;
using System.Collections.Generic;
using Sirenix.Serialization;

public class TileData
{
    public int tileID;
    public string tileName;

    public TileType tileType;
    public Sprite tileSprite;
    public GameObject prefab;
    public Vector3 startOffset;
}
