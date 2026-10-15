using UnityEngine;
using Nova;
using MoreMountains.Feedbacks;
using Commands;

public interface IPlacementState
{
    void EnterState(PlaceMgr placeMgr);
    void UpdateState();
    void ExitState();
}
public class StopPlacementState : IPlacementState
{
    public StopPlacementState()
    {
    }
    public void EnterState(PlaceMgr placeMgr)
    {
        placeMgr.isPlacing = false;
    }
    public void UpdateState()
    {
    }
    public void ExitState()
    {
    }
}
public class ObjectPlacementState : IPlacementState
{
    private PlaceMgr placeMgr;
    private ObjData currentObjConfig;
    private GameObject previewItem;
    private GameObject previewPrefab;
    private Quaternion rotation = Quaternion.identity;

    private Vector2Int lastPlacedGridPos = new Vector2Int(-1, -1);

    public ObjectPlacementState(ObjData objData)
    {
        currentObjConfig = objData;
    }

    public void EnterState(PlaceMgr placeMgr)
    {
        this.placeMgr = placeMgr;
        placeMgr.isPlacing = true;

        if (previewItem != null)
        {
            GameObject.Destroy(previewItem);
        }

        GameObject prefab = currentObjConfig.GetPrefab(Vector2Int.zero);
        if (prefab != null)
        {
            previewPrefab = prefab;
            previewItem = GameObject.Instantiate(prefab);
            previewItem.layer = 1;
            previewItem.transform.localScale = prefab.transform.localScale;
        }
        else
        {
            Debug.LogError("No prefab available for object placement.");
        }
    }

    public void UpdateState()
    {
        if (!placeMgr.RaycastOnMap(out Vector3 targetPos, out MapPlane map, out Vector2Int gridPos, out Vector3 mousepos))
            return;

        const int placementHeight = 0;
        targetPos.y = 0f;
        GameObject targetPrefab = currentObjConfig.GetPrefab(gridPos);
        UpdatePreviewPrefab(targetPrefab);
        if (previewItem == null) return;
        previewItem.transform.position = targetPos;
        rotation = placeMgr.HandleRotationInput(previewItem.transform.rotation);
        previewItem.transform.rotation = rotation;

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
        {
            var objectInstance = new ObjDataInstance(currentObjConfig.objID, gridPos, placementHeight, rotation, currentObjConfig);
            bool expanded = false;
            foreach (var occupiedGrid in objectInstance.currentOccupied)
                expanded |= map.mapData.EnsureGridContains(new Vector2Int(occupiedGrid.x, occupiedGrid.y));
            if (expanded) map.RefreshMapBounds();
        }

        bool canPlace = map.mapData.CanPlaceObj(gridPos, currentObjConfig.objID, placementHeight, rotation) &&
                        (map.levelData == null || map.levelData.CanPlaceMoreObjects(currentObjConfig));
        placeMgr.UpdatePreviewColor(canPlace, previewItem);

        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            PlaceObjectConditionally(targetPos, gridPos, map);
        }

        if (Input.GetMouseButtonDown(1))
        {
            placeMgr.RemoveObjectAt(targetPos, gridPos, map);
        }
    }

    private void UpdatePreviewPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            if (previewItem != null) GameObject.Destroy(previewItem);
            previewItem = null;
            return;
        }

        if (previewPrefab == prefab && previewItem != null) return;
        if (previewItem != null) GameObject.Destroy(previewItem);
        previewItem = GameObject.Instantiate(prefab);
        previewPrefab = prefab;
        previewItem.name = prefab.name + " (Preview)";
        previewItem.layer = 1;
        previewItem.transform.localScale = prefab.transform.localScale;
    }



    private void PlaceObjectConditionally(Vector3 targetPos, Vector2Int gridPos, MapPlane map)
    {
        if (gridPos != lastPlacedGridPos)
        {
            placeMgr.PlaceObjectAt(targetPos, gridPos, currentObjConfig, map,
                previewItem.transform.rotation, previewItem.transform.localScale);
            lastPlacedGridPos = gridPos;
        }
    }

    public void ExitState()
    {
        if (previewItem != null)
        {
            GameObject.Destroy(previewItem);
        }
    }
}
public class TilePlacementState : IPlacementState
{
    private PlaceMgr placeMgr;
    private TileData currentItemConfig;
    private GameObject previewItem;
    private Quaternion rotation = Quaternion.identity;

