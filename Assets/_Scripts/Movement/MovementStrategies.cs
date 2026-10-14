using UnityEngine;
using Commands;
using System.Collections.Generic;

namespace Movement
{

    public class SimpleMovementStrategy : IMovementStrategy
    {
        public GameObject gameObject;
        public ObjDataInstance objInstance;
        Vector2Int from;
        Vector2Int to;
        Quaternion currentRotation;

        bool pushObj;
        ObjDataInstance targetInstance;

        public List<IMovementStrategy> pushObjStrategies = new List<IMovementStrategy>();

        public ICommand CreateMoveCommand(Vector2Int moveDir, MapPlane map, bool player)
        {

            MoveObjectCommand moveObjectCommand = new MoveObjectCommand(
                gameObject,
                objInstance,
                from,
                to,
                objInstance.height,
                currentRotation,
                currentRotation,
                map,
                player
            );

            if (player && pushObj)
            {
                CompositeCommand compositeCommand = new CompositeCommand();

                foreach (var pushObj in pushObjStrategies)
                {
                    compositeCommand.AddCommand(pushObj.CreateMoveCommand(moveDir, map, false));
                }

                compositeCommand.AddCommand(moveObjectCommand);
                //compositeCommand.Execute();
                return compositeCommand;

            }
            //moveObjectCommand.Execute();
            return moveObjectCommand;
        }

        public bool CanMove(Vector2Int from, Vector2Int moveDir, int height, MapPlane map, bool player) //may change? set objinstance as a parameter
        {
            this.from = from;
            to = from + moveDir;
            if (!map.mapData.IsWithinGrid(to))
            {
                return false;
            }
            //wall check
            if (map.mapData.HasWallBetween(from, to))
            {
                return false;
            }


            Vector3Int fromGrid3D = new Vector3Int(from.x, from.y, height);
            string instanceID = map.mapData.GetInstanceIDAtGrid(fromGrid3D);
            if (string.IsNullOrEmpty(instanceID))
            {
                return false;
            }

            objInstance = map.mapData.GetInstance(instanceID);
            if (objInstance == null)
            {
                return false;
            }
            gameObject = map.GetObjectByInstanceID(instanceID);



            currentRotation = gameObject.transform.rotation;

            List<Vector3Int> targetOccupiedGrids = new List<Vector3Int>();
            foreach (var offset in map.mapData.GetObjectData(objInstance.objID).occupiedGrids)
            {
                Vector3Int baseOffset = new Vector3Int(offset.x, offset.z, offset.y);
                Vector3Int rotatedOffset = objInstance.RotateOffset(baseOffset, objInstance.rotation);

                Vector3Int finalPos = new Vector3Int(
                    to.x + rotatedOffset.x,
                    to.y + rotatedOffset.z,
                    height + rotatedOffset.y
                );

                targetOccupiedGrids.Add(finalPos);
            }

            //check trail bound
            ObjData selfData = map.mapData.GetObjectData(objInstance.objID);
            if (selfData.objType.HasFlag(ObjectType.TrailBound))
            {
                GameMgr gameMgr = GameMgr.Instance;
                foreach (var grid in targetOccupiedGrids)
                {
                    if (!map.mapData.HasTileType(new Vector2Int(grid.x, grid.y), TileType.NPCWalkable) && !gameMgr.IsHighlighted(grid))
                        return false;
                }
            }

            if (GameMgr.Instance.IsInsideWorld(map))
            {
                foreach (var grid in targetOccupiedGrids)
                {
                    if (GameMgr.Instance.IsBlockedByTrail(grid))
                    {
                        return false;
                    }
                }
            }

            if (!player)
            {
                foreach (var grid in targetOccupiedGrids)
                {
                    if (!map.mapData.CanMoveToGrid(grid, instanceID))
                    {
                        return false;
                    }
                }
            }
            else
            {
                pushObj = false;
                pushObjStrategies.Clear();
                foreach (var grid in targetOccupiedGrids)
                {
                    if (!map.mapData.CanWalkGrid(grid))
                    {
                        return false;
                    }

                    string targetInstanceID = map.mapData.GetInstanceIDAtGrid(grid);
                    if (!string.IsNullOrEmpty(targetInstanceID))
                    {
                        if (targetInstanceID == instanceID)
                        {
                            continue;
                        }

                        targetInstance = map.mapData.GetInstance(targetInstanceID);
                        if (targetInstance == null)
                        {
                            return false;
                        }

                        ObjData targetObjData = map.mapData.GetObjectData(targetInstance.objID);
                        if (targetObjData == null)
                        {
                            return false;
                        }
                        if (targetObjData.objType.HasFlag(ObjectType.Goal))
                        {
                            continue;
                        }


                        if (!targetObjData.objType.HasFlag(ObjectType.Pushable))
                        {
                            return false;
                        }
                        //Debug.Log("TRYPUSHOBJ");

                        IMovementStrategy strategy = MovementStrategyFactory.Instance.GetStrategy(targetObjData.objType);
                        Vector2Int targetPos = strategy.CalculateTargetPosition(targetInstance.mainGrid, moveDir, targetInstance.height, map, false);
                        if (targetPos == targetInstance.mainGrid)
                        {
                            return false;
                        }
                        pushObjStrategies.Add(strategy);
                        pushObj = true;
                    }

                }
            }


            return true;
        }

