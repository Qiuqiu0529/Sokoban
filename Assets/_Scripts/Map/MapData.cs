using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Sirenix.Serialization;
using System;
using PixelCrushers;

[CreateAssetMenu(fileName = "MapData", menuName = "Scriptable Objects/MapData")]
public class MapData : SerializedScriptableObject
{
    public string mapID;
    public int gridLength = 10;
    public int gridWidth = 10;
    // Logical grid coordinate at the lower-left corner of tileIDs.
    public Vector2Int gridMin = Vector2Int.zero;
    public float cellSize = 1.0f;

    public TileDataList_SO tileDataList;
    public WallDataList_SO wallDataList;
    public ObjDataList_SO objDataList;

    [OdinSerialize]
    private int[] tileIDs;
    [OdinSerialize]
    private Dictionary<Vector2Int, Vector3> tileRotations = new Dictionary<Vector2Int, Vector3>();

    [OdinSerialize]
    private Dictionary<Vector2Int, Dictionary<WallDirection, int>> tileWalls = new();


    [OdinSerialize]
    private Dictionary<string, ObjDataInstance> objectInstances = new Dictionary<string, ObjDataInstance>();

    [OdinSerialize]
    private Dictionary<Vector3Int, string> gridToInstanceMap3D = new Dictionary<Vector3Int, string>();

    [OdinSerialize]
    private Dictionary<Vector3Int, string> goalObjectMap3D = new Dictionary<Vector3Int, string>();


    // --- Getters ---
    public int[] GetTileIDs() => tileIDs;
    public Dictionary<Vector2Int, Vector3> GetTileRotations() => tileRotations;
    public Dictionary<Vector2Int, Dictionary<WallDirection, int>> GetTileWalls() => tileWalls;
    public Dictionary<string, ObjDataInstance> GetObjectInstances() => objectInstances;
    public Dictionary<Vector3Int, string> GetGridToInstanceMap() => gridToInstanceMap3D;
    public Dictionary<Vector3Int, string> GetGoalInstanceMap() => goalObjectMap3D;

    // --- Setters ---
    public void SetTileIDs(int[] ids) => tileIDs = ids;
    public void SetTileRotations(Dictionary<Vector2Int, Vector3> rotations) => tileRotations = rotations;
    public void SetTileWalls(Dictionary<Vector2Int, Dictionary<WallDirection, int>> walls) => tileWalls = walls;
    public void SetObjectInstances(Dictionary<string, ObjDataInstance> objs) => objectInstances = objs;
    public void SetGridToInstanceMap(Dictionary<Vector3Int, string> map) => gridToInstanceMap3D = map;
    public void SetGoalInstanceMap(Dictionary<Vector3Int, string> map) => goalObjectMap3D = map;


    public MapData()
    {
        objectInstances = new Dictionary<string, ObjDataInstance>();
        tileRotations = new Dictionary<Vector2Int, Vector3>();
        gridToInstanceMap3D = new Dictionary<Vector3Int, string>();
        goalObjectMap3D = new Dictionary<Vector3Int, string>();
    }

    public bool IsInitialized()
    {
        return tileIDs != null && tileIDs.Length > 0;
    }

    [Button("Initialize Grid")]
    public void Initialize()
    {
        tileIDs = new int[gridLength * gridWidth];
        gridMin = Vector2Int.zero;
        objectInstances.Clear();
        tileRotations.Clear();
        tileWalls.Clear();
        gridToInstanceMap3D.Clear();
        goalObjectMap3D.Clear();
        if (string.IsNullOrEmpty(mapID))
        {
            mapID = System.Guid.NewGuid().ToString();
        }

    }

