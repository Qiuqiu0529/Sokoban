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
    private Quaternion rotation = Quaternion.identity;
    private int currentHeight = 0;

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

        GameObject prefab = currentObjConfig.prefab;
        if (prefab != null)
        {
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

        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKeyDown(KeyCode.E)) currentHeight++;
            if (Input.GetKeyDown(KeyCode.Q)) currentHeight = Mathf.Max(0, currentHeight - 1);
        }

        targetPos.y = currentHeight * map.mapData.cellSize;
        previewItem.transform.position = targetPos;
        rotation = placeMgr.HandleRotationInput(previewItem.transform.rotation);
        previewItem.transform.rotation = rotation;

        bool canPlace = map.mapData.CanPlaceObj(gridPos, currentObjConfig.objID, currentHeight, rotation);
        placeMgr.UpdatePreviewColor(canPlace, previewItem);

        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            PlaceObjectConditionally(targetPos, gridPos, map);
        }

        if (Input.GetMouseButtonDown(1))
        {
            placeMgr.RemoveObjectAt(targetPos, gridPos, currentHeight, map);
        }
    }



    private void PlaceObjectConditionally(Vector3 targetPos, Vector2Int gridPos, MapPlane map)
    {
        if (gridPos != lastPlacedGridPos)
        {
            placeMgr.PlaceObjectAt(targetPos, gridPos, currentObjConfig, map,
                previewItem.transform.rotation, currentHeight, previewItem.transform.localScale);
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

        GameObject prefab = currentItemConfig.prefab;
        if (prefab != null)
        {
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

        Ray ray = Camera.main.ScreenPointToRay(mousepos);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, detectionLayers))
        {
            map = hit.collider.GetComponent<MapPlane>();
            if (map == null) return false;
            mousepos = hit.point;
            gridPos = map.WorldToGridPosition(mousepos);
            targetPos = map.GridToWorldPosition(gridPos);
            return true;
        }
        return false;
    }

    public Quaternion HandleRotationInput(Quaternion rotation)
    {
        Vector3 eulerRotation = rotation.eulerAngles;

        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            eulerRotation.x = (eulerRotation.x + 90) % 360;
        }
        else if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            eulerRotation.y = (eulerRotation.y + 90) % 360;
        }
        else if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            eulerRotation.z = (eulerRotation.z + 90) % 360;
        }

        rotation = Quaternion.Euler(eulerRotation);
        return rotation;
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


    public void PlaceObjectAt(Vector3 position, Vector2Int gridPos, ObjData objData, MapPlane map, Quaternion rotation, int height, Vector3 scale)
    {
        if (map.mapData.CanPlaceObj(gridPos, objData.objID, height, rotation))
        {
            if (map.levelData == null)
            {
                Debug.LogError($"[PlaceMgr] MapPlane {map.gameObject.name} is missing levelData reference!");
                return;
            }
            var placeCommand = new Commands.PlaceObjectCommand(position, gridPos, height, rotation, objData, map, scale);
            commandMgr.ExecuteCommand(placeCommand);
        }
    }

    public void RemoveObjectAt(Vector3 position, Vector2Int gridPos, int height, MapPlane map)
    {
        ObjData existingObj = map.GetObjectDataAtGridPosition(gridPos, height);
        if (existingObj != null)
        {
            var removeCommand = new Commands.RemoveObjectCommand(position, gridPos, height, map);
            commandMgr.ExecuteCommand(removeCommand);
        }
    }
}


