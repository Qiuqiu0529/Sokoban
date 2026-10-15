using UnityEngine;
using System.Collections.Generic;
using Commands;
using Movement;
using System.Collections;
using System;
using MoreMountains.Feedbacks;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using KToolkit;
using TMPro;

public class GameMgr : Singleton<GameMgr>
{
    public MapMgr mapMgr;
    public string levelName;
    public bool loadFromScriptsObj;
    public LevelData levelData;

    private GameplayState currentState = GameplayState.Playing;
    public GameObject currentPlayer;
    public MovableObj moveObjNow;
    [SerializeField]
    private TextMeshProUGUI levelText;

    private ObjDataInstance currentObjDataInstance;
    private MapPlane currentMap;
    private Vector2Int currentGridPos;


    public GameObject originalPlayer;
    private ObjDataInstance originalObjData;
    private MapPlane originalMap;


    private int currentHeight;

    private bool isPossessing = false;
    private GameObject possessedObject;
    private ObjDataInstance possessedObjData;
    private MapPlane possessedMap;

    private Command commandMgr;
    public IMovementStrategy movementStrategy;

    // Event for changing the camera
    public delegate void PosessEvent();
    public static event PosessEvent OnPosession;

    StepTracker accessibleTracker;
    StepTracker blockingTracker;

    public Vector2Int CurrentGridPos
    {
        get { return currentGridPos; }
        set { currentGridPos = value; }
    }

    private void Awake()
    {
        PauseMenu.OnPause += AddName;
    }

    private void OnDestroy()
    {
        PauseMenu.OnPause -= AddName;
    }

    private void AddName()
    {
        levelText.text = "Level " + levelName;
    }

    public void UpdatePlayerPosition(Vector2Int newPos)
    {
        currentGridPos = newPos;
    }

    public Vector3 InsidePos(Vector2Int pos)
    {
        return possessedMap.GridToWorldPosition(pos);
    }
    public Vector3 OutsidePos(Vector2Int pos)
    {
        return originalMap.GridToWorldPosition(pos);
    }

    #region  trail

    public bool IsHighlighted(Vector3Int grid)
    {
        return accessibleTracker.HasTrail(grid);
    }
    public bool IsBlockedByTrail(Vector3Int grid)
    {
        return blockingTracker.HasTrail(grid);
    }
    public bool IsInsideWorld(MapPlane map)
    {
        return map == possessedMap;
    }
    public enum TrailType { AccessibleTrail, BlockingTrail }
    public class StepTracker
    {
        private Dictionary<string, List<Vector3Int>> objectTrails = new();
        private Dictionary<string, int> objectHighLightCount = new();
        private Dictionary<string, Dictionary<Vector3Int, int>> localTrailCounts = new();
        private Dictionary<Vector3Int, int> globalTrailCounts = new();
        private Dictionary<string, int> maxStepList = new();

        private TrailType trailType;
        private MapPlane targetMap;
        private Action<Vector3Int, bool, MapPlane> setHighlight;
        private Dictionary<string, GameObject> indicate = new();

        public StepTracker(TrailType trailType, MapPlane tilemap, Action<Vector3Int, bool, MapPlane> setHighlightMethod)
        {
            this.trailType = trailType;
            this.targetMap = tilemap;
            this.setHighlight = setHighlightMethod;
        }
        public bool HasTrail(Vector3Int grid)
        {
            return globalTrailCounts.ContainsKey(grid) && globalTrailCounts[grid] > 0;
        }

        public void SetMaxStep(string instanceID, int stepMax)
        {
            maxStepList[instanceID] = stepMax;
        }