    [Button("Save Data")]
    public void SaveData() //need change, maybe save as json file 
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    public MapData DeepCopy()
    {
        MapData copy = ScriptableObject.CreateInstance<MapData>();
        copy.mapID = this.mapID;
        copy.gridLength = this.gridLength;
        copy.gridWidth = this.gridWidth;
        copy.gridMin = this.gridMin;
        copy.cellSize = this.cellSize;
        copy.tileDataList = this.tileDataList;
        copy.objDataList = this.objDataList;

        if (this.tileIDs != null)
        {
            copy.tileIDs = (int[])this.tileIDs.Clone();
        }
        else
        {
            copy.Initialize();
        }

        copy.tileRotations = new Dictionary<Vector2Int, Vector3>(this.tileRotations);
        copy.gridToInstanceMap3D = new Dictionary<Vector3Int, string>();
        copy.goalObjectMap3D = new Dictionary<Vector3Int, string>();
        copy.objectInstances = new Dictionary<string, ObjDataInstance>();

        copy.tileWalls = new Dictionary<Vector2Int, Dictionary<WallDirection, int>>();



        foreach (var kvp in this.tileWalls)
        {
            copy.tileWalls[kvp.Key] = new Dictionary<WallDirection, int>(kvp.Value);
        }

        foreach (var kvp in this.objectInstances)
        {
            copy.objectInstances[kvp.Key] = kvp.Value.DeepCopy();
        }

        foreach (var kvp in this.gridToInstanceMap3D)
        {
            copy.gridToInstanceMap3D[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in this.goalObjectMap3D)
        {
            copy.goalObjectMap3D[kvp.Key] = kvp.Value;
        }



        Debug.Log($"[MapData] Deep copied map {mapID} with {objectInstances.Count} objects");
        return copy;
    }


    #region  tile


    public void SetTileID(Vector2Int gridPosition, int tileID)
    {
        if (IsWithinGrid(gridPosition))
        {
            int index = (gridPosition.y - gridMin.y) * gridLength + (gridPosition.x - gridMin.x);
            this.tileIDs[index] = tileID;
        }
    }

    public void SetTileRotation(Vector2Int gridPosition, Quaternion rotation)
    {
        tileRotations[gridPosition] = rotation.eulerAngles;
    }

    public Vector3 GetTileRotation(Vector2Int gridPosition)
    {

        return tileRotations.ContainsKey(gridPosition) ? tileRotations[gridPosition] : Vector3.zero;
    }

    public int GetTileID(Vector2Int gridPosition)
    {
        if (IsWithinGrid(gridPosition))
        {
            int index = (gridPosition.y - gridMin.y) * gridLength + (gridPosition.x - gridMin.x);
            return tileIDs[index];
        }
        return -1;
    }

    public bool CanPlaceTile(Vector2Int gridPosition, TileData newtileData)
    {
        if (!IsWithinGrid(gridPosition))
            return false;

        Vector3Int pos3D = new Vector3Int(gridPosition.x, gridPosition.y, 0);
        string instanceID = GetInstanceIDAtGrid(pos3D);

        if (string.IsNullOrEmpty(instanceID))
            return true;


        return newtileData.tileType.HasFlag(TileType.Placeable);
    }



    public bool HasTileType(Vector2Int gridPos, TileType type)
    {
        if (!IsWithinGrid(gridPos))
            return false;

        if (tileDataList == null)
        {
            Debug.LogError($"[MapData] tileDataList is null on map {mapID}");
            return false;
        }

        int tileID = GetTileID(gridPos);
        TileData tileData = tileDataList.GetTileDetails(tileID);

        if (tileData == null)
            return false;

        return tileData.tileType.HasFlag(type);
    }



    #endregion


    #region Obj

    public ObjData GetObjectDataAtGrid(Vector3Int gridPosition)
    {
        if (gridToInstanceMap3D.TryGetValue(gridPosition, out string instanceID))
        {
            if (objectInstances.TryGetValue(instanceID, out var objInstance))
            {
                return objDataList.GetObjDetails(objInstance.objID);
            }
        }
        if (goalObjectMap3D.TryGetValue(gridPosition, out string goalInstanceID))
        {
            if (objectInstances.TryGetValue(goalInstanceID, out ObjDataInstance goalInstance))
            {
                return objDataList.GetObjDetails(goalInstance.objID);
            }
        }
        return null;
    }

    public string GetGoalIDAtGrid(Vector3Int gridPosition)
    {
        if (goalObjectMap3D.TryGetValue(gridPosition, out string goalInstanceID))
        {
            if (string.IsNullOrEmpty(goalInstanceID))
            {
                Debug.LogError($"[MapData] Found empty instance ID at position {gridPosition}");
                return null;
            }
            return goalInstanceID;
        }
        return null;
    }

    public string GetInstanceIDAtGrid(Vector3Int gridPosition)
    {
        if (gridToInstanceMap3D.TryGetValue(gridPosition, out string instanceID))
        {
            if (string.IsNullOrEmpty(instanceID))
            {
                Debug.LogError($"[MapData] Found empty instance ID at position {gridPosition}");
                return null;
            }
            return instanceID;
        }

        return GetGoalIDAtGrid(gridPosition);
    }

    private bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        Vector2Int delta = b - a;
        return (delta == Vector2Int.right || delta == Vector2Int.left ||
                delta == Vector2Int.up || delta == Vector2Int.down);
    }