    private Vector2Int lastPlacedGridPos = new Vector2Int(-1, -1);
    private bool isMouseHolding = false;

    public TilePlacementState(TileData tileData)
    {
        currentItemConfig = tileData;
    }

    public void EnterState(PlaceMgr placeMgr)
    {
        this.placeMgr = placeMgr;
        placeMgr.isPlacing = true;

        if (previewItem != null)
        {
            GameObject.Destroy(previewItem);
        }

        GameObject prefab = currentItemConfig.prefab;
        if (prefab != null)
        {
            previewItem = GameObject.Instantiate(prefab);
            previewItem.layer = 1;
            previewItem.transform.localScale = prefab.transform.localScale;
        }
        else
        {
            Debug.LogError("No prefab available for tile placement.");
        }
    }

    public void UpdateState()
    {
        if (!placeMgr.RaycastOnMap(out Vector3 targetPos, out MapPlane map, out Vector2Int gridPos, out Vector3 mousepos))
            return;

        previewItem.transform.position = targetPos + currentItemConfig.startOffset;
        rotation = placeMgr.HandleRotationInput(previewItem.transform.rotation);
        previewItem.transform.rotation = rotation;

        bool canPlace = map.mapData.CanPlaceTile(gridPos, currentItemConfig);
        placeMgr.UpdatePreviewColor(canPlace, previewItem);

        if (Input.GetMouseButtonDown(0))
        {
            isMouseHolding = true;
            PlaceTileConditionally(targetPos, gridPos, map, canPlace);
        }
        else if (Input.GetMouseButton(0) && isMouseHolding)
        {
            PlaceTileConditionally(targetPos, gridPos, map, canPlace);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMouseHolding = false;
        }

        if (Input.GetMouseButton(1))
        {
            placeMgr.RemoveTileAt(targetPos, gridPos, map);
        }
    }


    private void PlaceTileConditionally(Vector3 targetPos, Vector2Int gridPos, MapPlane map, bool canPlace)
    {
        if (gridPos != lastPlacedGridPos && canPlace)
        {
            placeMgr.PlaceTileAt(targetPos, gridPos, currentItemConfig, map,
                rotation, previewItem.transform.localScale);
            lastPlacedGridPos = gridPos;
        }
    }

    public void ExitState()
    {
        if (previewItem != null)
        {
            GameObject.Destroy(previewItem);
        }
    }
}


public class WallPlacementState : IPlacementState
{
    private PlaceMgr placeMgr;
    private WallData currentItemConfig;
    private GameObject previewItem;
    private GameObject previewPrefab;
    private Quaternion rotation = Quaternion.identity;

    private Vector2Int lastPlacedGridPos = new Vector2Int(-1, -1);
    private bool isMouseHolding = false;
    float cellSize = 1;

    public WallPlacementState(WallData wallData)
    {
        currentItemConfig = wallData;
    }

    public void EnterState(PlaceMgr placeMgr)
    {
        this.placeMgr = placeMgr;
        placeMgr.isPlacing = true;

        if (previewItem != null)
        {
            GameObject.Destroy(previewItem);
        }

        GameObject prefab = currentItemConfig.GetPrefab(Vector2Int.zero, WallDirection.Up);
        if (prefab != null)
        {
            previewPrefab = prefab;
            previewItem = GameObject.Instantiate(prefab);
            previewItem.layer = 1;
            previewItem.transform.localScale = prefab.transform.localScale;
        }
        else
        {
            Debug.LogError("No prefab available for wall placement.");
        }
    }

    public void UpdateState()
    {
        if (!placeMgr.RaycastOnMap(out Vector3 targetPos, out MapPlane map, out Vector2Int gridPos, out Vector3 mousepos))
            return;

        mousepos.y = 0;
        targetPos.y = 0;
        WallDirection wallDirection = GetWallDirection(targetPos, mousepos);
        UpdatePreviewPrefab(currentItemConfig.GetPrefab(gridPos, wallDirection));
        if (previewItem == null) return;
        RotatePreviewItem(wallDirection);


        Vector3 offset = Vector3.zero;
        switch (wallDirection)
        {
            case WallDirection.Up:
                offset = new Vector3(0, 0, 0.5f * cellSize);
                break;
            case WallDirection.Down:
                offset = new Vector3(0, 0, -0.5f * cellSize);
                break;
            case WallDirection.Left:
                offset = new Vector3(-0.5f * cellSize, 0, 0);
                break;
            case WallDirection.Right:
                offset = new Vector3(0.5f * cellSize, 0, 0);
                break;
        }

        previewItem.transform.position = targetPos + offset + currentItemConfig.startOffset;

        bool canPlace = map.mapData.CanPlaceWall(gridPos,wallDirection);
        placeMgr.UpdatePreviewColor(canPlace, previewItem);

        if (Input.GetMouseButtonDown(0))
        {
            isMouseHolding = true;
            PlaceWallConditionally(gridPos,map,wallDirection,canPlace);

        }
        else if (Input.GetMouseButton(0) && isMouseHolding)
        {
            PlaceWallConditionally(gridPos,map,wallDirection,canPlace);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMouseHolding = false;
        }

        if (Input.GetMouseButton(1))
        {
            placeMgr.RemoveWallAt(gridPos, wallDirection, map);
        }
    }

