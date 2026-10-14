using UnityEngine;
using System.Collections.Generic;
using Sirenix.Serialization;
using System;

public class ObjData
{
    public int objID;
    public string objName;

    [OdinSerialize]
    public List<Vector3Int> occupiedGrids = new List<Vector3Int>();

    public ObjectType objType;
    public Sprite objSprite;
    public GameObject prefab;
    public int maxCount = -1; 

    public bool ContainsGrid(Vector3Int gridPosition)
    {
        return occupiedGrids.Contains(gridPosition);
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

    public int height;
    public Quaternion rotation;
    public List<Vector3Int> currentOccupied;
    public bool canPush;
    public bool canPossess;

    public ObjDataInstance DeepCopy()
    {
        return new ObjDataInstance(this.objID, this.mainGrid, this.height, this.rotation)
        {
            instanceID = this.instanceID,
            canPossess = this.canPossess,
            canPush = this.canPush,
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
        canPush = objData.objType.HasFlag(ObjectType.Pushable);
        canPossess = objData.objType.HasFlag(ObjectType.CanPossess);
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
