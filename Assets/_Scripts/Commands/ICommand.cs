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
        private readonly Dictionary<WallDirection, int> previousWalls = new Dictionary<WallDirection, int>();

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
                foreach (WallDirection direction in System.Enum.GetValues(typeof(WallDirection)))
                {
                    int wallID = mapPlane.mapData.GetWall(gridPosition, direction);
                    if (wallID != -1) previousWalls[direction] = wallID;
                }
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
                foreach (var wall in previousWalls)
                {
                    WallData wallData = mapPlane.wallDataList.GetTileDetails(wall.Value);
                    if (wallData != null) mapPlane.PlaceWall(gridPosition, wallData, wall.Key);
                }
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
        private Vector2Int previousGridPos;
        private int previousHeight;
        private Quaternion previousRotation;
        private MapPlane previousMapPlane;
        private string previousInstanceID;
        private GameObject uniqueObject;
        private bool isPlayer;
        private int channelID;
        private int previousChannelID;
        private string currentInstanceID;

        public PlaceObjectCommand(Vector3 position, Vector2Int gridPos, int height, Quaternion rotation, ObjData config, MapPlane mapPlane, Vector3 scale, int channelID = 0)
        {
            this.placementPosition = position;
            this.gridPosition = gridPos;
            this.height = height;
            this.placementRotation = rotation;
            this.placementScale = scale;
            this.channelID = channelID;
            this.objConfig = config;
            this.mapPlane = mapPlane;

            bool channelUnique = channelID > 0 &&
                (config.objType.HasFlag(ObjectType.Ghost) || config.objType.HasFlag(ObjectType.Goal));
            this.isGlobalUnique = config.maxCount == 1 || config.objType.HasFlag(ObjectType.Player) || channelUnique;
            this.isPlayer = config.objType.HasFlag(ObjectType.Player);

            if (isGlobalUnique && mapPlane.levelData != null)
            {
                var map = mapPlane.levelData.mapData;
                if (map != null)
                {
                    ObjDataInstance linkedInstance = channelUnique
                        ? map.GetLinkedInstance(config.objType, channelID)
                        : null;
                    bool hasObjectCountUniqueness = config.maxCount == 1 || config.objType.HasFlag(ObjectType.Player);
                    IEnumerable<ObjDataInstance> candidates = linkedInstance != null
                        ? new[] { linkedInstance }
                        : map.GetAllObjectInstances();
                    foreach (ObjDataInstance instance in candidates)
                    {
                        if ((channelUnique && instance == linkedInstance) ||
                            ((!channelUnique || hasObjectCountUniqueness) && instance.objID == config.objID))
                        {
                            previousGridPos = instance.mainGrid;
                            previousHeight = instance.height;
                            previousRotation = instance.rotation;
                            previousInstanceID = instance.instanceID;
                            previousChannelID = instance.channelID;

                            var mapMgr = MapMgr.Instance;
                            if (mapMgr != null)
                            {
                                var temp = mapMgr.GetMapPlanes();
                                foreach (var plane in temp)
                                {
                                    if (plane.mapData == map)
                                    {
                                        previousMapPlane = plane;
                                        uniqueObject = plane.GetObjectByInstanceID(instance.instanceID);
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }

        public void Execute()
        {
            if (isGlobalUnique && uniqueObject != null)
            {
                if (previousMapPlane != null)
                {
                    previousMapPlane.mapData.RemoveObject(previousInstanceID);
                    if (previousMapPlane != mapPlane) previousMapPlane.spawnedObjects.Remove(uniqueObject);
                    previousMapPlane.instanceToGameObject.Remove(previousInstanceID);

                    Vector3 worldPosition = mapPlane.GridToWorldPosition(gridPosition);
                    worldPosition.y = height * mapPlane.mapData.cellSize;
                    uniqueObject.transform.position = worldPosition;
                    uniqueObject.transform.rotation = placementRotation;
                    BarrelMove movedBarrel = uniqueObject.GetComponent<BarrelMove>();
                    if (movedBarrel != null) movedBarrel.InitializeOrientation(placementRotation);

                    if (previousMapPlane != mapPlane)
                    {
                        uniqueObject.transform.parent = mapPlane.transform;
                        mapPlane.spawnedObjects.Add(uniqueObject);
                    }

                    currentInstanceID = mapPlane.mapData.PlaceObject(gridPosition, objConfig, height, placementRotation, channelID);
                    mapPlane.instanceToGameObject[currentInstanceID] = uniqueObject;

                    if (isPlayer && mapPlane.levelData != null)
                    {
                        mapPlane.levelData.playerStart.startGridPos = gridPosition;
                    }
                }
            }
            else
            {
                mapPlane.PlaceObject(gridPosition, objConfig, placementRotation, height, placementScale, channelID);
                ObjDataInstance placedInstance = channelID > 0
                    ? mapPlane.mapData.GetLinkedInstance(objConfig.objType, channelID)
                    : null;
                currentInstanceID = placedInstance != null
                    ? placedInstance.instanceID
                    : mapPlane.mapData.GetInstanceIDAtGrid(new Vector3Int(gridPosition.x, gridPosition.y, height));
                if (isPlayer && mapPlane.levelData != null)
                {
                    mapPlane.levelData.playerStart.startGridPos = gridPosition;
                }
            }
        }

        public void Undo()
        {
            if (isGlobalUnique && uniqueObject != null && previousMapPlane != null)
            {
                mapPlane.mapData.RemoveObject(currentInstanceID);
                if (previousMapPlane != mapPlane) mapPlane.spawnedObjects.Remove(uniqueObject);

                if (!string.IsNullOrEmpty(currentInstanceID))
                {
                    mapPlane.instanceToGameObject.Remove(currentInstanceID);
                }

                Vector3 previousWorldPosition = previousMapPlane.GridToWorldPosition(previousGridPos);
                previousWorldPosition.y = previousHeight * previousMapPlane.mapData.cellSize;
                uniqueObject.transform.position = previousWorldPosition;
                uniqueObject.transform.rotation = previousRotation;
                BarrelMove restoredBarrel = uniqueObject.GetComponent<BarrelMove>();
                if (restoredBarrel != null) restoredBarrel.InitializeOrientation(previousRotation);

                if (previousMapPlane != mapPlane)
                {
                    uniqueObject.transform.parent = previousMapPlane.transform;
                    previousMapPlane.spawnedObjects.Add(uniqueObject);
                }

                string newInstanceID = previousMapPlane.mapData.PlaceObject(previousGridPos, objConfig, previousHeight, previousRotation, previousChannelID);
                previousMapPlane.instanceToGameObject[newInstanceID] = uniqueObject;

                if (isPlayer && mapPlane.levelData != null)
                {
                    mapPlane.levelData.playerStart.startGridPos = previousGridPos;
                }
            }
            else
            {
                if (isGlobalUnique && !string.IsNullOrEmpty(currentInstanceID))
                {
                    GameObject placedObject = mapPlane.GetObjectByInstanceID(currentInstanceID);
                    mapPlane.mapData.RemoveObject(currentInstanceID);
                    if (placedObject != null)
                    {
                        mapPlane.spawnedObjects.Remove(placedObject);
                        Object.Destroy(placedObject);
                    }
                    mapPlane.instanceToGameObject.Remove(currentInstanceID);
                }
                else
                {
                    mapPlane.RemoveObject(gridPosition, height);
                }
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
        private Vector2Int previousPlayerPos;
        private int previousChannelID;

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
                string previousInstanceID = mapPlane.mapData.GetInstanceIDAtGrid(new Vector3Int(gridPos.x, gridPos.y, height));
                ObjDataInstance previousInstance = mapPlane.mapData.GetInstance(previousInstanceID);
                previousChannelID = previousInstance == null ? 0 : previousInstance.channelID;
                GameObject previousObj = mapPlane.GetObjectAtGridPosition(gridPosition, height);
                if (previousObj != null)
                {
                    previousRotation = previousObj.transform.rotation;
                }

                wasPlayer = previousObjConfig.objType.HasFlag(ObjectType.Player);
                if (wasPlayer && mapPlane.levelData != null && mapPlane.levelData.playerStart != null)
                {
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
                mapPlane.PlaceObject(gridPosition, previousObjConfig, previousRotation, height, default, previousChannelID);
                if (wasPlayer)
                {
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

        }

        public void Execute()
        {
            if (string.IsNullOrEmpty(instanceID) || objInstance == null) return;

            foreach (var grid in originalOccupiedGrids)
            {
                map.mapData.RemoveGridMapping(grid, instanceID);
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
            MovableObj movableObj = targetObject.GetComponent<MovableObj>();
            if (movableObj != null)
            {
                movableObj.SetMap(map);
                movableObj.PlayMoveFB(toPos, worldPos, toRotation, manhattanDistance);
            }
            else
            {
                // Basic pushable props such as the crate have no animation
                // component; keep their transform in sync with the grid.
                targetObject.transform.SetPositionAndRotation(worldPos, toRotation);
            }
            if (isPlayer)
            {
                GameMgr.Instance.UpdatePlayerPosition(toPos);
            }
        }

        public void Undo()
        {
            if (string.IsNullOrEmpty(instanceID) || objInstance == null) return;

            foreach (var grid in targetOccupiedGrids)
            {
                map.mapData.RemoveGridMapping(grid, instanceID);
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
            MovableObj movableObj = targetObject.GetComponent<MovableObj>();
            if (movableObj != null)
            {
                movableObj.PlayUndoMoveFB(fromPos, worldPos, fromRotation, manhattanDistance);
            }
            else
            {
                targetObject.transform.SetPositionAndRotation(worldPos, fromRotation);
            }
            if (isPlayer)
            {
                GameMgr gm = GameMgr.Instance;
                gm.UpdatePlayerPosition(fromPos);
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

    public class BarkCommand : ICommand
    {
        private readonly Vector2Int playerGridPosition;
        private readonly Vector2Int[] directions = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

        private readonly MapData mapData;
        private readonly MapPlane map;

        private readonly List<ObjDataInstance> attractedGhosts = new List<ObjDataInstance>();
        private readonly List<Vector2Int> attractedDirections = new List<Vector2Int>();
        public bool canBark;
        CompositeCommand compositeCommand;

        public BarkCommand(MapData mapData, MapPlane map, Vector2Int playerGridPosition)
        {
            this.mapData = mapData;
            this.playerGridPosition = playerGridPosition;
            //Debug.Log("playerGridPositio"+playerGridPosition);
            this.map = map;
            canBark = false;
            attractedGhosts.Clear();
            attractedDirections.Clear();

            foreach (var dir in directions)
            {
                Vector2Int checkPos = playerGridPosition + dir;
                HashSet<string> seenGhosts = new HashSet<string>();

                while (mapData.IsWithinGrid(new Vector2Int(checkPos.x, checkPos.y)))
                {
                    var instance = mapData.GetObjDataInstanceAtGrid(new Vector3Int(checkPos.x, checkPos.y, 0));
                    ObjData targetObjData = instance == null ? null : mapData.GetObjectData(instance.objID);
                    if (targetObjData == null || !targetObjData.objType.HasFlag(ObjectType.Ghost))
                    {
                        instance = null;
                        foreach (ObjDataInstance candidate in mapData.GetAllObjectInstances())
                        {
                            ObjData candidateData = mapData.GetObjectData(candidate.objID);
                            if (candidateData != null && candidateData.objType.HasFlag(ObjectType.Ghost) &&
                                candidate.currentOccupied.Contains(new Vector3Int(checkPos.x, checkPos.y, candidate.height)))
                            {
                                instance = candidate;
                                targetObjData = candidateData;
                                break;
                            }
                        }
                    }

                    if (instance != null && targetObjData != null && targetObjData.objType.HasFlag(ObjectType.Ghost) &&
                        seenGhosts.Add(instance.instanceID))
                    {
                        attractedGhosts.Add(instance);
                        attractedDirections.Add(-dir);
                    }
                    checkPos += dir;
                }
            }

            if (attractedGhosts.Count == 0) return;

            // All ghosts in line are treated as moving together. Their starting
            // cells are ignored during planning, then destinations are packed
            // deterministically from nearest to farthest from Pomme.
            HashSet<string> movingGhostIDs = new HashSet<string>();
            foreach (ObjDataInstance ghost in attractedGhosts)
            {
                movingGhostIDs.Add(ghost.instanceID);
            }

            HashSet<Vector3Int> settledGhostGrids = new HashSet<Vector3Int>();
            compositeCommand = new CompositeCommand();
            for (int i = 0; i < attractedGhosts.Count; i++)
            {
                ObjDataInstance ghost = attractedGhosts[i];
                Vector2Int moveDir = attractedDirections[i];
                Vector2Int destination = FindGhostDestination(ghost, moveDir, movingGhostIDs, settledGhostGrids);
                ObjData ghostData = mapData.GetObjectData(ghost.objID);
                bool blocksMovement = ghostData != null && ghostData.objType.HasFlag(ObjectType.BlocksMovement);
                if (blocksMovement)
                {
                    foreach (Vector3Int occupied in ghost.currentOccupied)
                    {
                        Vector3Int offset = occupied - new Vector3Int(ghost.mainGrid.x, ghost.mainGrid.y, ghost.height);
                        settledGhostGrids.Add(new Vector3Int(destination.x, destination.y, ghost.height) + offset);
                    }
                }

                if (destination == ghost.mainGrid) continue;
                GameObject ghostObject = map.GetObjectByInstanceID(ghost.instanceID);
                if (ghostObject == null) continue;

                ICommand moveCommand = new MoveObjectCommand(
                    ghostObject,
                    ghost,
                    ghost.mainGrid,
                    destination,
                    ghost.height,
                    ghostObject.transform.rotation,
                    ghostObject.transform.rotation,
                    map,
                    false);
                compositeCommand.AddCommand(moveCommand);
                canBark = true;
            }
        }

        Vector2Int FindGhostDestination(ObjDataInstance ghost, Vector2Int moveDir,
            HashSet<string> movingGhostIDs, HashSet<Vector3Int> settledGhostGrids)
        {
            Vector2Int destination = ghost.mainGrid;
            Vector3Int shift = new Vector3Int(moveDir.x, moveDir.y, 0);
            ObjData ghostData = mapData.GetObjectData(ghost.objID);
            bool ghostBlocksMovement = ghostData != null &&
                ghostData.objType.HasFlag(ObjectType.BlocksMovement);

            while (true)
            {
                Vector2Int nextMain = destination + moveDir;
                bool canMove = true;
                foreach (Vector3Int occupied in ghost.currentOccupied)
                {
                    Vector3Int offset = occupied - new Vector3Int(ghost.mainGrid.x, ghost.mainGrid.y, ghost.height);
                    Vector3Int nextGrid = new Vector3Int(nextMain.x, nextMain.y, ghost.height) + offset;
                    Vector3Int currentGrid = nextGrid - shift;
                    Vector2Int next2D = new Vector2Int(nextGrid.x, nextGrid.y);

                    if (!mapData.IsWithinGrid(next2D) ||
                        !mapData.IsGhostWalkable(next2D) ||
                        mapData.HasWallBetween(new Vector2Int(currentGrid.x, currentGrid.y), next2D))
                    {
                        canMove = false;
                        break;
                    }

                    if (ghostBlocksMovement && settledGhostGrids.Contains(nextGrid))
                    {
                        canMove = false;
                        break;
                    }

                    string blockingID = mapData.GetInstanceIDAtGrid(nextGrid);
                    if (!string.IsNullOrEmpty(blockingID) &&
                        blockingID != ghost.instanceID &&
                        !movingGhostIDs.Contains(blockingID))
                    {
                        ObjDataInstance blockingInstance = mapData.GetInstance(blockingID);
                        ObjData blockingData = blockingInstance == null
                            ? null
                            : mapData.GetObjectData(blockingInstance.objID);
                        if (blockingData == null ||
                            !MapData.CanShareMovementGrid(ghostData == null ? ObjectType.None : ghostData.objType,
                                blockingData.objType))
                        {
                            canMove = false;
                            break;
                        }
                    }
                }

                if (!canMove) break;
                destination = nextMain;
            }

            return destination;
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
