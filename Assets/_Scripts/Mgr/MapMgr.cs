using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Sirenix.Utilities;
using Sirenix.Serialization;

public class MapMgr : Singleton<MapMgr>
{
    public TileDataList_SO tileDataList;
    public ObjDataList_SO objDataList;
    public WallDataList_SO wallDataList;
    public MapPlane mapPlanePrefab;
    public Transform startPoint;

    private Vector3 originalStartPoint;
    private bool isEditMode;
    private LevelData currentLevelData;
    private List<MapPlane> mapPlanes = new List<MapPlane>();
    private Dictionary<string, MapData> runtimeMapData = new Dictionary<string, MapData>();

    public List<MapPlane> insideWorld = new List<MapPlane>();
    public List<MapPlane> outsideWorld = new List<MapPlane>();
    public int planeDistance=3;

    public bool IsAllMapLoaded;
    int mapplaneCount=0;
    int loadMap=0;
    void Awake()
    {
        base.Awake();
        originalStartPoint = startPoint.position;
        mapplaneCount=0;
        IsAllMapLoaded=false;
        loadMap=0;
    }

    public void Initialize(LevelData levelData, bool editMode)
    {
        if (!ValidateReferences(levelData)) return;

        isEditMode = editMode;
        currentLevelData = levelData;

        ClearCurrentState();
        PrepareMapData();
        LoadMaps();
    }

    public void FinishLoadOneMapPlane()
    {
        loadMap++;
        if(loadMap>=mapplaneCount)
        {
            IsAllMapLoaded=true;
        }
    }

    private bool ValidateReferences(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("[MapMgr] LevelData is null!");
            return false;
        }

        if (levelData.insideWorld == null || levelData.outsideWorld == null)
        {
            Debug.LogError("[MapMgr] LevelData world references are null!");
            return false;
        }

        if (levelData.insideWorld.Count == 0 && levelData.outsideWorld.Count == 0)
        {
            Debug.LogError("[MapMgr] LevelData contains no maps in either world list!");
            return false;
        }

        if (tileDataList == null || objDataList == null || wallDataList == null)
        {
            Debug.LogError("[MapMgr] Required data lists are not assigned!");
            return false;
        }

