using UnityEngine;
using NovaSamples.UIControls;
using System.Collections.Generic;
using System;
using UnityEngine.Rendering;
using MoreMountains.Feedbacks;
using Sirenix.Utilities;
using Nova;
using UnityEngine.Events;

public class LevelEditor : Singleton<LevelEditor>
{
    public LevelData levelData;
    private MapMgr mapMgr;

    public TextBlock rename;
    public LevelUI levelUI;
    [SerializeField] private ObjUI objectUI;
    [SerializeField] private GameObject levelListObject;
    [SerializeField] private GameObject objectListObject;
    public string levelName;

    public MMF_Player loadTestScene;
    private bool showOperationHints = true;
    private MyButton levelListButton;
    private MyButton objectListButton;

    void Start()
    {
        LevelLoadBridge.IsEditorTestLevel = false;
        HideMapSizeControls();
        if (Camera.main != null && Camera.main.GetComponent<CameraMove>() == null)
            Camera.main.gameObject.AddComponent<CameraMove>();
        if (!ValidateReferences()) return;

        if (objectUI == null)
            objectUI = FindObjectOfType<ObjUI>(true);
        if (levelListObject == null && levelUI != null)
            levelListObject = levelUI.gameObject;
        if (objectListObject == null && objectUI != null)
            objectListObject = objectUI.gameObject;
        BindListToggleButtons();

        string levelToResume = LevelLoadBridge.LevelNameToLoad;
        if (!string.IsNullOrEmpty(levelToResume))
        {
            LevelLoadBridge.LevelNameToLoad = null;
            LoadLevel(levelToResume);
            OpenObjectList();
            return;
        }

        if (!string.IsNullOrEmpty(levelName))
        {
            LoadLevel(levelName);
            OpenObjectList();
            return;
        }


        OpenLevelList();
    }

    private void BindListToggleButtons()
    {
        MyButton[] buttons = FindObjectsOfType<MyButton>(true);
        foreach (MyButton button in buttons)
        {
            TextBlock label = button.GetComponentInChildren<TextBlock>(true);
            if (label == null) continue;

            if (label.Text == "关卡列表")
            {
                levelListButton = button;
                EnsureButtonEvent(levelListButton).AddListener(OpenLevelList);
            }
            else if (label.Text == "物品栏")
            {
                objectListButton = button;
                EnsureButtonEvent(objectListButton).AddListener(OpenObjectList);
            }
        }
    }

    private static UnityEvent EnsureButtonEvent(MyButton button)
    {
        if (button.OnClicked == null) button.OnClicked = new UnityEvent();
        return button.OnClicked;
    }

    public void OpenLevelList()
    {
        ResolveListObjects();
        if (levelListObject != null) levelListObject.SetActive(true);
        if (objectListObject != null) objectListObject.SetActive(false);
        LoadJson();
    }

    public void OpenObjectList()
    {
        ResolveListObjects();
        if (levelListObject != null) levelListObject.SetActive(false);
        if (objectListObject != null) objectListObject.SetActive(true);
    }

    private void ResolveListObjects()
    {
        if (levelListObject == null && levelUI != null)
            levelListObject = levelUI.gameObject;
        if (objectUI == null)
            objectUI = FindObjectOfType<ObjUI>(true);
        if (objectListObject == null && objectUI != null)
            objectListObject = objectUI.gameObject;
    }

    private void OnDestroy()
    {
        if (levelListButton != null && levelListButton.OnClicked != null)
            levelListButton.OnClicked.RemoveListener(OpenLevelList);
        if (objectListButton != null && objectListButton.OnClicked != null)
            objectListButton.OnClicked.RemoveListener(OpenObjectList);
    }

    private void HideMapSizeControls()
    {
        // These are the two size sliders in the current level-editor scene.
        GameObject.Find("Slider_Dark")?.SetActive(false);
        GameObject.Find("Slider_Dark (1)")?.SetActive(false);
    }

    private bool ValidateReferences()
    {
        if (levelData == null)
        {
            Debug.LogError("[LevelEditor] LevelData is not assigned!");
            return false;
        }

        mapMgr = MapMgr.Instance;
        if (mapMgr == null)
        {
            Debug.LogError("[LevelEditor] MapMgr instance not found!");
            return false;
        }

        // if (levelData.mapData == null)
        // {
        //     Debug.LogError("[LevelEditor] No maps assigned in LevelData!");
        //     return false;
        // }

        return true;
    }

