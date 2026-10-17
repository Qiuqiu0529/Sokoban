using UnityEngine;
using System.Collections.Generic;
using Sirenix.Serialization;
using System;

public class ObjData
{
    public int objID;
    public string objName;

    // Keep 3D offsets for multi-cell footprints and future rules. Editor
    // placement currently fixes every instance to the ground layer.
    [OdinSerialize]
    public List<Vector3Int> occupiedGrids = new List<Vector3Int>();

    public ObjectType objType;
    public Sprite objSprite;
    public GameObject prefab;
    public int maxCount = -1; 

    public virtual GameObject GetPrefab(Vector2Int gridPosition)
    {
        return prefab;
    }

    public bool ContainsGrid(Vector3Int gridPosition)
    {
        return occupiedGrids.Contains(gridPosition);
    }
}

/// <summary>
/// ObjData variant for objects that choose a visual prefab from a set.
/// The choice is stable for each grid position, matching wall variant behavior.
/// </summary>
public class RandomObjData : ObjData
{
    public List<GameObject> prefabVariants = new List<GameObject>();

    public override GameObject GetPrefab(Vector2Int gridPosition)
    {
        if (prefabVariants == null || prefabVariants.Count == 0) return prefab;

        unchecked
        {
            uint hash = (uint)(gridPosition.x * 73856093) ^
                        (uint)(gridPosition.y * 19349663) ^
                        ((uint)objID * 2654435761u);
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

public class TrailObj:ObjData
{
    public int stepMax;
}


[System.Serializable]
public class ObjDataInstance
{
    public int objID;
    public string instanceID;
    public Vector2Int mainGrid;
    // Shared by linked runtime objects such as a ghost and its grave.
    // Zero means that the instance has not been assigned to a channel.
    public int channelID;

    public int height;
    public Quaternion rotation;
    public List<Vector3Int> currentOccupied;
    // Pure decorations can be placed continuously on the ground plane.
    // Other objects continue to use their grid-derived position.
    public bool hasCustomWorldPosition;
    public Vector3 customWorldPosition;

    public ObjDataInstance DeepCopy()
    {
        return new ObjDataInstance(this.objID, this.mainGrid, this.height, this.rotation)
        {
            instanceID = this.instanceID,
            channelID = this.channelID,
            hasCustomWorldPosition = this.hasCustomWorldPosition,
            customWorldPosition = this.customWorldPosition,
            currentOccupied = new List<Vector3Int>(this.currentOccupied)
        };
    }
    public ObjDataInstance(int id, Vector2Int position, int height, Quaternion rot)
    {
        objID = id;
        if (string.IsNullOrEmpty(instanceID))
        {
            instanceID = System.Guid.NewGuid().ToString();
        }
        mainGrid = position;
        this.height = height;
        rotation = rot;
    }


    public ObjDataInstance(int id, Vector2Int position, int height, Quaternion rot, ObjData objData)
    {
        objID = id;
        if (string.IsNullOrEmpty(instanceID))
        {
            instanceID = System.Guid.NewGuid().ToString();
        }

        mainGrid = position;
        this.height = height;
        rotation = rot;
        currentOccupied = CalculateOccupiedGrids(objData);
    }

    public List<Vector3Int> CalculateOccupiedGrids(ObjData objData)
    {
        List<Vector3Int> occupiedPositions = new List<Vector3Int>();

        foreach (var offset in objData.occupiedGrids)
        {
            Vector3Int baseOffset = new Vector3Int(offset.x, offset.z, offset.y);
            Vector3Int rotatedOffset = RotateOffset(baseOffset, rotation);
            
            Vector3Int finalPos = new Vector3Int(
                mainGrid.x + rotatedOffset.x,
                mainGrid.y + rotatedOffset.z,
                height + rotatedOffset.y
            );
            
            occupiedPositions.Add(finalPos);
        }
        return occupiedPositions;
    }

    public Vector3Int RotateOffset(Vector3Int offset, Quaternion rot)
    {
        Vector3 offsetVector = new Vector3(offset.x, offset.y, offset.z);
        
        Vector3 rotated = rot * offsetVector;
        
        return new Vector3Int(
            Mathf.RoundToInt(rotated.x),
            Mathf.RoundToInt(rotated.y),
            Mathf.RoundToInt(rotated.z)
        );
    }

}