    private void PlaceWallConditionally(Vector2Int gridPos, MapPlane map, WallDirection wallDirection, bool canPlace)
    {
        if (gridPos != lastPlacedGridPos && canPlace)
        {
            placeMgr.PlaceWallAt(gridPos, wallDirection, currentItemConfig, map);
            lastPlacedGridPos = gridPos;
        }
    }


    private WallDirection GetWallDirection(Vector3 gridCenter, Vector3 mousePos)
    {
        Vector3 direction = (mousePos - gridCenter).normalized;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z)) // Horizontal
        {
            return direction.x > 0 ? WallDirection.Right : WallDirection.Left;
        }
        else
        {
            return direction.z > 0 ? WallDirection.Up : WallDirection.Down;
        }
        return WallDirection.Up;
    }

    private void RotatePreviewItem(WallDirection wallDirection)
    {
        switch (wallDirection)
        {
            case WallDirection.Up:
                previewItem.transform.rotation = Quaternion.Euler(0, 0, 0);
                break;
            case WallDirection.Down:
                previewItem.transform.rotation = Quaternion.Euler(0, 180, 0);
                break;
            case WallDirection.Left:
                previewItem.transform.rotation = Quaternion.Euler(0, 90, 0);
                break;
            case WallDirection.Right:
                previewItem.transform.rotation = Quaternion.Euler(0, -90, 0);
                break;
        }
    }

    private void UpdatePreviewPrefab(GameObject prefab)
    {
        if (prefab == previewPrefab) return;
        if (previewItem != null) GameObject.Destroy(previewItem);
        previewItem = null;
        previewPrefab = prefab;
        if (prefab == null) return;

        previewItem = GameObject.Instantiate(prefab);
        previewItem.layer = 1;
        previewItem.transform.localScale = prefab.transform.localScale;
    }





    public void ExitState()
    {
        if (previewItem != null)
        {
            GameObject.Destroy(previewItem);
        }
    }
}

public class PlaceMgr : Singleton<PlaceMgr>
{
    public Camera uiCamera;
    UIBlockHit testUIBlockHit;



    public bool isPlacing = false;

    public LayerMask detectionLayers;

    private IPlacementState currentState;

    private float rightClickTime = 0f;
    private float doubleClickThreshold = 0.3f;


    private Color validPlacementColor = new Color(0, 1, 0, 0.5f);
    private Color invalidPlacementColor = new Color(1, 0, 0, 0.5f);


    private Command commandMgr= new Command();

    public void ClearAllCommands()
    {
        commandMgr.ClearAllCommands();
    }
    public void Redo()
    {
        commandMgr.Redo();
    }

    public void Undo()
    {
        commandMgr.Undo();
    }



    void Start()
    {
        currentState = new StopPlacementState();
    }

    public void SetPlacementState(IPlacementState newState)
    {
        currentState?.ExitState();
        currentState = newState;
        currentState.EnterState(this);
    }

    private void Update()
    {
        if (Interaction.Raycast(uiCamera.ScreenPointToRay(Input.mousePosition), out testUIBlockHit))
        {
            if (testUIBlockHit.UIBlock.name != "UIRoot")
            {
                return;
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (Time.time - rightClickTime < doubleClickThreshold)
            {
                SetPlacementState(new StopPlacementState());
            }
            rightClickTime = Time.time;
        }

        currentState?.UpdateState();
    }

    public void EditTileMode(TileData tileData)
    {
        SetPlacementState(new TilePlacementState(tileData));
    }
    public void EditObjectMode(ObjData objData)
    {
        SetPlacementState(new ObjectPlacementState(objData));
    }

    public void EditWallMode(WallData wallData)
    {
        SetPlacementState(new WallPlacementState(wallData));
    }


    public void ForceStopPlace()
    {
        SetPlacementState(new StopPlacementState());
    }

    public void UpdatePreviewColor(bool canPlace, GameObject previewItem)
    {
        if (previewItem == null) return;

        Color targetColor = canPlace ? validPlacementColor : invalidPlacementColor;
        Renderer[] renderers = previewItem.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
            {
                material.color = targetColor;
            }
        }
    }