        public Vector2Int CalculateTargetPosition(Vector2Int currentPos, Vector2Int moveDir, int height, MapPlane map, bool player)
        {
            if (CanMove(currentPos, moveDir, height, map, player))
            {
                return to;
            }
            return currentPos;
        }
    }


    public class RollingMovementStrategy : IMovementStrategy
    {

        private List<Vector2Int> movementPath = new List<Vector2Int>();

        public GameObject gameObject;
        public ObjDataInstance objInstance;
        Vector2Int from;
        Quaternion currentRotation;

        bool pushObj = false;
        ObjDataInstance targetInstance;

        public List<IMovementStrategy> pushObjStrategies = new List<IMovementStrategy>();

        public ICommand CreateMoveCommand(Vector2Int moveDir, MapPlane map, bool player)
        {
            if (movementPath == null || movementPath.Count == 0)
            {
                Debug.LogWarning("[RollingMovementStrategy] No movement path to create commands.");
                return null;
            }
            CompositeCommand compositeCommand = new CompositeCommand();
            if (player)
            {
                Vector2Int start = from;
                for (int i = 1; i < movementPath.Count; i++)
                {
                    Vector2Int end = movementPath[i];

                    MoveObjectCommand stepCommand = new MoveObjectCommand(
                        gameObject,
                        objInstance,
                        start,
                        end,
                        objInstance.height,
                        currentRotation,
                        currentRotation,
                        map,
                        player
                    );
                    stepCommand.Execute();

                    compositeCommand.AddCommand(stepCommand);
                    start = end;
                }
                compositeCommand.SetHasExcute();
                return compositeCommand;
            }
            MoveObjectCommand final = new MoveObjectCommand(
                gameObject,
                objInstance,
                from,
                movementPath[movementPath.Count - 1],
                objInstance.height,
                currentRotation,
                currentRotation,
                map,
                player
            );

            if (pushObj)
            {
                foreach (var pushObj in pushObjStrategies)
                {
                    compositeCommand.AddCommand(pushObj.CreateMoveCommand(moveDir, map, false));
                }

                compositeCommand.AddCommand(final);
                //compositeCommand.Execute();
                return compositeCommand;
            }


            //final.Execute();
            return final;
        }