    private void InitializeEditor()
    {
        mapMgr.Initialize(levelData, true);

        CameraMove cameraMove = Camera.main != null ? Camera.main.GetComponent<CameraMove>() : null;
        if (cameraMove != null)
        {
            var maps = mapMgr.GetMapPlanes();
            if (maps != null && maps.Count > 0) cameraMove.FocusMap(maps[0]);
        }

        levelData.ResetObjectCounts();
        PlaceMgr.Instance.ClearAllCommands();
    }

    private void OnGUI()
    {
        const float panelWidth = 390f;
        const float panelHeight = 220f;
        float right = Screen.width - 16f;
        float bottom = Screen.height - 16f;

        if (!showOperationHints)
        {
            Rect reopenRect = new Rect(right - 150f, bottom - 48f, 150f, 48f);
            if (GUI.Button(reopenRect, "操作提示", new GUIStyle(GUI.skin.button) { fontSize = 20 }))
                showOperationHints = true;
            return;
        }

        Rect panelRect = new Rect(right - panelWidth, bottom - panelHeight, panelWidth, panelHeight);
        GUI.Box(panelRect, GUIContent.none);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(12, 0, 0, 0)
        };
        GUI.Label(new Rect(panelRect.x + 8f, panelRect.y + 6f, panelWidth - 66f, 38f), "编辑器操作提示", titleStyle);

        GUIStyle closeStyle = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
        if (GUI.Button(new Rect(panelRect.xMax - 46f, panelRect.y + 8f, 34f, 32f), "×", closeStyle))
        {
            showOperationHints = false;
            return;
        }

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            padding = new RectOffset(14, 8, 2, 2),
            wordWrap = true
        };
        GUI.Label(new Rect(panelRect.x + 6f, panelRect.y + 48f, panelWidth - 20f, panelHeight - 56f),
            "鼠标中键拖动：平移视野\n鼠标右键拖动：旋转相机\n滚轮：缩放地图\n鼠标左键 / 拖动：放置当前选中内容\n鼠标右键单击：移除内容，双击取消摆放模式\n小键盘 2：平面旋转物件 90°", hintStyle);
    }


    private void SaveAllMaps()
    {
        levelData.mapData?.SaveData();
    }

    public void SaveLevelToJson()
    {
        levelName = LevelDataIO.SaveLevelData(levelData,levelName,true);
        LoadJson();
    }
    public void SaveCurrentLevel()
    {
        if (string.IsNullOrEmpty(levelName))
        {
            SaveLevelToJson();
            return;
        }

        levelName = LevelDataIO.SaveLevelData(levelData,levelName,true,true);
        LoadJson();
    }

    public void LoadJson()
    {
        levelUI.SetList(LevelDataIO.GetAllSavedLevelFiles(true));
    }

    public void LoadLevel(string fileName)
    {
        levelName = fileName;
        LevelDataIO.LoadLevelDataByName(levelData, fileName,true);
        InitializeEditor();
    }

    public void CreateNewLevel()
    {
        levelName="";
        CreateNewLevelData();
        InitializeEditor();

    }

    public static MapData CreateNewMapData()
    {
        MapData newMap = ScriptableObject.CreateInstance<MapData>();
        // Start with a small editable area; the map grows as the user places content.
        newMap.gridLength = 10;
        newMap.gridWidth = 10;
        newMap.Initialize();
        return newMap;
    }

    public void CreateNewLevelData()
    {
        MapData map = CreateNewMapData();
        levelData.mapData = map;
        levelData.playerStart = new PlayerStartPosition
        {
            startGridPos = Vector2Int.zero
        };
    }

    public void PlayThisLevel()
    {
        if (!levelName.IsNullOrWhitespace())
        {
            SaveCurrentLevel();
            // Destroy the active placement preview before the additive transition begins.
            PlaceMgr placeMgr = FindObjectOfType<PlaceMgr>();
            if (placeMgr != null)
                placeMgr.ForceStopPlace();

            LevelLoadBridge.LevelNameToLoad = levelName;
            LevelLoadBridge.IsEditorTestLevel = true;
            loadTestScene.PlayFeedbacks();
        }
    }

    public void DeleteLevel()
    {
        LevelDataIO.DeleteLevelDataByName(levelName,true);
        LoadJson();

    }

    public void Rename()
    {
        string temp = rename.Text;
        if (temp != null)
        {
            rename.Text = "";
            LevelDataIO.RenameLevelData(levelName, temp,true);
            levelName = temp;
            Debug.Log(temp);
            LoadJson();
        }

    }


}
