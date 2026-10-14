using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Linq;
using System.Collections;

public class MapPlane : MonoBehaviour
{
    public TileDataList_SO tileDataList;
    public ObjDataList_SO objDataList;
    public WallDataList_SO wallDataList;
    private MapData originalMapData;
    public GameObject planePreview;
    public GameObject objs;
    public MapData mapData
    {
        get
        {
            if (isEditMode || originalMapData == null)
            {
                return originalMapData;
            }

            if (string.IsNullOrEmpty(originalMapData.mapID))
            {
                Debug.LogError($"[MapPlane] Map ID is null or empty on {gameObject.name}");
                return originalMapData;
            }

            var runtimeData = MapMgr.Instance.GetRuntimeMapData(originalMapData.mapID);
            if (runtimeData == null)
            {
                Debug.LogError($"[MapPlane] Failed to get runtime data for map ID: {originalMapData.mapID}");
                return originalMapData;
            }

            // Validate runtime data
            if (runtimeData.GetAllObjectInstances().Count() != originalMapData.GetAllObjectInstances().Count())
            {
                Debug.LogError($"[MapPlane] Runtime data has different object count ({runtimeData.GetAllObjectInstances().Count()}) compared to original data ({originalMapData.GetAllObjectInstances().Count()})");
            }

            return runtimeData;
        }
        set { originalMapData = value; }
    }
    public LevelData levelData;
    public Transform startPoint;
    public bool isEditMode = false;
    private Dictionary<Vector2Int, GameObject> gridTiles = new Dictionary<Vector2Int, GameObject>();
    public List<GameObject> spawnedObjects = new List<GameObject>();
    public Dictionary<string, GameObject> instanceToGameObject = new Dictionary<string, GameObject>();  // instanceID -> GameObject
    public List<Vector2Int> lightTrail = new List<Vector2Int>();

    private Dictionary<(Vector2Int, WallDirection), GameObject> wallObjects = new();

    [SerializeField] private bool useLoadAnimation = false;
    [SerializeField] private float tileLoadDelay = 0.02f;
    [SerializeField] private float wallLoadDelay = 0.02f;

    public void LoadMap()
    {
        if (!ValidateDataReferences())
        {
            return;
        }

        if (isEditMode && !ValidateEditorMode())
        {
            return;
        }

        if (originalMapData == null)
        {
            Debug.LogError($"[MapPlane] mapData is null on {gameObject.name}");
            return;
        }

        mapData.tileDataList = tileDataList;
        mapData.objDataList = objDataList;
        mapData.wallDataList = wallDataList;

        StopAllCoroutines();
        StartCoroutine(LoadMapRoutine());
    }

    private IEnumerator LoadMapRoutine()
    {
        if (useLoadAnimation)
        {
            GameMgr.Instance.EnterBlockMove();
            yield return StartCoroutine(LoadTilesWithAnimation());
            yield return StartCoroutine(LoadWallsWithAnimation());
            yield return StartCoroutine(LoadObjectsWithAnimation());
            GameMgr.Instance.ResetBlockMove();
        }
        else
        {
            LoadTiles();
            LoadWalls();
            LoadObjects();
        }
        MapMgr.Instance.FinishLoadOneMapPlane();

        yield return new WaitForSeconds(0);
    }

    #region  loadAnim
    private IEnumerator LoadTilesWithAnimation()
    {
        for (int x = 0; x < mapData.gridLength; x++)
        {
            for (int y = 0; y < mapData.gridWidth; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                int tileID = mapData.GetTileID(gridPosition);

                if (tileID != 0 && tileID != -1)
                {
                    TileData tileData = tileDataList.GetTileDetails(tileID);
                    if (tileData != null)
                    {
                        GameObject tile = PlaceTile(gridPosition, tileData, Quaternion.Euler(mapData.GetTileRotation(gridPosition)));

                        if (tile != null)
                        {
                            float delay = UnityEngine.Random.Range(0f, 0.3f);
                            StartCoroutine(TileJumpEffect(tile, delay));
                        }
                    }
                }
                yield return new WaitForSeconds(tileLoadDelay);
            }
        }
    }

