using UnityEngine;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine.UIElements;
using System.Linq;
using Movement;

namespace Commands
{
    public interface ICommand
    {
        void Execute();
        void Undo();
        void ClearCommand();
    }

    public class CompositeCommand : ICommand
    {
        bool hasExcute;
        
        private List<ICommand> commands = new List<ICommand>();

        public int CommandSize()
        {
            return commands.Count;
        }

        public void AddCommand(ICommand command)
        {
            commands.Add(command);
        }
        public void SetHasExcute()
        {
            hasExcute = true;
        }

        public void Execute()
        {
            if (hasExcute)
            {
                return;
            }
            for (int i = 0; i < commands.Count; i++)
            {
                commands[i].Execute();
            }
        }

        public void Undo()
        {
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                commands[i].Undo();
            }
        }

        public void ClearCommand()
        {
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                commands[i].ClearCommand();
            }
        }
    }

    #region editormode
    public class PlaceTileCommand : ICommand
    {
        private GameObject placedTileInstance;
        private Vector3 placementPosition, placementScale;
        Vector2Int gridPosition;
        private Quaternion placementRotation;
        private TileData tileConfig;

        private GameObject previousTileInstance;
        private Quaternion previousRotation;
        private TileData previousTileConfig;
        private bool hadPreviousTile = false;

        private MapPlane mapPlane;

        public PlaceTileCommand(Vector3 position, Quaternion rotation, Vector3 scale, TileData config, MapPlane mapPlane)
        {
            this.placementPosition = position;
            this.placementRotation = rotation;
            this.tileConfig = config;
            this.placementScale = scale;
            this.mapPlane = mapPlane;

            gridPosition = mapPlane.WorldToGridPosition(position);
            previousTileInstance = mapPlane.GetTileAtGridPosition(gridPosition);
            if (previousTileInstance != null)
            {
                hadPreviousTile = true;
                previousTileConfig = mapPlane.GetTileDataAtGridPosition(gridPosition);
                previousRotation = previousTileInstance.transform.rotation;
            }
        }

        public void Execute()
        {
            mapPlane.PlaceTile(gridPosition, tileConfig, placementRotation);
        }

        public void Undo()
        {
            if (hadPreviousTile)
            {
                mapPlane.PlaceTile(gridPosition, previousTileConfig, previousRotation);
            }
            else
            {
                mapPlane.RemoveTile(gridPosition);
            }
        }

        public void ClearCommand()
        {

        }
    }

    public class RemoveTileCommand : ICommand
    {
        private Vector3 placementPosition;
        Vector2Int gridPosition;

        private GameObject previousTileInstance;
        private Quaternion previousRotation;
        private TileData previousTileConfig;
        private bool hadPreviousTile = false;

        private MapPlane mapPlane;

        public RemoveTileCommand(Vector3 position, MapPlane mapPlane)
        {
            this.placementPosition = position;
            this.mapPlane = mapPlane;

            gridPosition = mapPlane.WorldToGridPosition(position);
            previousTileInstance = mapPlane.GetTileAtGridPosition(gridPosition);
            if (previousTileInstance != null)
            {
                hadPreviousTile = true;
                previousTileConfig = mapPlane.GetTileDataAtGridPosition(gridPosition);
                previousRotation = previousTileInstance.transform.rotation;
            }
        }

        public void Execute()
        {
            mapPlane.RemoveTile(gridPosition);
        }

        public void Undo()
        {
            if (hadPreviousTile)
            {
                mapPlane.PlaceTile(gridPosition, previousTileConfig, previousRotation);
            }
        }

        public void ClearCommand()
        {

        }
    }

    public class PlaceWallCommand : ICommand
    {
        private Vector2Int gridPosition;
        private WallDirection wallDirection;
        private WallData wallData;

        private int previousWallID = -1;
        private bool hadPreviousWall = false;

        private MapPlane mapPlane;

        public PlaceWallCommand(Vector2Int gridPosition, WallDirection direction, WallData wallData, MapPlane mapPlane)
        {
            this.gridPosition = gridPosition;
            this.wallDirection = direction;
            this.wallData = wallData;
            this.mapPlane = mapPlane;

            previousWallID = mapPlane.mapData.GetWall(gridPosition, direction);
            hadPreviousWall = previousWallID != -1;
        }

        public void Execute()
        {
            mapPlane.PlaceWall(gridPosition, wallData, wallDirection);
        }

        public void Undo()
        {
            if (hadPreviousWall)
            {
                WallData previousWallData = mapPlane.wallDataList.GetTileDetails(previousWallID);
                if (previousWallData != null)
                {
                    mapPlane.PlaceWall(gridPosition, previousWallData, wallDirection);
                }
            }
            else
            {
                mapPlane.RemoveWall(gridPosition, wallDirection);
            }
        }

        public void ClearCommand() { }
    }

    public class RemoveWallCommand : ICommand
    {
        private Vector2Int gridPosition;
        private WallDirection wallDirection;

        private int previousWallID = -1;
        private WallData previousWallData;
        private bool hadPreviousWall = false;

        private MapPlane mapPlane;

        public RemoveWallCommand(Vector2Int gridPosition, WallDirection direction, MapPlane mapPlane)
        {
            this.gridPosition = gridPosition;
            this.wallDirection = direction;
            this.mapPlane = mapPlane;

            previousWallID = mapPlane.mapData.GetWall(gridPosition, direction);
            hadPreviousWall = previousWallID != -1;

            if (hadPreviousWall)
            {
                previousWallData = mapPlane.wallDataList.GetTileDetails(previousWallID);
            }
        }

        public void Execute()
        {
            mapPlane.RemoveWall(gridPosition, wallDirection);
        }

        public void Undo()
        {
            if (hadPreviousWall && previousWallData != null)
            {
                mapPlane.PlaceWall(gridPosition, previousWallData, wallDirection);
            }
        }

        public void ClearCommand() { }
    }



    public class PlaceObjectCommand : ICommand
    {
        private Vector3 placementPosition;
        private Vector2Int gridPosition;
        private int height;
        private Quaternion placementRotation;
        private Vector3 placementScale;
        private ObjData objConfig;
        private MapPlane mapPlane;

        private bool isGlobalUnique;
        private string previousMapID;
        private Vector2Int previousGridPos;
        private int previousHeight;
        private Quaternion previousRotation;
        private MapPlane previousMapPlane;
        private string previousInstanceID;
        private GameObject uniqueObject;
        private bool isPlayer;

        public PlaceObjectCommand(Vector3 position, Vector2Int gridPos, int height, Quaternion rotation, ObjData config, MapPlane mapPlane, Vector3 scale)
        {
            this.placementPosition = position;
            this.gridPosition = gridPos;
            this.height = height;
            this.placementRotation = rotation;
            this.placementScale = scale;
            this.objConfig = config;
            this.mapPlane = mapPlane;

            this.isGlobalUnique = config.maxCount == 1 || config.objType.HasFlag(ObjectType.Player);
            this.isPlayer = config.objType.HasFlag(ObjectType.Player);

            if (isGlobalUnique && mapPlane.levelData != null)
            {
                foreach (var map in mapPlane.levelData.insideWorld.Concat(mapPlane.levelData.outsideWorld))
                {
                    foreach (var instance in map.GetAllObjectInstances())
                    {
                        if (instance.objID == config.objID)
                        {
                            previousMapID = map.mapID;
                            previousGridPos = instance.mainGrid;
                            previousHeight = instance.height;
                            previousRotation = instance.rotation;
                            previousInstanceID = instance.instanceID;

                            var mapMgr = MapMgr.Instance;
                            if (mapMgr != null)
                            {
                                var temp = mapMgr.GetMapPlanes();
                                foreach (var plane in temp)
                                {
                                    if (plane.mapData == map)
                                    {
                                        previousMapPlane = plane;
                                        uniqueObject = plane.GetObjectAtGridPosition(previousGridPos, previousHeight);
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    }
                    if (previousMapPlane != null) break;
                }
            }
        }

        public void Execute()
        {
            if (isGlobalUnique && uniqueObject != null)
            {
                if (previousMapPlane != null)
                {
                    Vector3Int previousGrid3D = new Vector3Int(previousGridPos.x, previousGridPos.y, previousHeight);
                    previousMapPlane.mapData.RemoveObject(previousGrid3D);
                    previousMapPlane.spawnedObjects.Remove(uniqueObject);
                    previousMapPlane.instanceToGameObject.Remove(previousInstanceID);

                    Vector3 worldPosition = mapPlane.GridToWorldPosition(gridPosition);
                    worldPosition.y = height * mapPlane.mapData.cellSize;
                    uniqueObject.transform.position = worldPosition;
                    uniqueObject.transform.rotation = placementRotation;

                    if (previousMapPlane != mapPlane)
                    {
                        uniqueObject.transform.parent = mapPlane.transform;
                        mapPlane.spawnedObjects.Add(uniqueObject);
                    }

                    string newInstanceID = mapPlane.mapData.PlaceObject(gridPosition, objConfig, height, placementRotation);
                    mapPlane.instanceToGameObject[newInstanceID] = uniqueObject;

                    if (isPlayer && mapPlane.levelData != null)
                    {
                        mapPlane.levelData.playerStart.startMapID = mapPlane.mapData.mapID;
                        mapPlane.levelData.playerStart.startGridPos = gridPosition;
                    }
                }
            }
            else
            {
                mapPlane.PlaceObject(gridPosition, objConfig, placementRotation, height, placementScale);
                if (isPlayer && mapPlane.levelData != null)
                {
                    mapPlane.levelData.playerStart.startMapID = mapPlane.mapData.mapID;
                    mapPlane.levelData.playerStart.startGridPos = gridPosition;
                }
            }
        }

        public void Undo()
        {
            if (isGlobalUnique && uniqueObject != null && previousMapPlane != null)
            {
                Vector3Int currentGrid3D = new Vector3Int(gridPosition.x, gridPosition.y, height);
                mapPlane.mapData.RemoveObject(currentGrid3D);
                mapPlane.spawnedObjects.Remove(uniqueObject);

                string currentInstanceID = mapPlane.mapData.GetInstanceIDAtGrid(currentGrid3D);
                if (!string.IsNullOrEmpty(currentInstanceID))
                {
                    mapPlane.instanceToGameObject.Remove(currentInstanceID);
                }

                Vector3 previousWorldPosition = previousMapPlane.GridToWorldPosition(previousGridPos);
                previousWorldPosition.y = previousHeight * previousMapPlane.mapData.cellSize;
                uniqueObject.transform.position = previousWorldPosition;
                uniqueObject.transform.rotation = previousRotation;

                if (previousMapPlane != mapPlane)
                {
                    uniqueObject.transform.parent = previousMapPlane.transform;
                    previousMapPlane.spawnedObjects.Add(uniqueObject);
                }

                string newInstanceID = previousMapPlane.mapData.PlaceObject(previousGridPos, objConfig, previousHeight, previousRotation);
                previousMapPlane.instanceToGameObject[newInstanceID] = uniqueObject;

                if (isPlayer && mapPlane.levelData != null)
                {
                    mapPlane.levelData.playerStart.startMapID = previousMapID;
                    mapPlane.levelData.playerStart.startGridPos = previousGridPos;
                }
            }
            else
            {
                mapPlane.RemoveObject(gridPosition, height);
            }
        }

        public void ClearCommand()
        {
        }
    }

    public class RemoveObjectCommand : ICommand
    {
        private Vector3 placementPosition;
        private Vector2Int gridPosition;
        private int height;
        private MapPlane mapPlane;

        private ObjData previousObjConfig;
        private Quaternion previousRotation;
        private bool hadPreviousObject = false;

        private bool wasPlayer;
        private string previousMapID;
        private Vector2Int previousPlayerPos;

        public RemoveObjectCommand(Vector3 position, Vector2Int gridPos, int height, MapPlane mapPlane)
        {
            this.placementPosition = position;
            this.gridPosition = gridPos;
            this.height = height;
            this.mapPlane = mapPlane;

            previousObjConfig = mapPlane.GetObjectDataAtGridPosition(gridPosition, height);
            if (previousObjConfig != null)
            {
                hadPreviousObject = true;
                GameObject previousObj = mapPlane.GetObjectAtGridPosition(gridPosition, height);
                if (previousObj != null)
                {
                    previousRotation = previousObj.transform.rotation;
                }

                wasPlayer = previousObjConfig.objType.HasFlag(ObjectType.Player);
                if (wasPlayer && mapPlane.levelData != null && mapPlane.levelData.playerStart != null)
                {
                    previousMapID = mapPlane.levelData.playerStart.startMapID;
                    previousPlayerPos = mapPlane.levelData.playerStart.startGridPos;
                }
            }
        }

        public void Execute()
        {
            mapPlane.RemoveObject(gridPosition, height);
        }

        public void Undo()
        {
            if (hadPreviousObject)
            {
                mapPlane.PlaceObject(gridPosition, previousObjConfig, previousRotation, height);
                if (wasPlayer)
                {
                    mapPlane.levelData.playerStart.startMapID = previousMapID;
                    mapPlane.levelData.playerStart.startGridPos = previousPlayerPos;
                }
            }
        }

        public void ClearCommand()
        {
        }
    }

    #endregion
    #region gameplay
    public class MoveObjectCommand : ICommand
    {
        private GameObject targetObject;
        private MapPlane map;
        private Vector2Int fromPos;
        private Vector2Int toPos;
        int manhattanDistance;
        private int height;
        private Quaternion fromRotation;
        private Quaternion toRotation;
        private string instanceID;
        private ObjDataInstance objInstance;
        private List<Vector3Int> originalOccupiedGrids;
        private List<Vector3Int> targetOccupiedGrids;
        bool isPlayer;
        private bool isTrailAsObstacle;
        private bool isTrailAsRoad;

        private List<Vector3Int> addedTrailStep;
        private List<Vector3Int> removedTrailStep;




        public MoveObjectCommand(GameObject obj, ObjDataInstance instance, Vector2Int from, Vector2Int to, int height, Quaternion fromRot, Quaternion toRot, MapPlane map, bool player)
        {
            this.targetObject = obj;
            this.objInstance = instance;
            this.fromPos = from;
            this.toPos = to;
            this.height = height;
            this.fromRotation = fromRot;
            this.toRotation = toRot;
            this.map = map;
            this.isPlayer = player;
            this.instanceID = instance.instanceID;
            manhattanDistance = Mathf.Abs(fromPos.x - toPos.x) + Mathf.Abs(fromPos.y - toPos.y);

            this.originalOccupiedGrids = new List<Vector3Int>(instance.currentOccupied);
            this.targetOccupiedGrids = new List<Vector3Int>();
            foreach (var offset in map.mapData.GetObjectData(instance.objID).occupiedGrids)
            {
                Vector3Int baseOffset = new Vector3Int(offset.x, offset.z, offset.y);
                Vector3Int rotatedOffset = instance.RotateOffset(baseOffset, toRotation);

                Vector3Int finalPos = new Vector3Int(
                    toPos.x + rotatedOffset.x,
                    toPos.y + rotatedOffset.z,
                    height + rotatedOffset.y
                );

                targetOccupiedGrids.Add(finalPos);
            }

            var objData = map.mapData.GetObjectData(instance.objID);
            isTrailAsObstacle = objData.objType.HasFlag(ObjectType.TrailAsObstacle);
            isTrailAsRoad = objData.objType.HasFlag(ObjectType.TrailAsRoad);
            addedTrailStep = new List<Vector3Int>(targetOccupiedGrids);
        }

        public void Execute()
        {
            if (string.IsNullOrEmpty(instanceID) || objInstance == null) return;

            foreach (var grid in originalOccupiedGrids)
            {
                map.mapData.RemoveGridMapping(grid);
            }
            
            objInstance.currentOccupied = new List<Vector3Int>(targetOccupiedGrids);
            //Debug.Log(objInstance.currentOccupied);
            objInstance.mainGrid = toPos;
            objInstance.height = height;
            objInstance.rotation = toRotation;

            foreach (var grid in targetOccupiedGrids)
            {
                map.mapData.UpdateGridMapping(grid, instanceID);
            }

            Vector3 worldPos = map.GridToWorldPosition(toPos);
            worldPos.y = height * map.mapData.cellSize;
            //targetObject.transform.position = worldPos;
            targetObject.GetComponent<MovableObj>().SetMap(map);
            targetObject.GetComponent<MovableObj>().PlayMoveFB(toPos, worldPos, toRotation, manhattanDistance);
            //targetObject.transform.rotation = toRotation;
            if (isPlayer)
            {
                GameMgr.Instance.UpdatePlayerPosition(toPos);
                if (isTrailAsRoad)
                {
                    GameMgr.Instance.RecordPlayerStep(instanceID, addedTrailStep, out removedTrailStep);
                }
            }

            if (isTrailAsObstacle)
            {
                GameMgr.Instance.AddTrailStep(instanceID, addedTrailStep, out removedTrailStep);
            }
        }

        public void Undo()
        {
            if (string.IsNullOrEmpty(instanceID) || objInstance == null) return;

            foreach (var grid in targetOccupiedGrids)
            {
                map.mapData.RemoveGridMapping(grid);
            }

            objInstance.mainGrid = fromPos;
            objInstance.height = height;
            objInstance.rotation = fromRotation;
            objInstance.currentOccupied = new List<Vector3Int>(originalOccupiedGrids);

            foreach (var grid in originalOccupiedGrids)
            {
                map.mapData.UpdateGridMapping(grid, instanceID);

            }

            Vector3 worldPos = map.GridToWorldPosition(fromPos);
            worldPos.y = height * map.mapData.cellSize;
            targetObject.GetComponent<MovableObj>().PlayUndoMoveFB(fromPos, worldPos, fromRotation, manhattanDistance);
            // targetObject.transform.position = worldPos;
            // targetObject.transform.rotation = fromRotation;
            if (isPlayer)
            {
                GameMgr gm = GameMgr.Instance;
                gm.UpdatePlayerPosition(fromPos);
                if (isTrailAsRoad)
                {
                    gm.RevertPlayerStep(instanceID, addedTrailStep, removedTrailStep);
                }
            }
            if (isTrailAsObstacle)
            {
                GameMgr.Instance.RevertTrailStep(instanceID, addedTrailStep, removedTrailStep);
            }
        }

        public void ClearCommand()
        {
            targetObject = null;
            map = null;
            objInstance = null;
            originalOccupiedGrids = null;
            targetOccupiedGrids = null;
        }
    }

    public class PossessCommand : ICommand
    {
        public GameObject originalPlayer;
        private ObjDataInstance originalObjData;
        private MapPlane originalMap;
        private GameObject possessedObject;
        private ObjDataInstance possessedObjData;
        private MapPlane possessedMap;
        private IMovementStrategy pre;
        private IMovementStrategy after;

        public PossessCommand(GameObject player, GameObject possessedObject,
        MapPlane originalMap, MapPlane possessedMap, ObjDataInstance originalObjData, ObjDataInstance possessedObjData)
        {
            this.originalPlayer = player;
            this.possessedObject = possessedObject;
            this.originalMap = originalMap;
            this.possessedMap = possessedMap;
            this.originalObjData = originalObjData;
            this.possessedObjData = possessedObjData;
            pre = GameMgr.Instance.movementStrategy;

            if (possessedObjData.objID == 9)
            {
                BarrelMove barrelMove = possessedObject.GetComponent<BarrelMove>();
                if (barrelMove != null)
                {
                    after = barrelMove.CurrentMovementStartegy();
                    return;
                }
            }

            after = MovementStrategyFactory.Instance.GetStrategy(possessedMap.GetObjectDataAtGridPosition(possessedObjData.mainGrid).objType);

        }

        public void Execute()
        {
            GameMgr.Instance.DisAbleOriPlayer();
            //originalPlayer.SetActive(false);
            GameMgr.Instance.SetMapAndCurrentObj(possessedMap, possessedObject, possessedObjData.mainGrid, possessedObjData,0.8f);
            GameMgr.Instance.ChangeMovementStrategy(after);
        }

        public void Undo()
        {
            GameMgr.Instance.EnAbleOriPlayer();
            //originalPlayer.SetActive(true);
            GameMgr.Instance.SetMapAndCurrentObj(originalMap, originalPlayer, originalObjData.mainGrid, originalObjData);
            GameMgr.Instance.ChangeMovementStrategy(pre);
        }

        public void ClearCommand()
        {

        }
    }

    public class ExitPossessCommand : ICommand
    {
        private GameObject originalPlayer;
        private GameObject possessedObject;
        private MapPlane originalMap;
        private MapPlane possessedMap;
        private ObjDataInstance originalObjData;
        private ObjDataInstance possessedObjData;
        private IMovementStrategy pre;
        private IMovementStrategy after;

        private Vector2Int fromGrid;
        private Vector2Int toGrid;
        private int height;
        private Quaternion rotation;
        private List<Vector3Int> originalOccupiedGrids;
        private List<Vector3Int> targetOccupiedGrids;
        private string instanceID;

        public ExitPossessCommand(GameObject originalPlayer, GameObject possessedObject,
            MapPlane originalMap, MapPlane possessedMap,
            ObjDataInstance originalObjData, ObjDataInstance possessedObjData)
        {
            this.originalPlayer = originalPlayer;
            this.possessedObject = possessedObject;
            this.originalMap = originalMap;
            this.possessedMap = possessedMap;
            this.originalObjData = originalObjData;
            this.possessedObjData = possessedObjData;

            this.pre = GameMgr.Instance.movementStrategy;
            this.after = MovementStrategyFactory.Instance.GetStrategy(originalMap.GetObjectDataAtGridPosition(originalObjData.mainGrid).objType);

            this.instanceID = originalObjData.instanceID;

            fromGrid = originalObjData.mainGrid;
            toGrid = possessedObjData.mainGrid;
            height = possessedObjData.height;
            rotation = possessedObjData.rotation;

            originalOccupiedGrids = originalObjData.currentOccupied;
            targetOccupiedGrids = new List<Vector3Int>();

            foreach (var offset in originalMap.mapData.GetObjectData(originalObjData.objID).occupiedGrids)
            {
                Vector3Int baseOffset = new Vector3Int(offset.x, offset.z, offset.y);
                Vector3Int rotatedOffset = originalObjData.RotateOffset(baseOffset, rotation);

                Vector3Int finalPos = new Vector3Int(
                    toGrid.x + rotatedOffset.x,
                    toGrid.y + rotatedOffset.z,
                    height + rotatedOffset.y
                );
                targetOccupiedGrids.Add(finalPos);
            }
        }

        public void Execute()
        {
            foreach (var grid in originalOccupiedGrids)
            {
                originalMap.mapData.RemoveGridMapping(grid);
            }

            originalObjData.mainGrid = toGrid;
            originalObjData.height = height;
            originalObjData.rotation = rotation;
            originalObjData.currentOccupied = targetOccupiedGrids;

            foreach (var grid in targetOccupiedGrids)
            {
                originalMap.mapData.UpdateGridMapping(grid, instanceID);
            }

            // Vector3 worldPos = originalMap.GridToWorldPosition(toGrid);
            // worldPos.y = height * originalMap.mapData.cellSize;

            // originalPlayer.transform.position = worldPos;
            // originalPlayer.transform.rotation = rotation;
            GameMgr.Instance.EnAbleOriPlayer();
            //originalPlayer.SetActive(true);

            GameMgr.Instance.SetMapAndCurrentObj(originalMap, originalPlayer, toGrid, originalObjData);
            GameMgr.Instance.UpdatePlayerPosition(toGrid);
            GameMgr.Instance.ChangeMovementStrategy(after);
        }

        public void Undo()
        {
            foreach (var grid in targetOccupiedGrids)
            {
                originalMap.mapData.RemoveGridMapping(grid);
            }
            originalObjData.mainGrid = fromGrid;
            foreach (var grid in originalOccupiedGrids)
            {
                originalMap.mapData.UpdateGridMapping(grid, instanceID);
            }

            GameMgr.Instance.DisAbleOriPlayer(false);

            //originalPlayer.SetActive(false);
            GameMgr.Instance.SetMapAndCurrentObj(possessedMap, possessedObject, possessedObjData.mainGrid, possessedObjData);
            GameMgr.Instance.ChangeMovementStrategy(pre);
        }

        public void ClearCommand()
        {
        }
    }


    public class BarkCommand : ICommand
    {
        private readonly Vector2Int playerGridPosition;
        private readonly Vector2Int[] directions = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

        private readonly MapData mapData;
        private readonly MapPlane map;

        private readonly List<IMovementStrategy> attractObjStrategies = new List<IMovementStrategy>();
        List<Vector2Int> moveDirs = new List<Vector2Int>();
        public bool canBark;
        CompositeCommand compositeCommand;

        public BarkCommand(MapData mapData, MapPlane map, Vector2Int playerGridPosition, bool autoAttracted = false)
        {
            this.mapData = mapData;
            this.playerGridPosition = playerGridPosition;
            //Debug.Log("playerGridPositio"+playerGridPosition);
            this.map = map;
            canBark = false;
            attractObjStrategies.Clear();

            foreach (var dir in directions)
            {
                Vector2Int checkPos = playerGridPosition + dir;

                while (mapData.IsWithinGrid(new Vector2Int(checkPos.x, checkPos.y)))
                {
                    var instance = mapData.GetObjDataInstanceAtGrid(new Vector3Int(checkPos.x, checkPos.y, 0));
                    if (instance != null)
                    {
                        ObjData targetObjData = mapData.GetObjectData(instance.objID);
                        if (targetObjData != null)
                        {
                            if ((autoAttracted && targetObjData.objType.HasFlag(ObjectType.AutoAttracted))
                            || (!autoAttracted && targetObjData.objType.HasFlag(ObjectType.Attracted)))
                            {
                                var rollingMovement = new RollingMovementStrategy();
                                Vector2Int targetPos = rollingMovement.CalculateTargetPosition(instance.mainGrid, -dir, instance.height, map, false);
                                if (targetPos != instance.mainGrid)
                                {
                                    //Debug.Log($"[{instance.instanceID}] main: {instance.mainGrid} at height {instance.height}, checkPos: {checkPos}, targetPos: {targetPos}");
                                    canBark = true;

                                    attractObjStrategies.Add(rollingMovement);
                                    moveDirs.Add(-dir);
                                    break;
                                }
                                // else
                                // {
                                //     GameMgr.Instance.PlaySpiritCantMoveFB();
                                // }
                            }

                        }
                    }
                    checkPos += dir;
                }
            }

            if (canBark)
            {
                compositeCommand = new CompositeCommand();
                //Debug.Log(attractObjStrategies.Count);
                for (int i = 0; i < attractObjStrategies.Count; i++)
                {
                    compositeCommand.AddCommand(attractObjStrategies[i].CreateMoveCommand(moveDirs[i], map, false));
                }
            }
        }

        public void Execute()
        {
            compositeCommand.Execute();
        }

        public void Undo()
        {
            compositeCommand.Undo();
        }
        public void ClearCommand()
        {

        }
    }

    public class ChangeMovementStrategy : ICommand
    {
        private IMovementStrategy pre;
        private IMovementStrategy after;
        public ChangeMovementStrategy(IMovementStrategy pre, IMovementStrategy after)
        {
            this.pre = pre;
            this.after = after;
        }
        public void Execute()
        {
            GameMgr.Instance.ChangeMovementStrategy(after);
        }
        public void Undo()
        {
            GameMgr.Instance.ChangeMovementStrategy(pre);
        }
        public void ClearCommand()
        {

        }
    }
    #endregion
    public class ChangeBarrelMoveCommand : ICommand
    {
        private BarrelMove barrelMove;
        Vector2Int move;
        public ChangeBarrelMoveCommand(BarrelMove barrelMove,Vector2Int moveDir)
        {
            this.barrelMove = barrelMove;
            move=moveDir;
        }
        public void Execute()
        {
            barrelMove.SetHasMove(move);
        }
        public void Undo()
        {
            barrelMove.ResetMove();

        }
        public void ClearCommand()
        {

        }
    }
    public class EmptyCommand : ICommand
    {
        public void Execute()
        {

        }
        public void Undo()
        {

        }
        public void ClearCommand()
        {

        }
    }
}