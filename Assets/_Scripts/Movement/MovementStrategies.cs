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
        readonly List<ObjDataInstance> pushChain = new List<ObjDataInstance>();
        readonly Dictionary<string, BarrelPushPlan> barrelPushPlans = new Dictionary<string, BarrelPushPlan>();

        sealed class BarrelPushPlan
        {
            public ObjDataInstance instance;
            public List<Vector2Int> path = new List<Vector2Int>();
            public Quaternion finalRotation;
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

            if (player && pushObj)
            {
                CompositeCommand compositeCommand = new CompositeCommand();
                // The nearest crate is discovered first. Move the farthest crate
                // first so each destination is clear when its command executes.
                for (int i = pushChain.Count - 1; i >= 0; i--)
                {
                    ObjDataInstance crate = pushChain[i];
                    GameObject crateObject = map.GetObjectByInstanceID(crate.instanceID);
                    if (crateObject == null) continue;

                    if (barrelPushPlans.TryGetValue(crate.instanceID, out BarrelPushPlan barrelPlan))
                    {
                        compositeCommand.AddCommand(new BarrelPushCommand(map, crateObject, barrelPlan.instance,
                            barrelPlan.path, barrelPlan.finalRotation));
                        continue;
                    }

                    Vector2Int crateTarget = crate.mainGrid + moveDir;
                    compositeCommand.AddCommand(new MoveObjectCommand(
                        crateObject,
                        crate,
                        crate.mainGrid,
                        crateTarget,
                        crate.height,
                        crateObject.transform.rotation,
                        crateObject.transform.rotation,
                        map,
                        false));
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
            ObjDataInstance playerInstance = player
                ? map.mapData.GetInstanceAtGridWithType(fromGrid3D, ObjectType.Player)
                : null;
            string instanceID = playerInstance != null
                ? playerInstance.instanceID
                : map.mapData.GetInstanceIDAtGrid(fromGrid3D);
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
            ObjData movingObjData = map.mapData.GetObjectData(objInstance.objID);
            if (movingObjData == null) return false;
            // If a pressure-controlled blocker reappeared over this object, keep
            // the overlap trapped until the blocker is removed again.
            if (map.mapData.IsOverlappingBlockingObject(instanceID)) return false;

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
                pushChain.Clear();
                barrelPushPlans.Clear();
                HashSet<string> pushChainVisited = new HashSet<string>();
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

                        ObjDataInstance targetInstance = map.mapData.GetInstance(targetInstanceID);
                        if (targetInstance == null)
                        {
                            return false;
                        }

                        ObjData targetObjData = map.mapData.GetObjectData(targetInstance.objID);
                        if (targetObjData == null)
                        {
                            return false;
                        }
                        if (MapData.CanShareMovementGrid(movingObjData.objType, targetObjData.objType))
                        {
                            continue;
                        }
                        if (targetObjData.objType.HasFlag(ObjectType.Goal))
                        {
                            continue;
                        }


                        if (!targetObjData.objType.HasFlag(ObjectType.Pushable))
                        {
                            return false;
                        }
                        if (!TryCollectPushChain(map, targetInstance, moveDir, pushChain, pushChainVisited, true))
                        {
                            pushChain.Clear();
                            return false;
                        }
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

        bool TryCollectPushChain(MapPlane map, ObjDataInstance crate, Vector2Int moveDir,
            List<ObjDataInstance> chain, HashSet<string> visited, bool isFirstInChain)
        {
            if (crate == null || string.IsNullOrEmpty(crate.instanceID)) return false;
            if (!visited.Add(crate.instanceID)) return true;

            ObjData crateData = map.mapData.GetObjectData(crate.objID);
            if (crateData == null || !crateData.objType.HasFlag(ObjectType.Pushable) ||
                (!isFirstInChain && !crateData.objType.HasFlag(ObjectType.ChainPushable))) return false;

            GameObject crateObject = map.GetObjectByInstanceID(crate.instanceID);
            if (crateObject == null) return false;

            if (crateData.objType.HasFlag(ObjectType.Rollable))
            {
                if (crateObject.GetComponent<BarrelMove>() == null) return false;
                // Add the barrel before any object it pushes so reverse command
                // execution moves the farthest object first.
                chain.Add(crate);
                if (!TryCreateBarrelPushPlan(map, crate, moveDir, chain, visited,
                        out BarrelPushPlan barrelPlan))
                {
                    chain.RemoveAt(chain.Count - 1);
                    return false;
                }
                barrelPushPlans[crate.instanceID] = barrelPlan;
                return true;
            }

            // Store in near-to-far order; CreateMoveCommand reverses it.
            chain.Add(crate);
            Vector3Int shift = new Vector3Int(moveDir.x, moveDir.y, 0);
            foreach (Vector3Int occupiedGrid in crate.currentOccupied)
            {
                Vector3Int destination = occupiedGrid + shift;
                Vector2Int from2D = new Vector2Int(occupiedGrid.x, occupiedGrid.y);
                Vector2Int to2D = new Vector2Int(destination.x, destination.y);
                if (!map.mapData.IsWithinGrid(to2D) || map.mapData.HasWallBetween(from2D, to2D) ||
                    !map.mapData.CanWalkGrid(destination)) return false;

                string blockingID = map.mapData.GetInstanceIDAtGrid(destination);
                if (string.IsNullOrEmpty(blockingID) || blockingID == crate.instanceID) continue;

                ObjDataInstance blockingInstance = map.mapData.GetInstance(blockingID);
                if (blockingInstance == null) return false;
                ObjData blockingData = map.mapData.GetObjectData(blockingInstance.objID);
                if (blockingData == null) return false;
                if (blockingData.objType.HasFlag(ObjectType.Goal)) continue;
                if (MapData.CanShareMovementGrid(crateData.objType, blockingData.objType)) continue;
                if (!blockingData.objType.HasFlag(ObjectType.Pushable)) return false;

                if (!TryCollectPushChain(map, blockingInstance, moveDir, chain, visited, false)) return false;
            }
            return true;
        }

        bool TryCreateBarrelPushPlan(MapPlane map, ObjDataInstance barrel, Vector2Int moveDir,
            List<ObjDataInstance> chain, HashSet<string> visited, out BarrelPushPlan plan)
        {
            plan = null;
            GameObject barrelObject = map.GetObjectByInstanceID(barrel.instanceID);
            BarrelMove barrelMove = barrelObject != null ? barrelObject.GetComponent<BarrelMove>() : null;
            if (barrelMove == null) return false;
            Vector2Int barrelAxis = barrelMove.LongAxis;
            bool pushAlongHead = Mathf.Abs(moveDir.x * barrelAxis.x + moveDir.y * barrelAxis.y) == 1;
            // Standing barrels always tip over after one push. A lying barrel pushed
            // along its long axis stands up; a side push rolls until blocked.
            bool moveOneCell = barrelMove.IsStanding || pushAlongHead;
            Quaternion finalRotation = barrel.rotation;

            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int current = barrel.mainGrid;
            int maxSteps = map.mapData.gridLength + map.mapData.gridWidth + 1;
            int stepsToCheck = moveOneCell ? 1 : maxSteps;

            for (int i = 0; i < stepsToCheck; i++)
            {
                Vector2Int next = current + moveDir;
                if (!map.mapData.IsWithinGrid(next) || map.mapData.HasWallBetween(current, next) ||
                    !map.mapData.CanWalkGrid(new Vector3Int(next.x, next.y, barrel.height)))
                {
                    break;
                }

                string blockingID = map.mapData.GetInstanceIDAtGrid(new Vector3Int(next.x, next.y, barrel.height));
                if (!string.IsNullOrEmpty(blockingID) && blockingID != barrel.instanceID)
                {
                    ObjDataInstance blockingInstance = map.mapData.GetInstance(blockingID);
                    ObjData blockingData = blockingInstance != null
                        ? map.mapData.GetObjectData(blockingInstance.objID)
                        : null;
                    bool canShare = blockingData != null &&
                        (blockingData.objType.HasFlag(ObjectType.Goal) ||
                         MapData.CanShareMovementGrid(map.mapData.GetObjectData(barrel.objID).objType,
                             blockingData.objType));
                    if (!canShare)
                    {
                        // A barrel may push an immediately adjacent chain-pushable
                        // object one cell. During a roll it then stops at that object.
                        if (path.Count == 0 && blockingInstance != null &&
                            blockingData != null && blockingData.objType.HasFlag(ObjectType.Pushable) &&
                            TryCollectPushChain(map, blockingInstance, moveDir, chain, visited, false))
                        {
                            path.Add(next);
                        }
                        break;
                    }
                }

                path.Add(next);
                current = next;
            }

            if (path.Count == 0) return false;
            plan = new BarrelPushPlan
            {
                instance = barrel,
                path = path,
                finalRotation = finalRotation
            };
            return true;
        }
    }

    sealed class BarrelPushCommand : ICommand
    {
        readonly MapPlane map;
        readonly GameObject barrelObject;
        readonly ObjDataInstance barrelInstance;
        readonly List<Vector2Int> path;
        readonly Quaternion finalRotation;
        readonly List<ICommand> executedMoves = new List<ICommand>();

        public BarrelPushCommand(MapPlane map, GameObject barrelObject, ObjDataInstance barrelInstance,
            List<Vector2Int> path, Quaternion finalRotation)
        {
            this.map = map;
            this.barrelObject = barrelObject;
            this.barrelInstance = barrelInstance;
            this.path = new List<Vector2Int>(path);
            this.finalRotation = finalRotation;
        }

        public void Execute()
        {
            executedMoves.Clear();
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int from = barrelInstance.mainGrid;
                Quaternion fromRotation = barrelInstance.rotation;
                Quaternion toRotation = i == path.Count - 1 ? finalRotation : fromRotation;
                MoveObjectCommand move = new MoveObjectCommand(barrelObject, barrelInstance, from, path[i],
                    barrelInstance.height, fromRotation, toRotation, map, false);
                move.Execute();
                executedMoves.Add(move);
            }

        }

        public void Undo()
        {
            for (int i = executedMoves.Count - 1; i >= 0; i--) executedMoves[i].Undo();
        }

        public void ClearCommand()
        {
            executedMoves.Clear();
        }
    }


    public class RollingMovementStrategy : IMovementStrategy
    {

        private List<Vector2Int> movementPath = new List<Vector2Int>();

        public GameObject gameObject;
        public ObjDataInstance objInstance;
        Vector2Int from;
        Quaternion currentRotation;

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
            if (selfData == null) return false;
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

                foreach (var grid in targetOccupiedGrids)
                {
                    bool canEnter = selfData.objType.HasFlag(ObjectType.Ghost)
                        ? map.mapData.IsGhostWalkable(new Vector2Int(grid.x, grid.y)) &&
                          map.mapData.CanMoveToGrid(grid, instanceID)
                        : map.mapData.CanMoveToGrid(grid, instanceID);
                    if (!canEnter)
                    {
                        canMoveToNext = false;
                        break;
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

            foreach (var grid in targetOccupiedGrids)
            {
                if (!map.mapData.CanMoveToGrid(grid, instanceID))
                {
                    return false;
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
