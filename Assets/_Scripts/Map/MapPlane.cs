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
        // The editor places content against the unbounded grid, so it does not
        // need the finite Plane preview used by runtime maps.
        if (isEditMode)
        {
            if (planePreview != null)
            {
                planePreview.SetActive(false);
            }

            var previewCollider = GetComponent<Collider>();
            if (previewCollider != null)
            {
                previewCollider.enabled = false;
            }
        }

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
        bool hasPlacedTile = false;
        for (int x = mapData.gridMin.x; x < mapData.gridMin.x + mapData.gridLength; x++)
        {
            for (int y = mapData.gridMin.y; y < mapData.gridMin.y + mapData.gridWidth; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                int tileID = mapData.GetTileID(gridPosition);

                if (tileID != 0 && tileID != -1)
                {
                    TileData tileData = tileDataList.GetTileDetails(tileID);
                    if (tileData != null)
                    {
                        // Show the first real tile immediately. Empty cells do
                        // not consume animation time; later tiles keep the
                        // original staggered loading cadence.
                        if (hasPlacedTile)
                            yield return new WaitForSeconds(tileLoadDelay);

                        GameObject tile = PlaceTile(gridPosition, tileData, Quaternion.Euler(mapData.GetTileRotation(gridPosition)));

                        if (tile != null)
                        {
                            hasPlacedTile = true;
                            float delay = UnityEngine.Random.Range(0f, 0.3f);
                            StartCoroutine(TileJumpEffect(tile, delay));
                        }
                    }
                }
            }
        }
    }

    private IEnumerator LoadWallsWithAnimation()
    {
        for (int x = mapData.gridMin.x - 1; x <= mapData.gridMin.x + mapData.gridLength; x++)
        {
            for (int y = mapData.gridMin.y - 1; y <= mapData.gridMin.y + mapData.gridWidth; y++)
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
                        GameObject wall = null;
                        if (mapData.CanPlaceWall(gridPosition, direction))
                            wall = PlaceWall(gridPosition, wallData, direction);
                        else
                            mapData.RemoveWall(gridPosition, direction);

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
            worldPosition.y = startPoint.position.y + instance.height * mapData.cellSize;
            if (instance.hasCustomWorldPosition) worldPosition = instance.customWorldPosition;

            GameObject prefab = objData.GetPrefab(gridPosition);
            if (prefab == null) continue;
            GameObject objInstance = Instantiate(prefab, worldPosition, instance.rotation);
            Vector3 preScale = objInstance.transform.localScale;
            objInstance.transform.localScale = Vector3.zero;
            objInstance.transform.parent = objs.transform;

            spawnedObjects.Add(objInstance);
            instanceToGameObject[instance.instanceID] = objInstance;
            ApplyLoadedObjectChannel(instance, objInstance);

            float delay = UnityEngine.Random.Range(0f, 0.2f);
            StartCoroutine(PlayObjectAppearAnimation(objInstance, delay, preScale));

            if (objData.objType.HasFlag(ObjectType.Player))
            {
                Debug.Log($"[MapPlane] Found player object at {gridPosition}, instanceID {instance.instanceID}");

            }


        }

        InitializeLoadedMovableObjects(instances);

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
        if (isEditMode || planePreview == null) return;

        Vector3 gridOrigin = startPoint != null ? startPoint.position : transform.position;
        planePreview.transform.localScale = scale;
        if (startPoint != null) startPoint.position = gridOrigin;
    }

    public void RefreshMapBounds()
    {
        if (isEditMode || mapData == null || planePreview == null) return;
        SetPreViewScale(new Vector3((float)mapData.gridLength / 10 / mapData.cellSize, 1,
            (float)mapData.gridWidth / 10 / mapData.cellSize));
    }



    #region  tile
    private void LoadTiles()
    {
        for (int x = mapData.gridMin.x; x < mapData.gridMin.x + mapData.gridLength; x++)
        {
            for (int y = mapData.gridMin.y; y < mapData.gridMin.y + mapData.gridWidth; y++)
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
        float z = (gridPosition.y + 0.5f) * mapData.cellSize + startPoint.position.z;
        return new Vector3(x, startPoint.position.y, z);
    }

    public float GetGroundHeightAtWorldPosition(Vector3 worldPosition)
    {
        Vector2Int gridPosition = WorldToGridPosition(worldPosition);
        GameObject tile = GetTileAtGridPosition(gridPosition);
        if (tile != null)
        {
            Renderer[] renderers = tile.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                float surfaceHeight = float.NegativeInfinity;
                foreach (Renderer renderer in renderers)
                    surfaceHeight = Mathf.Max(surfaceHeight, renderer.bounds.max.y);
                return surfaceHeight;
            }
        }

        return startPoint != null ? startPoint.position.y : transform.position.y;
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

            foreach (WallDirection direction in System.Enum.GetValues(typeof(WallDirection)))
            {
                Vector2Int neighbor = gridPosition + (direction switch
                {
                    WallDirection.Up => Vector2Int.up,
                    WallDirection.Down => Vector2Int.down,
                    WallDirection.Left => Vector2Int.left,
                    WallDirection.Right => Vector2Int.right,
                    _ => Vector2Int.zero
                });
                if (!mapData.HasTileAt(gridPosition) && !mapData.HasTileAt(neighbor))
                    RemoveWall(gridPosition, direction);
            }
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
        for (int x = mapData.gridMin.x - 1; x <= mapData.gridMin.x + mapData.gridLength; x++)
        {
            for (int y = mapData.gridMin.y - 1; y <= mapData.gridMin.y + mapData.gridWidth; y++)
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
                        if (mapData.CanPlaceWall(gridPosition, direction))
                            PlaceWall(gridPosition, wallData, direction);
                        else
                            mapData.RemoveWall(gridPosition, direction);
                    }
                }
            }
        }
    }

    public GameObject PlaceWall(Vector2Int gridPosition, WallData wallData, WallDirection wallDirection)
    {
        if (!mapData.CanPlaceWall(gridPosition, wallDirection))
        {
            return null;
        }

        mapData.GetNormalizedWallPosition(gridPosition, wallDirection, out var normalizedPosition, out var normalizedDirection);
        var key = (normalizedPosition, normalizedDirection);
        if (wallObjects.TryGetValue(key, out var existingWall))
        {
            Destroy(existingWall);
            wallObjects.Remove(key);
        }
        mapData.SetWall(gridPosition, wallDirection, wallData.wallID);

        Vector3 basePosition = GridToWorldPosition(gridPosition);
        Quaternion rotation = GetRotationFromDirection(wallDirection);
        Vector3 offset = GetOffsetFromDirection(wallDirection, wallData.startOffset);
        GameObject wallPrefab = wallData.GetPrefab(gridPosition, wallDirection);
        if (wallPrefab == null)
        {
            Debug.LogError($"[MapPlane] No prefab configured for wall ID {wallData.wallID}");
            return null;
        }

        GameObject wallInstance = Instantiate(wallPrefab, basePosition + offset, rotation);
        wallInstance.transform.parent = this.transform;
        wallObjects[key] = wallInstance;

        return wallInstance;
    }

    public void RemoveWall(Vector2Int gridPosition, WallDirection wallDirection)
    {
        mapData.GetNormalizedWallPosition(gridPosition, wallDirection, out var normalizedPosition, out var normalizedDirection);
        var key = (normalizedPosition, normalizedDirection);

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
        ObjDataInstance playerInstance = mapData.GetInstanceAtGridWithType(grid3D, ObjectType.Player);
        if (playerInstance != null)
            return GetObjectByInstanceID(playerInstance.instanceID);
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

    public GameObject PlaceObject(Vector2Int gridPosition, ObjData objData, Quaternion quaternion, int height = 0, Vector3 scale = default, int channelID = 0, Vector3? customWorldPosition = null)
    {
        bool pureDecoration = objData != null && objData.objType.HasFlag(ObjectType.PureDecoration);
        if (!pureDecoration && !mapData.IsWithinGrid(gridPosition))
        {
            return null;
        }

        if (!mapData.CanPlaceObj(gridPosition, objData.objID, height, quaternion, channelID))
        {
            return null;
        }

        bool channelUnique = channelID > 0 &&
            (objData.objType.HasFlag(ObjectType.Ghost) || objData.objType.HasFlag(ObjectType.Goal));
        if (!pureDecoration && (objData.maxCount == 1 || objData.objType.HasFlag(ObjectType.Player) || channelUnique))
        {
            GameObject existingUniqueObj = null;
            string existingInstanceID = null;
            MapPlane sourceMap = null;

            if (levelData != null)
            {
                var map = levelData.mapData;
                if (map != null)
                {
                    ObjDataInstance linkedInstance = channelUnique
                        ? map.GetLinkedInstance(objData.objType, channelID)
                        : null;
                    bool hasObjectCountUniqueness = objData.maxCount == 1 || objData.objType.HasFlag(ObjectType.Player);
                    IEnumerable<ObjDataInstance> candidates = linkedInstance != null
                        ? new[] { linkedInstance }
                        : map.GetAllObjectInstances();
                    foreach (var instance in candidates)
                    {
                        if ((channelUnique && instance == linkedInstance) ||
                            ((!channelUnique || hasObjectCountUniqueness) && instance.objID == objData.objID))
                        {
                            existingInstanceID = instance.instanceID;
                            sourceMap = FindMapPlaneByMapData(map);
                            if (sourceMap != null)
                            {
                                existingUniqueObj = sourceMap.GetObjectByInstanceID(instance.instanceID);
                            }
                            break;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(existingInstanceID) && existingUniqueObj == null)
            {
                MapData sourceData = sourceMap != null ? sourceMap.mapData : levelData.mapData;
                sourceData.RemoveObject(existingInstanceID);
                if (sourceMap != null) sourceMap.instanceToGameObject.Remove(existingInstanceID);
            }

            if (existingUniqueObj != null)
            {
                Vector3 worldPosition = GridToWorldPosition(gridPosition);
                worldPosition.y = startPoint.position.y + height * mapData.cellSize;

                MapData sourceData = sourceMap != null ? sourceMap.mapData : mapData;
                sourceData.RemoveObject(existingInstanceID);
                if (sourceMap != null)
                {
                    sourceMap.instanceToGameObject.Remove(existingInstanceID);
                    if (sourceMap != this) sourceMap.spawnedObjects.Remove(existingUniqueObj);
                }
                if (sourceMap != this)
                {
                    existingUniqueObj.transform.parent = objs.transform;
                    spawnedObjects.Add(existingUniqueObj);
                }

                existingUniqueObj.transform.position = worldPosition;
                existingUniqueObj.transform.rotation = quaternion;
                BarrelMove existingBarrel = existingUniqueObj.GetComponent<BarrelMove>();
                if (existingBarrel != null) existingBarrel.InitializeOrientation(quaternion);
                ApplyObjectChannel(objData, existingUniqueObj, channelID);
                if (scale != default)
                {
                    existingUniqueObj.transform.localScale = scale;
                }

                string newInstanceID = mapData.PlaceObject(gridPosition, objData, height, quaternion, channelID);
                instanceToGameObject[newInstanceID] = existingUniqueObj;

                if (isEditMode && levelData != null && objData.objType.HasFlag(ObjectType.Player))
                {
                    levelData.playerStart.startGridPos = gridPosition;
                }

                return existingUniqueObj;
            }
        }

        GameObject existingObject = objData.objType.HasFlag(ObjectType.PureDecoration) ||
                                    objData.objType.HasFlag(ObjectType.Goal) ||
                                    objData.objType.HasFlag(ObjectType.PressurePlate)
            ? null
            : GetObjectAtGridPosition(gridPosition, height);
        if (existingObject != null)
        {
            RemoveObject(gridPosition, height);
        }

        Vector3 newWorldPosition = GridToWorldPosition(gridPosition);
        newWorldPosition.y = startPoint.position.y + height * mapData.cellSize;
        if (pureDecoration && customWorldPosition.HasValue)
            newWorldPosition = customWorldPosition.Value;

        string instanceID = mapData.PlaceObject(gridPosition, objData, height, quaternion, channelID,
            pureDecoration ? customWorldPosition : null);
        GameObject prefab = objData.GetPrefab(gridPosition);
        if (prefab == null)
        {
            Debug.LogError($"[MapPlane] No prefab configured for object ID {objData.objID}");
            mapData.RemoveObject(instanceID);
            return null;
        }
        GameObject objInstance = Instantiate(prefab, newWorldPosition, quaternion);
        BarrelMove placedBarrel = objInstance.GetComponent<BarrelMove>();
        if (placedBarrel != null) placedBarrel.InitializeOrientation(quaternion);
        ApplyObjectChannel(objData, objInstance, channelID);
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

    public GameObject GetPureDecorationAtGridPosition(Vector2Int gridPosition, int height = 0)
    {
        Vector3Int grid = new Vector3Int(gridPosition.x, gridPosition.y, height);
        foreach (ObjDataInstance instance in mapData.GetAllObjectInstances())
        {
            if (instance == null || instance.currentOccupied == null || !instance.currentOccupied.Contains(grid)) continue;
            ObjData data = mapData.GetObjectData(instance.objID);
            if (data == null || !data.objType.HasFlag(ObjectType.PureDecoration)) continue;
            if (instanceToGameObject.TryGetValue(instance.instanceID, out GameObject decoration)) return decoration;
        }
        return null;
    }

    public ObjDataInstance GetPureDecorationInstanceAtGridPosition(Vector2Int gridPosition, int height = 0)
    {
        Vector3Int grid = new Vector3Int(gridPosition.x, gridPosition.y, height);
        foreach (ObjDataInstance instance in mapData.GetAllObjectInstances())
        {
            if (instance == null || instance.currentOccupied == null || !instance.currentOccupied.Contains(grid)) continue;
            ObjData data = mapData.GetObjectData(instance.objID);
            if (data != null && data.objType.HasFlag(ObjectType.PureDecoration)) return instance;
        }
        return null;
    }

    public ObjDataInstance GetNearestPureDecorationInstance(Vector3 worldPosition)
    {
        ObjDataInstance nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (ObjDataInstance instance in mapData.GetAllObjectInstances())
        {
            if (instance == null) continue;
            ObjData data = mapData.GetObjectData(instance.objID);
            if (data == null || !data.objType.HasFlag(ObjectType.PureDecoration)) continue;
            Vector3 objectPosition = instanceToGameObject.TryGetValue(instance.instanceID, out GameObject obj) && obj != null
                ? obj.transform.position
                : (instance.hasCustomWorldPosition ? instance.customWorldPosition : GridToWorldPosition(instance.mainGrid));
            float distance = Vector2.Distance(new Vector2(objectPosition.x, objectPosition.z),
                new Vector2(worldPosition.x, worldPosition.z));
            float pickRadius = Mathf.Max(mapData.cellSize * 0.5f, data.prefab != null ? data.prefab.transform.localScale.magnitude * 0.35f : 0f);
            if (distance <= pickRadius && distance < nearestDistance)
            {
                nearest = instance;
                nearestDistance = distance;
            }
        }
        return nearest;
    }

    public void RemoveObjectByInstanceID(string instanceID)
    {
        if (string.IsNullOrEmpty(instanceID)) return;
        ObjDataInstance instance = mapData.GetInstance(instanceID);
        if (instance == null) return;
        ObjData data = mapData.GetObjectData(instance.objID);
        if (isEditMode && levelData != null && data != null) levelData.UpdateObjectCount(data, false);
        mapData.RemoveObject(instanceID);
        if (instanceToGameObject.TryGetValue(instanceID, out GameObject obj))
        {
            instanceToGameObject.Remove(instanceID);
            spawnedObjects.Remove(obj);
            if (obj != null) Destroy(obj);
        }
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

                GameObject prefab = objData.GetPrefab(gridPosition);
                if (prefab == null)
                {
                    Debug.LogError($"[MapPlane] Prefab is null for object ID {objData.objID}");
                    continue;
                }

                Vector3 worldPosition = GridToWorldPosition(gridPosition);
                worldPosition.y = startPoint.position.y + instance.height * mapData.cellSize;
                if (instance.hasCustomWorldPosition) worldPosition = instance.customWorldPosition;

                GameObject objInstance = Instantiate(prefab, worldPosition, instance.rotation);


                if (objInstance == null)
                {
                    Debug.LogError($"[MapPlane] Failed to instantiate object ID {objData.objID}");
                    continue;
                }

                spawnedObjects.Add(objInstance);
                instanceToGameObject[instance.instanceID] = objInstance;
                objInstance.transform.parent = objs.transform;

                var loadedObj = GetObjectByInstanceID(instance.instanceID);
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

        // Initialize movable actors from the authoritative level instances.
        // This also evaluates ghosts already standing on their paired grave.
        foreach (var instance in instances)
        {
            GameObject loadedObject = GetObjectByInstanceID(instance.instanceID);
            if (loadedObject == null) continue;

            ApplyLoadedObjectChannel(instance, loadedObject);
        }

        InitializeLoadedMovableObjects(instances);
        if (!isEditMode) RefreshPressureMechanisms(false);

        foreach (var kvp in instanceToGameObject)
        {
            if (kvp.Value == null)
            {
                Debug.LogError($"[MapPlane] Found null GameObject for instance ID {kvp.Key}");
            }
        }

        Debug.Log($"[MapPlane] Finished loading objects. Loaded {spawnedObjects.Count} objects, Dictionary size: {instanceToGameObject.Count}");
    }

    private void ApplyLoadedObjectChannel(ObjDataInstance instance, GameObject loadedObject)
    {
        if (instance == null || loadedObject == null) return;
        ObjData data = mapData.GetObjectData(instance.objID);
        ApplyObjectChannel(data, loadedObject, instance.channelID);
    }

    private void ApplyObjectChannel(ObjData data, GameObject target, int channelID)
    {
        if (data == null || target == null || channelID <= 0) return;
        bool usesChannel = data.objType.HasFlag(ObjectType.Ghost) || data.objType.HasFlag(ObjectType.Goal) ||
                           data.objType.HasFlag(ObjectType.PressurePlate) ||
                           data.objType.HasFlag(ObjectType.PressureControlled);
        if (!usesChannel) return;
        ChannelAppearance appearance = target.GetComponent<ChannelAppearance>();
        if (appearance == null) appearance = target.AddComponent<ChannelAppearance>();
        appearance.ApplyChannel(channelID);
    }

    /// <summary>Refreshes pressure plates after a complete turn, not during a roll animation.</summary>
    public void RefreshPressureMechanisms(bool animate = true)
    {
        if (mapData == null || isEditMode) return;
        HashSet<int> activeChannels = new HashSet<int>();
        List<ObjDataInstance> instances = mapData.GetAllObjectInstances().ToList();

        foreach (ObjDataInstance plate in instances)
        {
            ObjData plateData = mapData.GetObjectData(plate.objID);
            if (plateData == null || !plateData.objType.HasFlag(ObjectType.PressurePlate) || plate.channelID <= 0 ||
                plate.currentOccupied == null || plate.currentOccupied.Count == 0)
                continue;

            foreach (ObjDataInstance occupant in instances)
            {
                if (occupant.instanceID == plate.instanceID || occupant.height != plate.height ||
                    occupant.currentOccupied == null || !occupant.currentOccupied.Contains(plate.currentOccupied[0]))
                    continue;
                ObjData occupantData = mapData.GetObjectData(occupant.objID);
                if (occupantData != null && occupantData.objType.HasFlag(ObjectType.CanActivatePressurePlate))
                {
                    activeChannels.Add(plate.channelID);
                    break;
                }
            }
        }

        foreach (ObjDataInstance instance in instances)
        {
            ObjData data = mapData.GetObjectData(instance.objID);
            if (data == null || !data.objType.HasFlag(ObjectType.PressureControlled)) continue;
            bool hidden = instance.channelID > 0 && activeChannels.Contains(instance.channelID);
            mapData.SetPressureObjectHidden(instance, hidden);

            GameObject instanceObject = GetObjectByInstanceID(instance.instanceID);
            if (instanceObject == null) continue;
            PressureDissolveEffect dissolve = instanceObject.GetComponent<PressureDissolveEffect>();
            if (dissolve == null) dissolve = instanceObject.AddComponent<PressureDissolveEffect>();
            dissolve.SetVisible(!hidden, animate);
        }
    }

    private void InitializeLoadedMovableObjects(List<ObjDataInstance> instances)
    {
        foreach (ObjDataInstance instance in instances)
        {
            GameObject loadedObject = GetObjectByInstanceID(instance.instanceID);
            MovableObj movable = loadedObject != null ? loadedObject.GetComponent<MovableObj>() : null;
            if (movable == null) continue;

            movable.SetMap(this);
            movable.pos = instance.mainGrid;
            BarrelMove barrel = movable as BarrelMove;
            if (barrel != null) barrel.InitializeOrientation(instance.rotation);
        }

        // Pair checks need every channel object to have been instantiated first.
        foreach (ObjDataInstance instance in instances)
        {
            GameObject loadedObject = GetObjectByInstanceID(instance.instanceID);
            NPCMove ghost = loadedObject != null ? loadedObject.GetComponent<NPCMove>() : null;
            if (ghost != null) ghost.RefreshPairState();
        }
    }

    public GameObject GetObjectByInstanceID(string instanceID)
    {
        if (instanceToGameObject.TryGetValue(instanceID, out GameObject obj))
        {
            return obj;
        }
        return null;
    }

    public string GetInstanceIDForObject(GameObject target)
    {
        if (target == null) return null;
        foreach (var entry in instanceToGameObject)
            if (entry.Value == target) return entry.Key;
        return null;
    }

    #endregion
}