        return true;
    }

    private void ClearCurrentState()
    {
        foreach (var plane in mapPlanes)
        {
            if (plane != null) Destroy(plane.gameObject);
        }
        mapPlanes.Clear();
        runtimeMapData.Clear();
        insideWorld.Clear();
        outsideWorld.Clear();
        mapplaneCount = 0;
        loadMap = 0;
        IsAllMapLoaded = false;
        startPoint.position = originalStartPoint;
    }

    private void PrepareMapData()
    {
        if (!isEditMode)
        {
            runtimeMapData.Clear();

            void ProcessMapList(List<MapData> mapList)
            {
                if (mapList == null) return;

                foreach (var map in mapList)
                {
                    if (map == null)
                    {
                        Debug.LogError("[MapMgr] Found null map!");
                        continue;
                    }

                    if (string.IsNullOrEmpty(map.mapID))
                    {
                        Debug.LogWarning("[MapMgr] Original map has no ID, initializing...");
                        map.Initialize();
                    }

                    var runtimeMap = map.DeepCopy();

                    if (string.IsNullOrEmpty(runtimeMap.mapID))
                    {
                        Debug.LogError($"[MapMgr] Runtime map copy has no ID for original map ID: {map.mapID}");
                        continue;
                    }

                    runtimeMap.tileDataList = tileDataList;
                    runtimeMap.objDataList = objDataList;
                    runtimeMap.wallDataList = wallDataList;
                    runtimeMapData[runtimeMap.mapID] = runtimeMap;

                    Debug.Log($"[MapMgr] Successfully created runtime data for map ID: {runtimeMap.mapID}");
                }
            }

            ProcessMapList(currentLevelData.insideWorld);
            ProcessMapList(currentLevelData.outsideWorld);
        }
    }

    private void LoadMaps()
    {
        foreach (var map in currentLevelData.insideWorld)
        {
            if (map != null)
            {
                insideWorld.Add(CreateMapPlane(map));
            }
        }

        foreach (var map in currentLevelData.outsideWorld)
        {
            if (map != null)
            {
                outsideWorld.Add(CreateMapPlane(map));
            }
        }
    }

    private MapPlane CreateMapPlane(MapData mapData)
    {
        if (!ValidateMapData(mapData)) return null;


        if (!mapData.IsInitialized())
        {
            Debug.LogError($"[MapMgr] Map data not initialized for map ID: {mapData.mapID}");
            mapData.Initialize();
        }

        MapPlane planeInstance = Instantiate(mapPlanePrefab, startPoint.position, Quaternion.identity);
        planeInstance.isEditMode = isEditMode;
        if (!isEditMode)
        {
            planeInstance.SetPreViewScale(CalculateMapScale(mapData));
        }

        if (!isEditMode)
        {
            // if (!runtimeMapData.ContainsKey(mapData.mapID))
            // {
            //     Debug.LogWarning($"[MapMgr] Runtime data not found for map ID: {mapData.mapID}, creating new copy");
            //     var runtimeMap = mapData.DeepCopy();
            //     runtimeMap.tileDataList = tileDataList;
            //     runtimeMap.objDataList = objDataList;
            //     runtimeMap.wallDataList = wallDataList;
            //     runtimeMapData[mapData.mapID] = runtimeMap;
            // }

            planeInstance.mapData = runtimeMapData[mapData.mapID];
        }
        else
        {
            planeInstance.mapData = mapData;
        }

        planeInstance.levelData = currentLevelData;
        planeInstance.tileDataList = tileDataList;
        planeInstance.objDataList = objDataList;
        planeInstance.wallDataList = wallDataList;

        mapPlanes.Add(planeInstance);
        UpdateStartPoint(mapData);
        ++mapplaneCount;

        planeInstance.LoadMap();
        return planeInstance;
    }

    private bool ValidateMapData(MapData mapData)
    {
        if (mapData == null)
        {
            Debug.LogError("[MapMgr] MapData is null!");
            return false;
        }

        if (string.IsNullOrEmpty(mapData.mapID))
        {
            Debug.LogWarning("[MapMgr] Map ID is null or empty, generating new ID");
            mapData.Initialize(); // This will generate a new ID if none exists
            if (string.IsNullOrEmpty(mapData.mapID))
            {
                Debug.LogError("[MapMgr] Failed to generate map ID!");
                return false;
            }
        }

        return true;
    }

    private Vector3 CalculateMapScale(MapData mapData)
    {
        return new Vector3(
            (float)mapData.gridLength / 10 / mapData.cellSize,
            1,
            (float)mapData.gridWidth / 10 / mapData.cellSize
        );
    }

    private void UpdateStartPoint(MapData mapData)
    {
        startPoint.position += new Vector3((mapData.gridLength + planeDistance) * mapData.cellSize, 0, 0);
    }

    public MapData GetRuntimeMapData(string mapID)
    {
        if (string.IsNullOrEmpty(mapID))
        {
            Debug.LogError("[MapMgr] Attempted to get runtime data with null or empty map ID!");
            return null;
        }

        if (!runtimeMapData.ContainsKey(mapID))
        {
            Debug.LogError($"[MapMgr] No runtime data found for map ID: {mapID}");
            return null;
        }

        return runtimeMapData[mapID];
    }


    public ObjDataInstance GetCorrespondingObjInstance(bool inside, Vector2Int gridPosition, out MapPlane correspondingMap, int height = 0)
    {
        Vector3Int grid3D = new Vector3Int(gridPosition.x, gridPosition.y, height);
        Debug.Log("finobjinetsnce");
        if (inside)
        {
            foreach (var mapplane in insideWorld)
            {
                ObjDataInstance objDataInstance = mapplane.mapData.GetObjDataInstanceAtGrid(grid3D);
                if (objDataInstance != null)
                {
                    Debug.Log("objDataInstance!=null");
                    correspondingMap = mapplane;
                    return objDataInstance;
                }
            }
        }
        else
        {
            foreach (var mapplane in outsideWorld)
            {
                ObjDataInstance objDataInstance = mapplane.mapData.GetObjDataInstanceAtGrid(grid3D);
                if (objDataInstance != null)
                {
                    correspondingMap = mapplane;
                    return objDataInstance;
                }
            }
        }
        correspondingMap = mapPlanes[0];
        return null;
    }



    public List<MapPlane> GetMapPlanes() => mapPlanes;
}