        public bool CanMove(Vector2Int from, Vector2Int moveDir, int height, MapPlane map, bool player)
        {
            movementPath.Clear();
            movementPath.Add(from);

            Vector3Int fromGrid3D = new Vector3Int(from.x, from.y, height);
            this.from = from;
            string instanceID = map.mapData.GetInstanceIDAtGrid(fromGrid3D);
            if (string.IsNullOrEmpty(instanceID))
            {
                return false;
            }

            objInstance = map.mapData.GetInstance(instanceID);
            if (objInstance == null)
            {
                return false;
            }

            gameObject = map.GetObjectByInstanceID(instanceID);

            currentRotation = gameObject.transform.rotation;

            Vector2Int currentPos = from;
            ObjData selfData = map.mapData.GetObjectData(objInstance.objID);
            GameMgr gameMgr = GameMgr.Instance;
            bool trailBound = selfData.objType.HasFlag(ObjectType.TrailBound);
            bool inside = gameMgr.IsInsideWorld(map);
            bool firstStep = true;
            bool isNpc = selfData.objType.HasFlag(ObjectType.NPC);



            while (true)
            {
                Vector2Int nextPos = currentPos + moveDir;

                if (!map.mapData.IsWithinGrid(nextPos))
                {
                    break;
                }

                if (map.mapData.HasWallBetween(currentPos, nextPos))
                {
                    break;
                }

                bool canMoveToNext = true;
                List<Vector3Int> targetOccupiedGrids = new List<Vector3Int>();
                foreach (var offset in map.mapData.GetObjectData(objInstance.objID).occupiedGrids)
                {
                    Vector3Int baseOffset = new Vector3Int(offset.x, offset.z, offset.y);
                    Vector3Int rotatedOffset = objInstance.RotateOffset(baseOffset, objInstance.rotation);

                    Vector3Int finalPos = new Vector3Int(
                        nextPos.x + rotatedOffset.x,
                        nextPos.y + rotatedOffset.z,
                        height + rotatedOffset.y
                    );

                    targetOccupiedGrids.Add(finalPos);
                }

                if (firstStep)
                {
                    firstStep = false;
                    if (isNpc)
                    {
                        pushObj = false;
                        pushObjStrategies.Clear();
                        foreach (var grid in targetOccupiedGrids)
                        {
                            if (!map.mapData.CanWalkGrid(grid))
                            {
                                return false;
                            }
                            if (trailBound)
                            {
                                if (!map.mapData.HasTileType(new Vector2Int(grid.x, grid.y), TileType.NPCWalkable) && !gameMgr.IsHighlighted(grid))
                                {
                                    canMoveToNext = false;
                                    break;
                                }
                            }
                            if (inside)
                            {
                                if (gameMgr.IsBlockedByTrail(grid))
                                {
                                    canMoveToNext = false;
                                    break;
                                }
                            }

                            string targetInstanceID = map.mapData.GetInstanceIDAtGrid(grid);
                            if (!string.IsNullOrEmpty(targetInstanceID))
                            {
                                if (targetInstanceID == instanceID)
                                {
                                    continue;
                                }

                                targetInstance = map.mapData.GetInstance(targetInstanceID);
                                if (targetInstance == null)
                                {
                                    return false;
                                }

                                ObjData targetObjData = map.mapData.GetObjectData(targetInstance.objID);
                                if (targetObjData == null)
                                {
                                    return false;
                                }
                                if (targetObjData.objType.HasFlag(ObjectType.Goal))
                                {
                                    continue;
                                }

                                if (!targetObjData.objType.HasFlag(ObjectType.Pushable))
                                {
                                    return false;
                                }
                                //Debug.Log("TRYPUSHOBJ");

                                IMovementStrategy strategy = MovementStrategyFactory.Instance.GetStrategy(targetObjData.objType);
                                Vector2Int targetPos = strategy.CalculateTargetPosition(targetInstance.mainGrid, moveDir, targetInstance.height, map, false);
                                if (targetPos == targetInstance.mainGrid)
                                {
                                    return false;
                                }
                                pushObjStrategies.Add(strategy);
                                pushObj = true;
                            }

                        }

                        if (pushObj)
                        {
                            movementPath.Add(nextPos);
                            break;
                        }
                    }
                }

                foreach (var grid in targetOccupiedGrids)
                {
                    if (!map.mapData.CanMoveToGrid(grid, instanceID))
                    {
                        canMoveToNext = false;
                        break;
                    }
                    if (trailBound)
                    {
                        if (!map.mapData.HasTileType(new Vector2Int(grid.x, grid.y), TileType.NPCWalkable) && !gameMgr.IsHighlighted(grid))
                        {
                            canMoveToNext = false;
                            break;
                        }
                    }
                    if (inside)
                    {
                        if (gameMgr.IsBlockedByTrail(grid))
                        {
                            canMoveToNext = false;
                            break;
                        }
                    }

                }


                if (!canMoveToNext)
                {
                    break;
                }

                movementPath.Add(nextPos);
                currentPos = nextPos;
            }

            return movementPath.Count > 1;
        }