    private IEnumerator LoadWallsWithAnimation()
    {
        for (int x = -1; x <= mapData.gridLength; x++)
        {
            for (int y = -1; y <= mapData.gridWidth; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                Dictionary<WallDirection, int> wallDict = mapData.GetAllWalls(gridPosition);

                foreach (var dirKvp in wallDict)
                {
                    WallDirection direction = dirKvp.Key;
                    int wallID = dirKvp.Value;

                    if (wallID == -1) continue;

                    WallData wallData = wallDataList.GetTileDetails(wallID);
                    if (wallData != null)
                    {
                        GameObject wall = PlaceWall(gridPosition, wallData, direction);
                        if (wall != null)
                        {
                            float delay = UnityEngine.Random.Range(0f, 0.3f);
                            StartCoroutine(TileJumpEffect(wall, delay));
                        }
                        yield return new WaitForSeconds(wallLoadDelay);
                    }
                }

            }

        }
    }

    public IEnumerator LoadObjectsWithAnimation()
    {
        Debug.Log($"[MapPlane] Starting to load objects for map {mapData.mapID}");

        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
        instanceToGameObject.Clear();

        var instances = mapData.GetAllObjectInstances().ToList();
        Debug.Log($"[MapPlane] Found {instances.Count} objects to load");

        foreach (var instance in instances)
        {
            Vector2Int gridPosition = instance.mainGrid;
            ObjData objData = mapData.GetObjectData(instance.objID);

            if (objData == null)
            {
                Debug.LogError($"[MapPlane] Could not find object data for ID {instance.objID}");
                continue;
            }

            if (objData.prefab == null)
            {
                Debug.LogError($"[MapPlane] Prefab is null for object ID {objData.objID}");
                continue;
            }

            Vector3 worldPosition = GridToWorldPosition(gridPosition);
            worldPosition.y = instance.height * mapData.cellSize;

            GameObject objInstance = Instantiate(objData.prefab, worldPosition, instance.rotation);
            Vector3 preScale = objInstance.transform.localScale;
            objInstance.transform.localScale = Vector3.zero;
            objInstance.transform.parent = objs.transform;

            spawnedObjects.Add(objInstance);
            instanceToGameObject[instance.instanceID] = objInstance;

            float delay = UnityEngine.Random.Range(0f, 0.2f);
            StartCoroutine(PlayObjectAppearAnimation(objInstance, delay, preScale));

            if (objData.objType.HasFlag(ObjectType.Player))
            {
                Debug.Log($"[MapPlane] Found player object at {gridPosition}, instanceID {instance.instanceID}");

            }


        }

        yield return new WaitForSeconds(0.5f);

        Debug.Log($"[MapPlane] Finished loading objects. Loaded {spawnedObjects.Count} objects, Dictionary size: {instanceToGameObject.Count}");
    }

    public void SetIndiCator(ObjDataInstance instance)
    {
        GameObject move = GetObjectByInstanceID(instance.instanceID);
        MovableObj movableObj = move.GetComponent<MovableObj>();
        if (movableObj)
        {
            if (movableObj.useIndicator)
            {
                movableObj.MoveIndicator(instance.mainGrid,0.01f);
            }
        }
    }

