using UnityEngine;
using Commands;
using Movement;
using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;

public class GameMgr : Singleton<GameMgr>
{
    public MapMgr mapMgr;
    public string levelName;
    public bool loadFromScriptsObj;
    public LevelData levelData;
    [SerializeField] private bool focusCameraOnMapLoad = true;
    [SerializeField] private bool enableCameraControls = true;
    public bool IsEditorTestLevel { get; private set; }

    private GameplayState currentState = GameplayState.Playing;
    public GameObject currentPlayer;
    public MovableObj moveObjNow;
    [SerializeField]
    private TextMeshProUGUI levelText;

    private MapPlane currentMap;
    private Vector2Int currentGridPos;


    private int currentHeight;

    public static bool IsUsingGamepad { get; private set; }
    public static event Action<bool> OnInputDeviceChanged;

    private Command commandMgr;
    public IMovementStrategy movementStrategy;


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
        StopTrackingInputDevice();
    }

    private void AddName()
    {
        levelText.text = "Level " + levelName;
    }

    public void UpdatePlayerPosition(Vector2Int newPos)
    {
        currentGridPos = newPos;
    }

    public Vector3 MapPos(Vector2Int pos)
    {
        return currentMap != null ? currentMap.GridToWorldPosition(pos) : Vector3.zero;
    }
    [SerializeField] bool playMoveAnim = false;
    [SerializeField] bool blockMove = false;
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
        return !playMoveAnim && !blockMove;
    }

    #region  init
    void Start()
    {
        if (!ValidateReferences()) return;
        if (enableCameraControls)
        {
            Camera gameplayCamera = GetGameplayCamera();
            if (gameplayCamera != null && gameplayCamera.GetComponent<CameraMove>() == null)
                gameplayCamera.gameObject.AddComponent<CameraMove>();
        }
        InitializeGame();
        
    }

    private Camera GetGameplayCamera()
    {
        // Gameplay scenes use SpiritCamera as the full-screen view while
        // RealCamera renders to the minimap. Directly opening a level scene
        // has no Camera tagged MainCamera, so Camera.main alone is not enough.
        GameObject spiritCameraObject = GameObject.FindGameObjectWithTag("SpiritCamera");
        Camera spiritCamera = spiritCameraObject != null
            ? spiritCameraObject.GetComponent<Camera>()
            : null;
        if (spiritCamera != null && spiritCamera.isActiveAndEnabled)
            return spiritCamera;

        if (Camera.main != null)
            return Camera.main;

        GameObject realCameraObject = GameObject.FindGameObjectWithTag("RealCamera");
        return realCameraObject != null ? realCameraObject.GetComponent<Camera>() : null;
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
    private void InitializeGame()
    {
        IsEditorTestLevel = LevelLoadBridge.IsEditorTestLevel;
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
                mapMgr.Initialize(levelData, false);

            }
            else
            {
                Debug.LogWarning("No level name provided to GameMgr.");
            }
        }
        FocusCameraBeforeMapFinishesLoading();
        if (MiniMapMgr.Instance != null)
            MiniMapMgr.Instance.DeActiveMiniMap();
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

        // Let spawned objects run Start before the player is looked up.
        yield return null;
        InitMap();
        InitializePlayer();

        InitialFB();
        playMoveAnim = false;

    }

    private void FocusCameraBeforeMapFinishesLoading()
    {
        if (!focusCameraOnMapLoad) return;

        Camera gameplayCamera = GetGameplayCamera();
        CameraMove cameraMove = gameplayCamera != null
            ? gameplayCamera.GetComponent<CameraMove>()
            : null;
        var mapPlanes = mapMgr != null ? mapMgr.GetMapPlanes() : null;
        if (cameraMove != null && mapPlanes != null && mapPlanes.Count > 0)
            cameraMove.FocusMap(mapPlanes[0]);
        else if (gameplayCamera == null)
            Debug.LogError("[GameMgr] No gameplay camera found. Expected SpiritCamera, MainCamera, or RealCamera.");
    }

    private void InitMap()
    {
        var mapPlanes = mapMgr.GetMapPlanes();
        currentMap = mapPlanes != null && mapPlanes.Count > 0 ? mapPlanes[0] : null;
        if (currentMap == null)
            Debug.LogError("[GameMgr] The level did not load its map plane.");

    }

    private void InitializePlayer()
    {

        if (currentMap == null) return;
        Debug.Log($"[GameMgr] Looking for player at position: {levelData.playerStart.startGridPos}");
        var map = currentMap;

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

        if (currentPlayer != null)
        {
            moveObjNow = currentPlayer.GetComponent<MovableObj>();
            Debug.Log($"[GameMgr] Found player at height: {currentHeight}");
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
            currentMap.RefreshPressureMechanisms();
            compositeCommand.AddCommand(barkCommand);
        }
        return compositeCommand;
    }

    #region  input

    [SerializeField] private InputActionAsset inputActionsAsset;
    private InputAction moveAction;
    private InputAction barkAction;
    private InputAction undoAction;
    private InputAction resetAction;
    [SerializeField] private float inputCooldown = 0.2f;
    [SerializeField] private float inputBufferTime = 0.3f;

    private float inputTimer = 0f;

    private Vector2 navInput => moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
    private bool barkPressed => barkAction?.WasPressedThisFrame() ?? false;
    private bool undoPressed => undoAction?.IsPressed() ?? false;
    //private bool resetPressed => resetAction?.triggered ?? false;


    private bool bufferedMove = false;
    float lastMoveInputTime=0,lastBarkInputTime=0,lastUndoInputTime=0;
    private Vector2Int bufferedMoveDir = Vector2Int.zero;

    private bool bufferedBark = false;
    private bool bufferedUndo = false;
    public void InitPlayerInput()
    {
        StopTrackingInputDevice();
        SetInputDevice(Gamepad.current != null && Keyboard.current == null);
        var playerActionMap = inputActionsAsset.FindActionMap("Player");

        moveAction = playerActionMap.FindAction("Move");
        barkAction = playerActionMap.FindAction("Bark");
        undoAction = playerActionMap.FindAction("Undo");
        resetAction = playerActionMap.FindAction("Reset");

        moveAction.Enable();
        barkAction.Enable();
        undoAction.Enable();
        resetAction.Enable();

        TrackInputAction(moveAction);
        TrackInputAction(barkAction);
        TrackInputAction(undoAction);
        TrackInputAction(resetAction);
    }

    private void TrackInputAction(InputAction action)
    {
        if (action == null) return;
        action.started += OnPlayerInputStarted;
    }

    private void StopTrackingInputDevice()
    {
        if (moveAction != null) moveAction.started -= OnPlayerInputStarted;
        if (barkAction != null) barkAction.started -= OnPlayerInputStarted;
        if (undoAction != null) undoAction.started -= OnPlayerInputStarted;
        if (resetAction != null) resetAction.started -= OnPlayerInputStarted;
    }

    private void OnPlayerInputStarted(InputAction.CallbackContext context)
    {
        bool isGamepad;
        if (context.control.device is Gamepad)
            isGamepad = true;
        else if (context.control.device is Keyboard)
            isGamepad = false;
        else
            return;

        SetInputDevice(isGamepad);
    }

    private static void SetInputDevice(bool isGamepad)
    {
        if (IsUsingGamepad == isGamepad) return;
        IsUsingGamepad = isGamepad;
        OnInputDeviceChanged?.Invoke(IsUsingGamepad);
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
        else if (bufferedUndo && Time.time - lastUndoInputTime <= inputBufferTime)
        {
            inputTimer = 0f;
            commandMgr.Undo();
            if (currentMap != null) currentMap.RefreshPressureMechanisms();
            bufferedUndo = false;
        }

        if (Time.time - lastMoveInputTime > inputBufferTime) bufferedMove = false;
        if (Time.time - lastBarkInputTime > inputBufferTime) bufferedBark = false;
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
            currentMap.RefreshPressureMechanisms();
            compositeCommand.AddCommand(commandtemp);

            commandMgr.AddCommand(compositeCommand);
        }
        else
        {
            PlayCantMoveFB();
        }
    }
    


    #endregion

    #region FB
    public MMF_Player enablePlayerFB, disablePlayerFB, cantMoveFB;
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