        public Vector2Int CalculateTargetPosition(Vector2Int currentPos, Vector2Int moveDir, int height, MapPlane map, bool player)
        {
            if (CanMove(currentPos, moveDir, height, map, player))
            {
                return movementPath[movementPath.Count - 1];
            }
            return currentPos;
        }
    }

    public class JumpMovementStrategy : IMovementStrategy
    {
        private Vector2Int to;
        private int jumpDistance = 2;

        List<IMovementStrategy> movementStrategies = new List<IMovementStrategy>();

        public GameObject gameObject;
        public ObjDataInstance objInstance;
        Vector2Int from;
        Quaternion currentRotation;


        public JumpMovementStrategy(int distance = 2)
        {
            jumpDistance = Mathf.Max(1, distance);
        }

        public ICommand CreateMoveCommand(Vector2Int moveDir, MapPlane map, bool player)
        {
            MoveObjectCommand moveObjectCommand = new MoveObjectCommand(
                gameObject,
                objInstance,
                from,
                to,
                objInstance.height,
                currentRotation,
                currentRotation,
                map,
                player
            );
            //moveObjectCommand.Execute();
            return moveObjectCommand;
        }

        public bool CanMove(Vector2Int from, Vector2Int moveDir, int height, MapPlane map, bool player)
        {
            to = from + moveDir * jumpDistance;
            this.from = from;

            if (!map.mapData.IsWithinGrid(to))
            {
                return false;
            }

            Vector3Int fromGrid3D = new Vector3Int(from.x, from.y, height);
            string instanceID = map.mapData.GetInstanceIDAtGrid(fromGrid3D);
            if (string.IsNullOrEmpty(instanceID))
            {
                return false;
            }

            objInstance = map.mapData.GetInstance(instanceID);
            if (objInstance == null)
            {
                return false;
            }

            gameObject = map.GetObjectByInstanceID(instanceID);
            currentRotation = gameObject.transform.rotation;

            for (int i = 1; i < jumpDistance; i++)
            {
                Vector2Int middlePos = from + moveDir * i;
                if (!map.mapData.IsWithinGrid(middlePos))
                {
                    return false;
                }
            }

            List<Vector3Int> targetOccupiedGrids = new List<Vector3Int>();
            foreach (var offset in map.mapData.GetObjectData(objInstance.objID).occupiedGrids)
            {
                Vector3Int baseOffset = new Vector3Int(offset.x, offset.z, offset.y);
                Vector3Int rotatedOffset = objInstance.RotateOffset(baseOffset, objInstance.rotation);

                Vector3Int finalPos = new Vector3Int(
                    to.x + rotatedOffset.x,
                    to.y + rotatedOffset.z,
                    height + rotatedOffset.y
                );

                targetOccupiedGrids.Add(finalPos);
            }

            ObjData selfData = map.mapData.GetObjectData(objInstance.objID);
            GameMgr gameMgr = GameMgr.Instance;
            bool trailBound = selfData.objType.HasFlag(ObjectType.TrailBound);
            bool inside = gameMgr.IsInsideWorld(map);

            foreach (var grid in targetOccupiedGrids)
            {
                if (!map.mapData.CanMoveToGrid(grid, instanceID))
                {
                    return false;
                }

                if (trailBound)
                {
                    if (!map.mapData.HasTileType(new Vector2Int(grid.x, grid.y), TileType.NPCWalkable) && !gameMgr.IsHighlighted(grid))
                    {
                        return false;
                    }
                }
                if (inside)
                {
                    if (gameMgr.IsBlockedByTrail(grid))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public Vector2Int CalculateTargetPosition(Vector2Int currentPos, Vector2Int moveDir, int height, MapPlane map, bool player)
        {
            if (CanMove(currentPos, moveDir, height, map, player))
            {
                return to;
            }
            return currentPos;
        }
    }


}