    public bool RaycastOnMap(out Vector3 targetPos, out MapPlane map, out Vector2Int gridPos, out Vector3 mousepos)
    {
        targetPos = Vector3.zero;
        map = null;
        gridPos = Vector2Int.zero;
        mousepos = Input.mousePosition;

        var maps = MapMgr.Instance != null ? MapMgr.Instance.GetMapPlanes() : null;
        if (maps == null || maps.Count == 0 || maps[0] == null) return false;

        map = maps[0];
        Ray ray = Camera.main.ScreenPointToRay(mousepos);
        Plane mapPlane = new Plane(Vector3.up, map.startPoint.position);
        if (!mapPlane.Raycast(ray, out float distance)) return false;

        mousepos = ray.GetPoint(distance);
        gridPos = map.WorldToGridPosition(mousepos);
        targetPos = map.GridToWorldPosition(gridPos);

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            if (map.mapData.EnsureGridContains(gridPos)) map.RefreshMapBounds();
        }
        return true;
    }

    public Quaternion HandleRotationInput(Quaternion rotation)
    {
        float yaw = rotation.eulerAngles.y;
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            yaw = (yaw + 90) % 360;
        }

        // Objects stay on the board plane: only rotate around the vertical axis.
        return Quaternion.Euler(0, yaw, 0);
    }

    public void PlaceTileAt(Vector3 position, Vector2Int gridPos, TileData tileData, MapPlane map, Quaternion rotation, Vector3 scale)
    {
        var placeCommand = new Commands.PlaceTileCommand(position, rotation, scale, tileData, map);
        commandMgr.ExecuteCommand(placeCommand);
    }

    public void RemoveTileAt(Vector3 position, Vector2Int gridPos, MapPlane map)
    {
        if (map.GetTileID(gridPos) > 0)
        {
            var removeCommand = new Commands.RemoveTileCommand(position, map);
            commandMgr.ExecuteCommand(removeCommand);
        }
    }

    public void PlaceWallAt(Vector2Int gridPos, WallDirection direction, WallData wallData, MapPlane map)
    {
        var placeCommand = new Commands.PlaceWallCommand(gridPos, direction, wallData, map);
        commandMgr.ExecuteCommand(placeCommand);
    }

    public void RemoveWallAt(Vector2Int gridPos, WallDirection direction, MapPlane map)
    {
        int existingWallID = map.mapData.GetWall(gridPos, direction);
        if (existingWallID != -1)
        {
            var removeCommand = new Commands.RemoveWallCommand(gridPos, direction, map);
            commandMgr.ExecuteCommand(removeCommand);
        }
    }


    public void PlaceObjectAt(Vector3 position, Vector2Int gridPos, ObjData objData, MapPlane map, Quaternion rotation, Vector3 scale)
    {
        const int editorHeight = 0;
        if (map.mapData.CanPlaceObj(gridPos, objData.objID, editorHeight, rotation) &&
            (map.levelData == null || map.levelData.CanPlaceMoreObjects(objData)))
        {
            if (map.levelData == null)
            {
                Debug.LogError($"[PlaceMgr] MapPlane {map.gameObject.name} is missing levelData reference!");
                return;
            }
            var placeCommand = new Commands.PlaceObjectCommand(position, gridPos, editorHeight, rotation, objData, map, scale);
            commandMgr.ExecuteCommand(placeCommand);
        }
    }

    public void RemoveObjectAt(Vector3 position, Vector2Int gridPos, MapPlane map)
    {
        const int editorHeight = 0;
        ObjData existingObj = map.GetObjectDataAtGridPosition(gridPos, editorHeight);
        if (existingObj != null)
        {
            var removeCommand = new Commands.RemoveObjectCommand(position, gridPos, editorHeight, map);
            commandMgr.ExecuteCommand(removeCommand);
        }
    }
}