    public bool CanPlaceObj(Vector2Int mainGrid, int objID, int height, Quaternion rotation)
    {
        if (!objDataList) return false;

        ObjData objData = objDataList.GetObjDetails(objID);
        if (objData == null) return false;
        bool isGoal = objData.objType.HasFlag(ObjectType.Goal);
        bool isPlayer = objData.objType.HasFlag(ObjectType.Player);

        ObjDataInstance tempInstance = new ObjDataInstance(objID, mainGrid, height, rotation, objData);

        foreach (var grid in tempInstance.currentOccupied)
        {
            // Pomme may only be placed on a tile marked Walkable. Goal markers
            // are stored separately from solid occupants, so they can share
            // the tile with the player.
            if (isPlayer && !HasTileType(new Vector2Int(grid.x, grid.y), TileType.Walkable))
            {
                return false;
            }

            if (!CanPlaceAtGrid(grid, isGoal))
            {
                return false;
            }
        }

        if (tempInstance.currentOccupied.Count > 1)
        {
            for (int i = 0; i < tempInstance.currentOccupied.Count; i++)
            {
                Vector2Int a = new Vector2Int(tempInstance.currentOccupied[i].x, tempInstance.currentOccupied[i].y);
                for (int j = i + 1; j < tempInstance.currentOccupied.Count; j++)
                {

                    Vector2Int b = new Vector2Int(tempInstance.currentOccupied[j].x, tempInstance.currentOccupied[j].y);
                    if (IsAdjacent(a, b) && HasWallBetween(a, b))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public bool CanPlaceAtGrid(Vector3Int gridPosition, bool isGoal = false)
    {
        Vector2Int baseGrid = new Vector2Int(gridPosition.x, gridPosition.y);
        if (!IsWithinGrid(baseGrid))
        {
            return false;
        }

        if (gridPosition.z == 0)
        {
            if (!HasTileAt(baseGrid))
            {
                return false;
            }
        }

        if (gridToInstanceMap3D.ContainsKey(gridPosition))
        {
            return false;
        }

        if (isGoal)
        {
            return !goalObjectMap3D.ContainsKey(gridPosition);
        }
        return true;

    }

    public bool CanMoveToGrid(Vector3Int gridPosition, string instanceID)
    {
        Vector2Int baseGrid = new Vector2Int(gridPosition.x, gridPosition.y);
        if (!IsWithinGrid(baseGrid))
        {
            return false;
        }

        if (gridPosition.z == 0)
        {
            if (!HasTileAt(baseGrid))
            {
                return false;
            }
        }

        if (!gridToInstanceMap3D.ContainsKey(gridPosition))
        {
            return true;
        }

        if (gridToInstanceMap3D.TryGetValue(gridPosition, out string existingInstanceID))
        {
            return existingInstanceID == instanceID;
        }

        return false;
    }

    public bool CanWalkGrid(Vector3Int gridPosition)
    {
        if (gridPosition.z == 0)
        {
            if (!HasTileAt(new Vector2Int(gridPosition.x, gridPosition.y)))
            {
                return false;
            }
        }
        return true;
    }


    public string PlaceObject(Vector2Int mainGrid, ObjData objData, int height, Quaternion rotation)
    {
        if (objData == null)
        {
            Debug.LogError("[MapData] Attempted to place null ObjData");
            return null;
        }

        ObjDataInstance newInstance = new ObjDataInstance(objData.objID, mainGrid, height, rotation, objData);
        string instanceID = newInstance.instanceID;

        if (string.IsNullOrEmpty(instanceID))
        {
            Debug.LogError("[MapData] Generated instance ID is null or empty");
            return null;
        }

        objectInstances[instanceID] = newInstance;

        if (objData.objType.HasFlag(ObjectType.Goal))
        {
            foreach (var grid in newInstance.currentOccupied)
            {
                goalObjectMap3D[grid] = instanceID;
            }
        }
        else
        {
            foreach (var grid in newInstance.currentOccupied)
            {
                gridToInstanceMap3D[grid] = instanceID;
            }
        }
        Debug.Log($"[MapData] Created new object {objData.objID} at {mainGrid}, height {height}, instance ID: {instanceID}");
        return instanceID;
    }

    public void RemoveObject(Vector3Int gridPosition)
    {
        if (gridToInstanceMap3D.TryGetValue(gridPosition, out string instanceID))
        {
            if (objectInstances.TryGetValue(instanceID, out ObjDataInstance objInstance))
            {
                foreach (var occupiedGrid in objInstance.currentOccupied)
                {
                    gridToInstanceMap3D.Remove(occupiedGrid);
                }
                objectInstances.Remove(instanceID);
            }
        }

        if (goalObjectMap3D.TryGetValue(gridPosition, out string goalInstanceID))
        {
            if (objectInstances.TryGetValue(goalInstanceID, out ObjDataInstance goalInstance))
            {
                foreach (var occupiedGrid in goalInstance.currentOccupied)
                {
                    goalObjectMap3D.Remove(occupiedGrid);
                }
                objectInstances.Remove(goalInstanceID);
            }
        }
    }
    #endregion


    #region walls

    private Vector2Int GetNeighbor(Vector2Int pos, WallDirection dir)
    {
        return dir switch
        {
            WallDirection.Up => pos + Vector2Int.up,
            WallDirection.Down => pos + Vector2Int.down,
            WallDirection.Left => pos + Vector2Int.left,
            WallDirection.Right => pos + Vector2Int.right,
            _ => pos
        };
    }

    private WallDirection GetOpposite(WallDirection dir)
    {
        return dir switch
        {
            WallDirection.Up => WallDirection.Down,
            WallDirection.Down => WallDirection.Up,
            WallDirection.Left => WallDirection.Right,
            WallDirection.Right => WallDirection.Left,
            _ => dir
        };
    }


    public void GetNormalizedWallPosition(Vector2Int gridPos, WallDirection direction, out Vector2Int basePos, out WallDirection baseDir)
    {
        switch (direction)
        {
            case WallDirection.Left:
                basePos = gridPos + Vector2Int.left;
                baseDir = WallDirection.Right;
                break;
            case WallDirection.Down:
                basePos = gridPos + Vector2Int.down;
                baseDir = WallDirection.Up;
                break;
            default:
                basePos = gridPos;
                baseDir = direction;
                break;
        }
    }

    public bool CanPlaceWall(Vector2Int gridPos, WallDirection direction)
    {
        Vector2Int otherPos = GetNeighbor(gridPos, direction);

        // A wall belongs to the shared edge between two cells. At least one
        // side must have a placed tile, which also permits walls around the
        // outer edge of a tiled area.
        if (!HasTileAt(gridPos) && !HasTileAt(otherPos)) return false;

        Vector3Int pos3D = new Vector3Int(gridPos.x, gridPos.y, 0);
        string instanceID = GetInstanceIDAtGrid(pos3D);
        Vector3Int pos3D1 = new Vector3Int(otherPos.x, otherPos.y, 0);
        string instanceID1 = GetInstanceIDAtGrid(pos3D1);

        if (!string.IsNullOrEmpty(instanceID) && instanceID == instanceID1)
        {
            return false;
        }

        return true;
    }

    public bool HasTileAt(Vector2Int gridPos)
    {
        int tileID = GetTileID(gridPos);
        return tileID != 0 && tileID != -1;
    }


    public void SetWall(Vector2Int gridPos, WallDirection direction, int wallID)
    {
        GetNormalizedWallPosition(gridPos, direction, out var basePos, out var baseDir);

        Vector2Int neighborPos = GetNeighbor(basePos, baseDir);
        WallDirection mirroredDirection = GetOpposite(baseDir);
        if (tileWalls.TryGetValue(neighborPos, out var mirroredWalls))
        {
            mirroredWalls.Remove(mirroredDirection);
            if (mirroredWalls.Count == 0) tileWalls.Remove(neighborPos);
        }

        if (!tileWalls.ContainsKey(basePos))
        {
            tileWalls[basePos] = new Dictionary<WallDirection, int>();
        }

        tileWalls[basePos][baseDir] = wallID;
    }

    public int GetWall(Vector2Int gridPos, WallDirection direction)
    {
        GetNormalizedWallPosition(gridPos, direction, out var basePos, out var baseDir);

        if (tileWalls.TryGetValue(basePos, out var dirDict))
        {
            if (dirDict.TryGetValue(baseDir, out var wallID))
            {
                return wallID;
            }
        }

        // Read older maps that may have stored the shared edge from its other side.
        Vector2Int neighborPos = GetNeighbor(basePos, baseDir);
        if (tileWalls.TryGetValue(neighborPos, out var neighborWalls) &&
            neighborWalls.TryGetValue(GetOpposite(baseDir), out var mirroredWallID))
        {
            return mirroredWallID;
        }

        return -1;//no walls
    }

    public Dictionary<WallDirection, int> GetAllWalls(Vector2Int gridPos)
    {
        if (tileWalls.TryGetValue(gridPos, out var walls))
        {
            return new Dictionary<WallDirection, int>(walls);
        }
        return new Dictionary<WallDirection, int>();
    }

    public void RemoveWall(Vector2Int gridPos, WallDirection direction)
    {
        GetNormalizedWallPosition(gridPos, direction, out var basePos, out var baseDir);

        if (tileWalls.TryGetValue(basePos, out var dirDict))
        {
            dirDict.Remove(baseDir);
            if (dirDict.Count == 0)
            {
                tileWalls.Remove(basePos);
            }
        }

        Vector2Int neighborPos = GetNeighbor(basePos, baseDir);
        if (tileWalls.TryGetValue(neighborPos, out var neighborWalls))
        {
            neighborWalls.Remove(GetOpposite(baseDir));
            if (neighborWalls.Count == 0) tileWalls.Remove(neighborPos);
        }
    }

    public bool HasWallBetween(Vector2Int a, Vector2Int b)
    {
        Vector2Int delta = b - a;

        if (delta == Vector2Int.right) return GetWall(a, WallDirection.Right) != -1;
        if (delta == Vector2Int.left) return GetWall(b, WallDirection.Right) != -1;
        if (delta == Vector2Int.up) return GetWall(a, WallDirection.Up) != -1;
        if (delta == Vector2Int.down) return GetWall(b, WallDirection.Up) != -1;

        return false;
    }


    #endregion

    #region other
    public bool IsWithinGrid(Vector2Int gridPosition)
    {
        return gridPosition.x >= gridMin.x && gridPosition.x < gridMin.x + gridLength &&
               gridPosition.y >= gridMin.y && gridPosition.y < gridMin.y + gridWidth;
    }

    /// <summary>Expands the editable grid while preserving all existing tile data.</summary>
    public bool EnsureGridContains(Vector2Int gridPosition)
    {
        if (IsWithinGrid(gridPosition)) return false;
        if (tileIDs == null)
        {
            tileIDs = new int[gridLength * gridWidth];
        }

        int newMinX = Mathf.Min(gridMin.x, gridPosition.x);
        int newMinY = Mathf.Min(gridMin.y, gridPosition.y);
        int newMaxX = Mathf.Max(gridMin.x + gridLength - 1, gridPosition.x);
        int newMaxY = Mathf.Max(gridMin.y + gridWidth - 1, gridPosition.y);
        int newLength = newMaxX - newMinX + 1;
        int newWidth = newMaxY - newMinY + 1;
        int[] expanded = new int[newLength * newWidth];

        for (int y = 0; y < gridWidth; y++)
        {
            for (int x = 0; x < gridLength; x++)
            {
                int oldIndex = y * gridLength + x;
                int newX = gridMin.x + x - newMinX;
                int newY = gridMin.y + y - newMinY;
                expanded[newY * newLength + newX] = tileIDs[oldIndex];
            }
        }

        gridMin = new Vector2Int(newMinX, newMinY);
        gridLength = newLength;
        gridWidth = newWidth;
        tileIDs = expanded;
        return true;
    }

    public IEnumerable<ObjDataInstance> GetAllObjectInstances()
    {
        foreach (var instance in objectInstances.Values)
        {
            if (instance == null)
            {
                Debug.LogError("[MapData] Found null instance in objectInstances");
                continue;
            }
            yield return instance;
        }
    }

    public ObjData GetObjectData(int objID)
    {
        return objDataList.GetObjDetails(objID);
    }

    public ObjDataInstance GetInstance(string instanceID)
    {
        if (objectInstances.TryGetValue(instanceID, out var instance))
        {
            return instance;
        }
        return null;
    }

    public ObjDataInstance GetObjDataInstanceAtGrid(Vector3Int gridPosition)
    {
        string instanceID = GetInstanceIDAtGrid(gridPosition);
        if (string.IsNullOrEmpty(instanceID))
        {
            return null;
        }

        ObjDataInstance instance = GetInstance(instanceID);
        return instance;
    }

    public void RemoveGridMapping(Vector3Int grid)
    {
        //gridToInstanceMap3D.Remove(grid);
        bool removed = gridToInstanceMap3D.Remove(grid);
        // if (removed)
        //     Debug.Log("Removed successfully.");
        // else
        //     Debug.Log("Grid not found.");
        string instanceID = GetInstanceIDAtGrid(grid);
        if (string.IsNullOrEmpty(instanceID))
        {
            Debug.Log(grid + " noObj");
        }
        else
        {
            Debug.Log(grid + " had object with ID: " + instanceID);
        }
    }

    public void UpdateGridMapping(Vector3Int grid, string instanceID)
    {
        gridToInstanceMap3D[grid] = instanceID;
        Debug.Log(grid);
    }
    #endregion

}