    public void LoadTrail()
    {
        var instances = mapData.GetAllObjectInstances().ToList();
        foreach (var instance in instances)
        {
            Vector2Int gridPosition = instance.mainGrid;
            ObjData objData = mapData.GetObjectData(instance.objID);

            if (objData.objType.HasFlag(ObjectType.TrailAsObstacle))
            {
                TrailObj trailObj = objData as TrailObj;
                GameMgr.Instance.SetBlockingMaxStep(instance.instanceID, trailObj.stepMax);
                List<Vector3Int> vector3s = new List<Vector3Int>();
                GameMgr.Instance.AddTrailStep(instance.instanceID, instance.currentOccupied, out vector3s);

            }

            if (objData.objType.HasFlag(ObjectType.TrailAsRoad))
            {
                TrailObj trailObj = objData as TrailObj;
                GameMgr.Instance.SetAccseeibleMaxStep(instance.instanceID, trailObj.stepMax);

                List<Vector3Int> vector3s = new List<Vector3Int>();
                GameMgr.Instance.RecordPlayerStep(instance.instanceID, instance.currentOccupied, out vector3s);
                

            }
            SetIndiCator(instance);
        }
    }
    private IEnumerator PlayObjectAppearAnimation(GameObject obj, float delay, Vector3 finalScale)
    {
        yield return new WaitForSeconds(delay);

        float duration = 0.2f;
        float elapsed = 0f;

        Vector3 startScale = Vector3.zero;
        Vector3 endScale = finalScale;

        while (elapsed < duration)
        {
            obj.transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        obj.transform.localScale = endScale;
    }


    // private IEnumerator PlayBounceAnimation(GameObject obj)
    // {
    //     Vector3 startPos = obj.transform.localPosition;
    //     Vector3 endPos = startPos + Vector3.up * bounceHeight;

    //     float timer = 0f;
    //     while (timer < bounceDuration)
    //     {
    //         float t = timer / bounceDuration;
    //         float curveValue = tileBounceCurve.Evaluate(t);
    //         obj.transform.localPosition = Vector3.LerpUnclamped(startPos, endPos, curveValue);
    //         timer += Time.deltaTime;
    //         yield return null;
    //     }

    //     obj.transform.localPosition = startPos;
    //     yield return new WaitForSeconds(0);
    // }

    private IEnumerator TileJumpEffect(GameObject tile, float delay)
    {
        yield return new WaitForSeconds(delay);

        Vector3 originalPos = tile.transform.position;
        float jumpHeight = 0.2f;
        float duration = 0.15f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float yOffset = Mathf.Sin(elapsed / duration * Mathf.PI) * jumpHeight;
            tile.transform.position = originalPos + new Vector3(0, yOffset, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        tile.transform.position = originalPos;
    }



    #endregion

    private bool ValidateDataReferences()
    {
        if (tileDataList == null || objDataList == null || wallDataList == null)
        {
            Debug.LogError($"[MapPlane] tileDataList or objDataList is null on {gameObject.name}");
            return false;
        }
        return true;
    }

    private bool ValidateEditorMode()
    {
        if (levelData == null)
        {
            Debug.LogError($"[MapPlane] levelData is null on {gameObject.name} in edit mode");
            return false;
        }
        return true;
    }

    public void SetPreViewScale(Vector3 scale)
    {
        planePreview.transform.localScale = scale;
    }



    #region  tile
    private void LoadTiles()
    {
        for (int x = 0; x < mapData.gridLength; x++)
        {
            for (int y = 0; y < mapData.gridWidth; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                int tileID = mapData.GetTileID(gridPosition);

                if (tileID != 0 && tileID != -1)
                {
                    TileData tileData = tileDataList.GetTileDetails(tileID);
                    if (tileData != null)
                    {
                        PlaceTile(gridPosition, tileData, Quaternion.Euler(mapData.GetTileRotation(gridPosition)));
                    }
                }
            }
        }
    }

    public GameObject GetTileAtGridPosition(Vector2Int gridPosition)
    {
        if (gridTiles.ContainsKey(gridPosition))
        {
            return gridTiles[gridPosition];
        }
        return null;
    }

    public TileData GetTileDataAtGridPosition(Vector2Int gridPosition)
    {
        if (gridTiles.ContainsKey(gridPosition))
        {
            int tileID = mapData.GetTileID(gridPosition);
            return tileDataList.GetTileDetails(tileID);
        }
        return null;
    }


    public int GetTileID(Vector2Int gridPosition)
    {
        return mapData.GetTileID(gridPosition);
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x - startPoint.position.x) / mapData.cellSize);
        int y = Mathf.FloorToInt((worldPosition.z - startPoint.position.z) / mapData.cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        float x = (gridPosition.x + 0.5f) * mapData.cellSize + startPoint.position.x;
        float y = (gridPosition.y + 0.5f) * mapData.cellSize + startPoint.position.z;
        return new Vector3(x, 0, y);
    }

    public float GetCellSize()
    {
        return mapData.cellSize;
    }

    public void RemoveTile(Vector2Int gridPosition)
    {
        GameObject existingTile = GetTileAtGridPosition(gridPosition);
        if (existingTile != null)
        {
            mapData.SetTileID(gridPosition, 0);
            gridTiles.Remove(gridPosition);
            mapData.SetTileRotation(gridPosition, Quaternion.identity);
            Destroy(existingTile);
        }
    }


    public GameObject PlaceTile(Vector2Int gridPosition, TileData tileData, Quaternion quaternion)
    {
        if (!mapData.IsWithinGrid(gridPosition))
        {
            return null;
        }

        GameObject existingTile = GetTileAtGridPosition(gridPosition);
        if (existingTile != null)
        {
            Destroy(existingTile);
            gridTiles.Remove(gridPosition);
        }

        mapData.SetTileID(gridPosition, tileData.tileID);
        mapData.SetTileRotation(gridPosition, quaternion);

        Vector3 worldPosition = GridToWorldPosition(gridPosition);

        GameObject tileInstance = Instantiate(tileData.prefab, worldPosition + tileData.startOffset, quaternion);
        gridTiles[gridPosition] = tileInstance;
        tileInstance.transform.parent = this.transform;
        return tileInstance;
    }


    #endregion

    #region  wall

    private Quaternion GetRotationFromDirection(WallDirection direction)
    {
        return direction switch
        {
            WallDirection.Up => Quaternion.Euler(0, 0, 0),
            WallDirection.Right => Quaternion.Euler(0, 90, 0),
            WallDirection.Down => Quaternion.Euler(0, 180, 0),
            WallDirection.Left => Quaternion.Euler(0, 270, 0),
            _ => Quaternion.identity,
        };
    }

    private Vector3 GetOffsetFromDirection(WallDirection direction, Vector3 wallOffset)
    {
        float halfCell = mapData.cellSize / 2f;

        return direction switch
        {
            WallDirection.Up => new Vector3(0, 0, halfCell),
            WallDirection.Right => new Vector3(halfCell, 0, 0),
            WallDirection.Down => new Vector3(0, 0, -halfCell),
            WallDirection.Left => new Vector3(-halfCell, 0, 0),
            _ => Vector3.zero,
        } + wallOffset;
    }

    private void LoadWalls()
    {
        for (int x = -1; x <= mapData.gridLength; x++)
        {
            for (int y = -1; y <= mapData.gridWidth; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                Dictionary<WallDirection, int> wallDict = mapData.GetAllWalls(gridPosition);

                foreach (var dirKvp in wallDict)
                {
                    WallDirection direction = dirKvp.Key;
                    int wallID = dirKvp.Value;

                    if (wallID == -1) continue;

                    WallData wallData = wallDataList.GetTileDetails(wallID);
                    if (wallData != null)
                    {
                        PlaceWall(gridPosition, wallData, direction);
                    }
                }
            }
        }
    }

    public GameObject PlaceWall(Vector2Int gridPosition, WallData wallData, WallDirection wallDirection)
    {
        var key = (gridPosition, wallDirection);
        if (wallObjects.TryGetValue(key, out var existingWall))
        {
            Destroy(existingWall);
            wallObjects.Remove(key);
        }
        mapData.SetWall(gridPosition, wallDirection, wallData.wallID);

        Vector3 basePosition = GridToWorldPosition(gridPosition);
        Quaternion rotation = GetRotationFromDirection(wallDirection);
        Vector3 offset = GetOffsetFromDirection(wallDirection, wallData.startOffset);
        GameObject wallInstance = Instantiate(wallData.prefab, basePosition + offset, rotation);
        wallInstance.transform.parent = this.transform;
        wallObjects[key] = wallInstance;

        return wallInstance;
    }

    public void RemoveWall(Vector2Int gridPosition, WallDirection wallDirection)
    {
        var key = (gridPosition, wallDirection);

        if (wallObjects.TryGetValue(key, out var wallObj))
        {
            Destroy(wallObj);
            wallObjects.Remove(key);
        }

        mapData.RemoveWall(gridPosition, wallDirection);
    }

    #endregion



    #region item


    public GameObject GetObjectAtGridPosition(Vector2Int gridPosition, int height = 0)
    {
        Vector3Int grid3D = new Vector3Int(gridPosition.x, gridPosition.y, height);
        string instanceID = mapData.GetInstanceIDAtGrid(grid3D);

        if (string.IsNullOrEmpty(instanceID))
        {
            Debug.Log($"[MapPlane] No instance ID found at position {gridPosition}, height {height}");
            return null;
        }

        if (!instanceToGameObject.TryGetValue(instanceID, out GameObject obj))
        {
            Debug.LogError($"[MapPlane] Instance ID {instanceID} found but no corresponding GameObject at {gridPosition}, height {height}");
            return null;
        }

        return obj;
    }

    public void RemoveObject(Vector2Int gridPosition, int height = 0)
    {
        Vector3Int grid3D = new Vector3Int(gridPosition.x, gridPosition.y, height);
        string instanceID = mapData.GetInstanceIDAtGrid(grid3D);
        if (!string.IsNullOrEmpty(instanceID) && instanceToGameObject.TryGetValue(instanceID, out GameObject existingObject))
        {
            if (isEditMode && levelData != null)
            {
                ObjData objData = GetObjectDataAtGridPosition(gridPosition, height);
                if (objData != null)
                {
                    levelData.UpdateObjectCount(objData, false);
                }
            }

            mapData.RemoveObject(grid3D);
            spawnedObjects.Remove(existingObject);
            instanceToGameObject.Remove(instanceID);
            Destroy(existingObject);
        }
    }

    public GameObject PlaceObject(Vector2Int gridPosition, ObjData objData, Quaternion quaternion, int height = 0, Vector3 scale = default)
    {
        if (!mapData.IsWithinGrid(gridPosition))
        {
            return null;
        }

        if (!mapData.CanPlaceObj(gridPosition, objData.objID, height, quaternion))
        {
            return null;
        }

        if (objData.maxCount == 1 || objData.objType.HasFlag(ObjectType.Player))
        {
            GameObject existingUniqueObj = null;
            string existingInstanceID = null;
            Vector3Int existingGrid3D = Vector3Int.zero;
            MapPlane sourceMap = null;

            if (levelData != null)
            {
                foreach (var map in levelData.insideWorld.Concat(levelData.outsideWorld))
                {
                    foreach (var instance in map.GetAllObjectInstances())
                    {
                        if (instance.objID == objData.objID)
                        {
                            existingInstanceID = instance.instanceID;
                            existingGrid3D = new Vector3Int(instance.mainGrid.x, instance.mainGrid.y, instance.height);
                            sourceMap = FindMapPlaneByMapData(map);
                            if (sourceMap != null)
                            {
                                existingUniqueObj = sourceMap.GetObjectAtGridPosition(instance.mainGrid, instance.height);
                            }
                            break;
                        }
                    }
                    if (existingUniqueObj != null) break;
                }
            }

            if (existingUniqueObj != null)
            {
                Vector3 worldPosition = GridToWorldPosition(gridPosition);
                worldPosition.y = height * mapData.cellSize;

                if (sourceMap != this)
                {
                    sourceMap.spawnedObjects.Remove(existingUniqueObj);
                    sourceMap.instanceToGameObject.Remove(existingInstanceID);
                    sourceMap.mapData.RemoveObject(existingGrid3D);
                    existingUniqueObj.transform.parent = objs.transform;
                    spawnedObjects.Add(existingUniqueObj);
                }

                existingUniqueObj.transform.position = worldPosition;
                existingUniqueObj.transform.rotation = quaternion;
                if (scale != default)
                {
                    existingUniqueObj.transform.localScale = scale;
                }

                string newInstanceID = mapData.PlaceObject(gridPosition, objData, height, quaternion);
                instanceToGameObject[newInstanceID] = existingUniqueObj;

                if (isEditMode && levelData != null && objData.objType.HasFlag(ObjectType.Player))
                {
                    levelData.playerStart.startMapID = mapData.mapID;
                    levelData.playerStart.startGridPos = gridPosition;
                }

                return existingUniqueObj;
            }
        }

        GameObject existingObject = GetObjectAtGridPosition(gridPosition, height);
        if (existingObject != null)
        {
            RemoveObject(gridPosition, height);
        }

        Vector3 newWorldPosition = GridToWorldPosition(gridPosition);
        newWorldPosition.y = height * mapData.cellSize;

        string instanceID = mapData.PlaceObject(gridPosition, objData, height, quaternion);
        GameObject objInstance = Instantiate(objData.prefab, newWorldPosition, quaternion);
        if (scale != default)
        {
            objInstance.transform.localScale = scale;
        }
        spawnedObjects.Add(objInstance);
        instanceToGameObject[instanceID] = objInstance;
        objInstance.transform.parent = objs.transform;

        if (isEditMode && levelData != null)
        {
            levelData.UpdateObjectCount(objData, true);
        }

        return objInstance;
    }

    private MapPlane FindMapPlaneByMapData(MapData mapData)
    {
        var mapMgr = MapMgr.Instance;
        if (mapMgr != null)
        {
            var mapPlanes = mapMgr.GetMapPlanes();
            foreach (var mapPlane in mapPlanes)
            {
                if (mapPlane.mapData == mapData)
                {
                    return mapPlane;
                }
            }
        }
        return null;
    }

    public ObjData GetObjectDataAtGridPosition(Vector2Int gridPosition, int height = 0)
    {
        Vector3Int grid3D = new Vector3Int(gridPosition.x, gridPosition.y, height);
        return mapData.GetObjectDataAtGrid(grid3D);
    }

    public void LoadObjects()
    {
        Debug.Log($"[MapPlane] Starting to load objects for map {mapData.mapID}");

        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
        instanceToGameObject.Clear();

        var instances = mapData.GetAllObjectInstances().ToList();
        Debug.Log($"[MapPlane] Found {instances.Count} objects to load");

        foreach (var instance in instances)
        {

            Vector2Int gridPosition = instance.mainGrid;
            ObjData objData = mapData.GetObjectData(instance.objID);

            if (objData != null)
            {
                Debug.Log($"[MapPlane] Loading object ID {objData.objID} at position {gridPosition}, height {instance.height}, instanceID {instance.instanceID}");
                if (objData.objType.HasFlag(ObjectType.Player))
                {
                    Debug.Log($"[MapPlane] Found player object at {gridPosition}, instanceID {instance.instanceID}");
                }

                if (objData.prefab == null)
                {
                    Debug.LogError($"[MapPlane] Prefab is null for object ID {objData.objID}");
                    continue;
                }

                Vector3 worldPosition = GridToWorldPosition(gridPosition);
                worldPosition.y = instance.height * mapData.cellSize;

                GameObject objInstance = Instantiate(objData.prefab, worldPosition, instance.rotation);


                if (objInstance == null)
                {
                    Debug.LogError($"[MapPlane] Failed to instantiate object ID {objData.objID}");
                    continue;
                }

                spawnedObjects.Add(objInstance);
                instanceToGameObject[instance.instanceID] = objInstance;
                objInstance.transform.parent = objs.transform;

                var loadedObj = GetObjectAtGridPosition(gridPosition, instance.height);
                if (loadedObj == null)
                {
                    Debug.LogError($"[MapPlane] Failed to retrieve object at {gridPosition} after placement!");
                    Debug.LogError($"[MapPlane] Instance ID: {instance.instanceID}, Object ID: {objData.objID}");
                    Debug.LogError($"[MapPlane] Dictionary contains key: {instanceToGameObject.ContainsKey(instance.instanceID)}");

                    if (!instanceToGameObject.ContainsKey(instance.instanceID))
                    {
                        Debug.Log($"[MapPlane] Attempting to fix by re-adding to dictionary");
                        instanceToGameObject[instance.instanceID] = objInstance;
                    }
                }
            }
            else
            {
                Debug.LogError($"[MapPlane] Could not find object data for ID {instance.objID}");
            }
        }

        foreach (var kvp in instanceToGameObject)
        {
            if (kvp.Value == null)
            {
                Debug.LogError($"[MapPlane] Found null GameObject for instance ID {kvp.Key}");
            }
        }

        Debug.Log($"[MapPlane] Finished loading objects. Loaded {spawnedObjects.Count} objects, Dictionary size: {instanceToGameObject.Count}");
    }

    public GameObject GetObjectByInstanceID(string instanceID)
    {
        if (instanceToGameObject.TryGetValue(instanceID, out GameObject obj))
        {
            return obj;
        }
        return null;
    }

    #endregion
}