        public void AddStep(string instanceID, List<Vector3Int> newGrids, out List<Vector3Int> removedGrids)
        {
            removedGrids = new List<Vector3Int>();
            int maxStep = maxStepList[instanceID];

            if (!objectHighLightCount.ContainsKey(instanceID))
                objectHighLightCount[instanceID] = 0;
            if (!objectTrails.ContainsKey(instanceID))
                objectTrails[instanceID] = new List<Vector3Int>();
            if (!localTrailCounts.ContainsKey(instanceID))
                localTrailCounts[instanceID] = new Dictionary<Vector3Int, int>();

            var trailList = objectTrails[instanceID];
            var trailCountMap = localTrailCounts[instanceID];

            foreach (var grid in newGrids)
            {
                trailList.Add(grid);

                if (!trailCountMap.ContainsKey(grid) || trailCountMap[grid] == 0)
                {
                    objectHighLightCount[instanceID] = objectHighLightCount[instanceID] + 1;
                }
                trailCountMap[grid] = trailCountMap.GetValueOrDefault(grid, 0) + 1;

                if (!globalTrailCounts.ContainsKey(grid) || globalTrailCounts[grid] == 0)
                {
                    setHighlight(grid, true, targetMap);
                }
                globalTrailCounts[grid] = globalTrailCounts.GetValueOrDefault(grid, 0) + 1;

            }

            while (objectHighLightCount[instanceID] > maxStep)
            {
                Vector3Int oldGrid = trailList[0];
                removedGrids.Add(oldGrid);
                trailList.RemoveAt(0);
                if (globalTrailCounts.TryGetValue(oldGrid, out int globalCount))
                {
                    globalCount -= 1;
                    if (globalCount <= 0)
                    {
                        globalTrailCounts.Remove(oldGrid);
                        setHighlight(oldGrid, false, targetMap);
                    }
                    else
                    {
                        globalTrailCounts[oldGrid] = globalCount;
                    }
                }

                if (trailCountMap.TryGetValue(oldGrid, out int cnt))
                {
                    cnt -= 1;
                    if (cnt <= 0)
                    {
                        trailCountMap.Remove(oldGrid);
                        objectHighLightCount[instanceID] = objectHighLightCount[instanceID] - 1;
                    }
                    else
                    {
                        trailCountMap[oldGrid] = cnt;
                    }
                }

            }
        }

        public void RevertStep(string instanceID, List<Vector3Int> addedGrids, List<Vector3Int> removedGrids)
        {
            var trailList = objectTrails[instanceID];
            var trailCountMap = localTrailCounts[instanceID];

            foreach (var grid in addedGrids)
            {
                trailList.RemoveAt(trailList.Count - 1);
                if (!trailCountMap.TryGetValue(grid, out var cnt) || cnt <= 1)
                {
                    trailCountMap.Remove(grid);
                    objectHighLightCount[instanceID] = objectHighLightCount[instanceID] - 1;
                }
                else
                {
                    trailCountMap[grid] = cnt - 1;
                }

                if (!globalTrailCounts.TryGetValue(grid, out var globalCount) || globalTrailCounts[grid] <= 1)
                {
                    setHighlight(grid, false, targetMap);
                    globalTrailCounts.Remove(grid);
                }
                else
                {
                    globalTrailCounts[grid] = globalCount - 1;
                }

            }

            int i = removedGrids.Count - 1;
            for (; i >= 0; i--)
            {
                var grid = removedGrids[i];
                trailList.Insert(0, grid);

                if (!trailCountMap.ContainsKey(grid) || trailCountMap[grid] == 0)
                {
                    objectHighLightCount[instanceID] = objectHighLightCount[instanceID] + 1;
                }
                trailCountMap[grid] = trailCountMap.GetValueOrDefault(grid, 0) + 1;

                if (!globalTrailCounts.ContainsKey(grid) || globalTrailCounts[grid] == 0)
                {
                    setHighlight(grid, true, targetMap);
                }
                globalTrailCounts[grid] = globalTrailCounts.GetValueOrDefault(grid, 0) + 1;
            }
        }
    }

    public void RecordPlayerStep(string instanceID, List<Vector3Int> newGrids, out List<Vector3Int> removedOldStep)
    {
        accessibleTracker.AddStep(instanceID, newGrids, out removedOldStep);
    }


    public void RevertPlayerStep(string instanceID, List<Vector3Int> addedGrids, List<Vector3Int> removedOldStep)
    {
        accessibleTracker.RevertStep(instanceID, addedGrids, removedOldStep);
    }

