using UnityEngine;
using System.Collections.Generic;
using Sirenix.Serialization;

public class WallData
{
    public int wallID;
    public string wallName;
    public Sprite wallSprite;
    // Optional visual variants for this logical wall type. The legacy prefab
    // remains as a fallback for existing wall data assets.
    public List<GameObject> prefabVariants = new List<GameObject>();
    public GameObject prefab;
    public Vector3 startOffset;

    public GameObject GetPrefab(Vector2Int gridPosition, WallDirection direction)
    {
        if (prefabVariants == null || prefabVariants.Count == 0) return prefab;

        // Normalize shared edges so placing from either adjacent cell produces
        // the same variant after the wall data is loaded again.
        if (direction == WallDirection.Left)
        {
            gridPosition += Vector2Int.left;
            direction = WallDirection.Right;
        }
        else if (direction == WallDirection.Down)
        {
            gridPosition += Vector2Int.down;
            direction = WallDirection.Up;
        }

        unchecked
        {
            uint hash = (uint)(gridPosition.x * 73856093) ^
                        (uint)(gridPosition.y * 19349663) ^
                        (uint)((int)direction * 83492791) ^
                        ((uint)wallID * 2654435761u);
            int startIndex = (int)(hash % (uint)prefabVariants.Count);
            for (int i = 0; i < prefabVariants.Count; i++)
            {
                GameObject candidate = prefabVariants[(startIndex + i) % prefabVariants.Count];
                if (candidate != null) return candidate;
            }
        }

        return prefab;
    }
}

// [System.Serializable]
// public class WallDataInstance
// {
//     public int wallID;
//     public string instanceID;


   

//     public WallDataInstance DeepCopy()
//     {
       
//     }
   
// }