    private void SetHighlight(Vector3Int grid, bool highlight, MapPlane map)
    {
        if (grid.z != 0)
        {
            return;
        }
        GameObject tileObj = map.GetTileAtGridPosition(new Vector2Int(grid.x, grid.y));
        if (tileObj != null)
        {
            var renderer = tileObj.transform.GetChild(0).GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = highlight;
            }
        }
    }



    public void SetAccseeibleMaxStep(string instanceID, int smaxstep)
    {
        accessibleTracker.SetMaxStep(instanceID, smaxstep);
    }

    public void SetBlockingMaxStep(string instanceID, int smaxstep)
    {
        blockingTracker.SetMaxStep(instanceID, smaxstep);
    }
    public void AddTrailStep(string instanceID,
    List<Vector3Int> newGrids, out List<Vector3Int> removedGrids)
    {
        blockingTracker.AddStep(instanceID, newGrids, out removedGrids);

    }

    public void RevertTrailStep(string instanceID, List<Vector3Int> newGrids, List<Vector3Int> removedGrids)
    {
        blockingTracker.RevertStep(instanceID, newGrids, removedGrids);

    }

    #endregion


    [SerializeField] bool playMoveAnim = false;
    [SerializeField] bool blockMove = false;
    float lastMoveTime = 0;
    public void EnterPlayMoveAnim()
    {
        playMoveAnim = true;

    }
    public void ResetPlayMoveAnim()
    {
        playMoveAnim = false;
    }

    public void EnterBlockMove()
    {
        blockMove = true;
    }
    public void ResetBlockMove()
    {
        blockMove = false;
    }


    public bool CanCheckEnd()
    {
        return !playMoveAnim;
    }

    #region  init
    void Start()
    {
        if (!ValidateReferences()) return;
        InitializeGame();
        
    }

    private bool ValidateReferences()
    {
        if (mapMgr == null)
        {
            Debug.LogError("[GameMgr] MapMgr instance not found!");
            return false;
        }

        if (levelData == null)
        {
            Debug.LogError("[GameMgr] LevelData is not assigned!");
            return false;
        }

        if (levelData.playerStart == null)
        {
            Debug.LogError("[GameMgr] PlayerStart is not set in LevelData!");
            return false;
        }

        return true;
    }
    public bool onlyOneWorld = true;

    private void InitializeGame()
    {

        InitPlayerInput();
        playMoveAnim = true;
        if (loadFromScriptsObj)
        {
            mapMgr.Initialize(levelData, false);
        }
        else
        {
            if (string.IsNullOrEmpty(levelName))
            {
                levelName = LevelLoadBridge.LevelNameToLoad;

            }
            if (!string.IsNullOrEmpty(levelName))
            {

                LevelDataIO.LoadLevelDataByName(levelData, levelName, true);
                mapMgr.Initialize(levelData, true);

            }
            else
            {
                Debug.LogWarning("No level name provided to GameMgr.");
            }
        }
        if (levelData.insideWorld[0].GetTileRotations().Count > 0 &&
        levelData.outsideWorld[0].GetTileRotations().Count > 0)
        {
            onlyOneWorld = false;
            MiniMapMgr.Instance.ActiveMiniMap();
        }
        else
        {
            MiniMapMgr.Instance.DeActiveMiniMap();
        }
        StartCoroutine(WaitForMapLoadThenInitPlayer());
        commandMgr = new Command();
        levelText.text = "Levell " + levelName;


    }

    private IEnumerator WaitForMapLoadThenInitPlayer()
    {

        while (!mapMgr.IsAllMapLoaded)
        {
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.2f);
        InitMap();
        InitializePlayer();
        accessibleTracker = new StepTracker(TrailType.AccessibleTrail, originalMap, SetHighlight);
        blockingTracker = new StepTracker(TrailType.BlockingTrail, possessedMap, SetHighlight);


        originalMap.LoadTrail();
        possessedMap.LoadTrail();

        InitialFB();
        playMoveAnim = false;

    }
    private void InitMap()
    {
        string targetMapID = levelData.playerStart.startMapID;
        if (string.IsNullOrEmpty(targetMapID))
        {
            Debug.LogError("[GameMgr] Player start map ID is null or empty!");
            return;
        }
        bool foundMap = false;
        var mapPlanes = mapMgr.GetMapPlanes();
        foreach (var map in mapPlanes)
        {
            if (map.mapData.mapID == targetMapID)
            {
                foundMap = true;
                currentMap = map;
                originalMap = map;
            }
            else
            {
                possessedMap = map;
            }
        }
        if (!foundMap)
        {
            Debug.LogError($"[GameMgr] Could not find map with ID: {targetMapID}");
        }


    }

    private void InitializePlayer()
    {

        Debug.Log($"[GameMgr] Looking for player in map: {levelData.playerStart.startMapID} at position: {levelData.playerStart.startGridPos}");
        var map = originalMap;

        currentGridPos = levelData.playerStart.startGridPos;
        currentPlayer = map.GetObjectAtGridPosition(currentGridPos);


        if (currentPlayer == null)
        {
            currentPlayer = map.GetObjectAtGridPosition(currentGridPos, 1);
            currentHeight = currentPlayer != null ? 1 : 0;
        }
        else
        {
            currentHeight = 0;
        }

        Vector3Int grid3D = new Vector3Int(currentGridPos.x, currentGridPos.y, currentHeight);

        originalObjData = map.mapData.GetObjDataInstanceAtGrid(grid3D);
        currentObjDataInstance = originalObjData;
        // List<Vector3Int> vector3s = new List<Vector3Int>();
        // RecordPlayerStep(currentObjDataInstance.currentOccupied, out vector3s);
        if (currentPlayer != null)
        {
            moveObjNow = currentPlayer.GetComponent<MovableObj>();
            Debug.Log($"[GameMgr] Found player at height: {currentHeight}");
            isPossessing = false;
            originalPlayer = currentPlayer;
            movementStrategy = new SimpleMovementStrategy();
            return;
        }
        else
        {
            Debug.LogError($"[GameMgr] Player object not found at position {currentGridPos} ");
        }

    }
    #endregion
    public void ChangeMovementStrategy(IMovementStrategy movementStrategy)
    {
        this.movementStrategy = movementStrategy;
    }

    public Image resetHoldImage;
    public float resetHoldTime = 1f;
    private bool isResetHolding = false;
    private float holdTimer = 0f;




    void Update()
    {
        if (currentState != GameplayState.Playing) return;
        if (blockMove) return;


        inputTimer += Time.deltaTime;
        CacheMovementInput();
        CacheUndoInput();
        CachePossessionInput();
        CacheBarkInput();


        if (inputTimer >= inputCooldown && !playMoveAnim)
        {
            ExecuteBufferedInputs();
        }


        if (resetAction.IsPressed())
        {
            if (!isResetHolding)
            {
                isResetHolding = true;
                holdTimer = 0f;
                resetHoldImage.fillAmount = 0f;
                resetHoldImage.gameObject.SetActive(true); // show UI
            }

            holdTimer += Time.deltaTime;
            resetHoldImage.fillAmount = holdTimer / resetHoldTime;

            if (holdTimer >= resetHoldTime)
            {
                blockMove = true;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
        else
        {
            if (isResetHolding)
            {
                isResetHolding = false;
                holdTimer = 0f;
                resetHoldImage.fillAmount = 0f;
                resetHoldImage.gameObject.SetActive(false); // hide UI
            }
        }




    }

    CompositeCommand MakeBarkCommand()
    {
        CompositeCommand compositeCommand = new CompositeCommand();
        BarkCommand barkCommand = new BarkCommand(currentMap.mapData, currentMap, currentGridPos);
        if (barkCommand.canBark)
        {
            barkCommand.Execute();
            compositeCommand.AddCommand(barkCommand);
        }
        return compositeCommand;
    }

    #region  input

    [SerializeField] private InputActionAsset inputActionsAsset;
    private InputAction moveAction;
    private InputAction barkAction;
    private InputAction possessAction;
    private InputAction undoAction;
    private InputAction resetAction;
    [SerializeField] private float inputCooldown = 0.2f;
    [SerializeField] private float inputBufferTime = 0.3f;

    private float inputTimer = 0f;

    private Vector2 navInput => moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
    private bool barkPressed => barkAction?.WasPressedThisFrame() ?? false;
    private bool possessPressed => possessAction?.WasPressedThisFrame() ?? false;
    private bool undoPressed => undoAction?.IsPressed() ?? false;
    //private bool resetPressed => resetAction?.triggered ?? false;


    private bool bufferedMove = false;
    float lastMoveInputTime=0,lastBarkInputTime=0,lastPossessInputTime=0,lastUndoInputTime=0;
    private Vector2Int bufferedMoveDir = Vector2Int.zero;

    private bool bufferedBark = false;
    private bool bufferedPossess = false;
    private bool bufferedUndo = false;
    public void InitPlayerInput()
    {
        var playerActionMap = inputActionsAsset.FindActionMap("Player");

        moveAction = playerActionMap.FindAction("Move");
        barkAction = playerActionMap.FindAction("Bark");
        possessAction = playerActionMap.FindAction("Possess");
        undoAction = playerActionMap.FindAction("Undo");
        resetAction = playerActionMap.FindAction("Reset");

        moveAction.Enable();
        barkAction.Enable();
        possessAction.Enable();
        undoAction.Enable();
        resetAction.Enable();
    }

    void CacheMovementInput()
    {
        if (currentPlayer == null) return;

        Vector2 input = navInput;

        if (input.magnitude > 0.1f)
        {
            Vector2Int moveDir = Mathf.Abs(input.x) > Mathf.Abs(input.y)
                ? (input.x > 0 ? Vector2Int.right : Vector2Int.left)
                : (input.y > 0 ? Vector2Int.up : Vector2Int.down);

            if (Time.time - lastMoveInputTime > 0.15f)
            { 
                bufferedMove = true;
                bufferedMoveDir = moveDir;
                lastMoveInputTime = Time.time;
            }
            
        }
    }

    void CacheBarkInput()
    {
        if (barkPressed)
        {
            bufferedBark = true;
            lastBarkInputTime = Time.time;
        }
    }

    void CachePossessionInput()
    {
        if (possessPressed)
        {
            bufferedPossess = true;
            lastPossessInputTime = Time.time;
        }
    }

    void CacheUndoInput()
    {
        if (undoPressed)
        {
            if (Time.time - lastUndoInputTime > 0.1f)
            { 
                bufferedUndo = true;
                lastUndoInputTime = Time.time;
            }
            
        }
    }

    void ExecuteBufferedInputs()
    {
        if (bufferedMove && Time.time - lastMoveInputTime <= inputBufferTime)
        {
            ExecuteMove(bufferedMoveDir);
            bufferedMove = false;
        }
        else if (bufferedBark && Time.time - lastBarkInputTime <= inputBufferTime)
        {
            inputTimer = 0f;
            moveObjNow.PlayBarkFB();
            StartCoroutine(WaitBarkAnim());
            bufferedBark = false;
        }
        else if (bufferedPossess && Time.time - lastPossessInputTime <= inputBufferTime)
        {
            ExecutePossess();
            bufferedPossess = false;
        }
        else if (bufferedUndo && Time.time - lastUndoInputTime <= inputBufferTime)
        {
            inputTimer = 0f;
            commandMgr.Undo();
            bufferedUndo = false;
        }

        if (Time.time - lastMoveInputTime > inputBufferTime) bufferedMove = false;
        if (Time.time - lastBarkInputTime > inputBufferTime) bufferedBark = false;
        if (Time.time - lastPossessInputTime > inputBufferTime) bufferedPossess = false;
        if (Time.time - lastUndoInputTime > inputBufferTime) bufferedUndo = false;
    }

    IEnumerator WaitBarkAnim()
    {
        yield return new WaitForSeconds(0.2f);
        CompositeCommand compositeCommand = MakeBarkCommand();
        if (compositeCommand.CommandSize() > 0)
        {
            commandMgr.AddCommand(compositeCommand);
        }
        else
        {
            PlaySpiritCantMoveFB();
        }
    }


    void ExecuteMove(Vector2Int moveDir)
    {
        inputTimer = 0f;

        if (!moveObjNow.CanMove(moveDir))
        {
            PlayCantMoveFB();
            return;
        }

        Vector2Int targetPos = movementStrategy.CalculateTargetPosition(currentGridPos, moveDir, currentHeight, currentMap, true);
        if (targetPos != currentGridPos)
        {
            if (cantMoveFB.IsPlaying)
                cantMoveFB.StopFeedbacks();

            ICommand moveCommand = movementStrategy.CreateMoveCommand(moveDir, currentMap, true);
            moveCommand.Execute();

            CompositeCommand compositeCommand = new CompositeCommand();
            compositeCommand.AddCommand(moveCommand);

            ICommand commandtemp = moveObjNow.AnyCommand(moveDir);
            commandtemp.Execute();
            compositeCommand.AddCommand(commandtemp);

            commandMgr.AddCommand(compositeCommand);
        }
        else
        {
            PlayCantMoveFB();
        }
    }
    


    void ExecutePossess()
    {
        inputTimer = 0f;

        if (!isPossessing)
        {
            if (CanEnterPossess())
            {
                inputTimer = -0.7f;
                ICommand possessCommand = new PossessCommand(originalPlayer, possessedObject, originalMap, possessedMap, originalObjData, possessedObjData);
                commandMgr.ExecuteCommand(possessCommand);
                KEventManager.SendNotification(EventName.PlayerPossess, possessCommand);
            }
            else
            {
                PlayCantMoveFB();
            }
        }
        else
        {
            if (CanExitPossess())
            {
                exitPossessFB?.PlayFeedbacks();
                ICommand exitCommand = new ExitPossessCommand(originalPlayer, currentPlayer, originalMap, currentMap, originalObjData, possessedObjData);
                commandMgr.ExecuteCommand(exitCommand);
            }
            else
            {
                PlayCantMoveFB();
            }
        }
    }
   
    #endregion


    public bool CanExitPossess()
    {
        Vector3Int originalGrid3D = new Vector3Int(currentGridPos.x, currentGridPos.y, originalObjData.height);
        return originalMap.mapData.CanMoveToGrid(originalGrid3D, originalObjData.instanceID);
    }

    public bool CanEnterPossess()
    {
        // Possession is retired by the current Sokoban rules.
        return false;
    }

    public void SetMapAndCurrentObj(MapPlane map, GameObject gameObject, Vector2Int gridPos, ObjDataInstance objDataInstance, float waitTime = 0.1f)
    {
        if (map != currentMap)
        {
            StartCoroutine(WaitPossessAnim(waitTime));
        }
        currentMap = map;
        Vector3 worldPos = currentMap.GridToWorldPosition(gridPos);
        currentPlayer = gameObject;
        currentPlayer.transform.position = worldPos;
        currentGridPos = gridPos;
        currentObjDataInstance = objDataInstance;
        isPossessing = !isPossessing;
        moveObjNow.ExitPossess();
        moveObjNow = currentPlayer.GetComponent<MovableObj>();
        moveObjNow.EnterPossess();
    }

    IEnumerator WaitPossessAnim(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        OnPosession?.Invoke();
    }



    #region FB
    public MMF_Player enablePlayerFB, disablePlayerFB, cantMoveFB;
    public MMF_Player possessFB, exitPossessFB;
    public MMF_Player spiritCantMoveFB;
    public MMF_SquashAndStretch mMF_SquashAndStretchOnEnable, mMF_SquashAndStretchOnDisable;
    public MMF_SetActive mMF_SetActiveOnEnable, mMF_SetActiveOnDisable;
    public MMPositionShaker mMF_PositionShake;

    public void InitialFB()
    {
        if (enablePlayerFB)
        {
            mMF_SquashAndStretchOnEnable = enablePlayerFB.GetFeedbackOfType<MMF_SquashAndStretch>();
            mMF_SquashAndStretchOnEnable.SquashAndStretchTarget = currentPlayer.transform;
            mMF_SetActiveOnEnable = enablePlayerFB.GetFeedbackOfType<MMF_SetActive>();
            mMF_SetActiveOnEnable.TargetGameObject = currentPlayer;
        }
        if (disablePlayerFB)
        {
            mMF_SquashAndStretchOnDisable = disablePlayerFB.GetFeedbackOfType<MMF_SquashAndStretch>();
            mMF_SquashAndStretchOnDisable.SquashAndStretchTarget = currentPlayer.transform;
            mMF_SetActiveOnDisable = disablePlayerFB.GetFeedbackOfType<MMF_SetActive>();
            mMF_SetActiveOnDisable.TargetGameObject = currentPlayer;
        }
    }

    public void EnAbleOriPlayer()
    {
        enablePlayerFB?.PlayFeedbacks();
    }

    public void DisAbleOriPlayer(bool usepause = true)
    {
        moveObjNow.PlayPossessFB();
        if (usepause)
        {
            disablePlayerFB.GetFeedbackOfType<MMF_HoldingPause>().Active = true;
        }
        else
        {
            disablePlayerFB.GetFeedbackOfType<MMF_HoldingPause>().Active = false;
        }
        disablePlayerFB?.PlayFeedbacks();
    }

    public void PlayCantMoveFB()
    {
        mMF_PositionShake.TargetTransform = moveObjNow.model.transform;
        moveObjNow.CantMoveFB();
        cantMoveFB.PlayFeedbacks();
    }

    public void PlaySpiritCantMoveFB()
    {
        spiritCantMoveFB?.PlayFeedbacks();
    }


    #endregion



}
