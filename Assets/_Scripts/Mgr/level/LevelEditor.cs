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
    private bool showValidationResults;
    private Vector2 validationScrollPosition;
    private readonly List<string> validationErrors = new List<string>();
    private readonly List<string> validationWarnings = new List<string>();

    void Start()
    {
        LevelLoadBridge.IsEditorTestLevel = false;
        HideMapSizeControls();
        if (Camera.main != null && Camera.main.GetComponent<CameraMove>() == null)
            Camera.main.gameObject.AddComponent<CameraMove>();
        BindListToggleButtons();
        if (!ValidateReferences()) return;

        if (objectUI == null)
            objectUI = FindObjectOfType<ObjUI>(true);
        if (levelListObject == null && levelUI != null)
            levelListObject = levelUI.gameObject;
        if (objectListObject == null && objectUI != null)
            objectListObject = objectUI.gameObject;
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
        if (showValidationResults)
            DrawValidationResults();

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

    public void CheckLevel()
    {
        validationErrors.Clear();
        validationWarnings.Clear();
        validationScrollPosition = Vector2.zero;
        showValidationResults = true;

        MapData map = levelData != null ? levelData.mapData : null;
        if (map == null)
        {
            validationErrors.Add("关卡没有地图数据。请创建或加载一个关卡后再检查。");
            return;
        }
        if (map.tileDataList == null || map.tileDataList.tileDataList == null)
            validationErrors.Add("没有配置地块数据列表。");
        if (map.objDataList == null || map.objDataList.objDataList == null)
        {
            validationErrors.Add("没有配置物品数据列表，无法检查摆放物品。");
            return;
        }
        if (map.gridLength <= 0 || map.gridWidth <= 0 || map.GetTileIDs() == null ||
            map.GetTileIDs().Length != map.gridLength * map.gridWidth)
        {
            validationErrors.Add("地图网格尺寸与地块数据不匹配，无法安全检查。");
            return;
        }

        int tileCount = 0;
        for (int y = map.gridMin.y; y < map.gridMin.y + map.gridWidth; y++)
        for (int x = map.gridMin.x; x < map.gridMin.x + map.gridLength; x++)
            if (map.HasTileAt(new Vector2Int(x, y))) tileCount++;
        if (tileCount == 0)
            validationErrors.Add("地图上至少需要摆放一个地块。");

        List<ObjDataInstance> players = new List<ObjDataInstance>();
        Dictionary<int, int> ghostsByChannel = new Dictionary<int, int>();
        Dictionary<int, int> gravesByChannel = new Dictionary<int, int>();
        Dictionary<int, int> platesByChannel = new Dictionary<int, int>();
        Dictionary<int, int> controlledByChannel = new Dictionary<int, int>();
        int ghostCount = 0;
        int graveCount = 0;

        foreach (ObjDataInstance instance in map.GetAllObjectInstances())
        {
            ObjData data = map.GetObjectData(instance.objID);
            if (data == null)
            {
                validationErrors.Add($"找不到物品 ID {instance.objID} 对应的数据。");
                continue;
            }

            ObjectType type = data.objType;
            if (type.HasFlag(ObjectType.PureDecoration)) continue;
            if (type.HasFlag(ObjectType.Player)) players.Add(instance);
            if (type.HasFlag(ObjectType.Ghost))
            {
                ghostCount++;
                IncrementChannelCount(ghostsByChannel, instance.channelID);
            }
            if (type.HasFlag(ObjectType.Goal))
            {
                graveCount++;
                IncrementChannelCount(gravesByChannel, instance.channelID);
            }
            if (type.HasFlag(ObjectType.PressurePlate)) IncrementChannelCount(platesByChannel, instance.channelID);
            if (type.HasFlag(ObjectType.PressureControlled)) IncrementChannelCount(controlledByChannel, instance.channelID);

            List<Vector3Int> occupied = instance.currentOccupied;
            if (occupied == null || occupied.Count == 0) occupied = instance.CalculateOccupiedGrids(data);
            foreach (Vector3Int cell in occupied)
            {
                if (!map.HasTileAt(new Vector2Int(cell.x, cell.y)))
                {
                    validationErrors.Add($"“{data.objName}”占据的格子 ({cell.x}, {cell.y}) 没有地块。");
                    break;
                }
            }
        }

        if (players.Count == 0) validationErrors.Add("需要摆放一只小狗（Pomme）。");
        else if (players.Count > 1) validationErrors.Add($"小狗只能摆放一只，当前检测到 {players.Count} 只。");
        if (ghostCount == 0 || graveCount == 0)
            validationErrors.Add("至少需要一只鬼魂和一块墓碑，并通过相同频道配对。");

        int matchedPairs = 0;
        HashSet<int> channels = new HashSet<int>(ghostsByChannel.Keys);
        channels.UnionWith(gravesByChannel.Keys);
        foreach (int channel in channels)
        {
            ghostsByChannel.TryGetValue(channel, out int ghosts);
            gravesByChannel.TryGetValue(channel, out int graves);
            if (channel <= 0)
                validationErrors.Add("鬼魂和墓碑必须设置有效频道（频道 1 起）。");
            else if (ghosts > 1 || graves > 1)
                validationErrors.Add($"频道 {channel} 只能有一只鬼魂和一块对应墓碑（当前：鬼魂 {ghosts}，墓碑 {graves}）。");
            else if (ghosts == 1 && graves == 1)
                matchedPairs++;
            else
                validationErrors.Add($"频道 {channel} 的鬼魂与墓碑没有配对（当前：鬼魂 {ghosts}，墓碑 {graves}）。");
        }
        if (ghostCount > 0 && graveCount > 0 && matchedPairs == 0)
            validationErrors.Add("没有找到频道匹配的鬼魂与墓碑。");

        if (HasInvalidChannel(platesByChannel) || HasInvalidChannel(controlledByChannel))
            validationErrors.Add("压力板和南瓜挡路物必须设置有效频道（频道 1 起）。");
        foreach (int channel in platesByChannel.Keys)
            if (channel > 0 && !controlledByChannel.ContainsKey(channel))
                validationWarnings.Add($"频道 {channel} 有压力板，但没有对应的南瓜挡路物。");
        foreach (int channel in controlledByChannel.Keys)
            if (channel > 0 && !platesByChannel.ContainsKey(channel))
                validationErrors.Add($"频道 {channel} 的南瓜挡路物没有对应压力板。");

        if (players.Count == 1)
        {
            Vector2Int start = levelData.playerStart != null ? levelData.playerStart.startGridPos : players[0].mainGrid;
            if (levelData.playerStart == null)
                validationWarnings.Add("玩家起点数据缺失，将按狗狗摆放位置检查；请确认关卡加载时的起点设置。");
            else if (start != players[0].mainGrid)
                validationErrors.Add($"玩家起点 ({start.x}, {start.y}) 与狗狗摆放位置 ({players[0].mainGrid.x}, {players[0].mainGrid.y}) 不一致。");
            if (map.tileDataList != null && map.tileDataList.tileDataList != null &&
                !map.HasTileType(start, TileType.Walkable))
                validationErrors.Add($"玩家起点 ({start.x}, {start.y}) 没有可供小狗行走的地块。");
        }

        if (validationErrors.Count == 0 && validationWarnings.Count == 0)
            validationWarnings.Add("检查通过：地块、狗狗、鬼魂/墓碑配对及压力板频道均符合基础要求。");
    }

    private static void IncrementChannelCount(Dictionary<int, int> counts, int channel)
    {
        counts.TryGetValue(channel, out int current);
        counts[channel] = current + 1;
    }

    private static bool HasInvalidChannel(Dictionary<int, int> counts)
    {
        foreach (int channel in counts.Keys)
            if (channel <= 0) return true;
        return false;
    }

    private void DrawValidationResults()
    {
        const float width = 620f;
        const float height = 420f;
        Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.Box(rect, GUIContent.none);
        GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        GUI.Label(new Rect(rect.x + 18f, rect.y + 10f, width - 90f, 38f), "关卡检查", title);
        if (GUI.Button(new Rect(rect.xMax - 52f, rect.y + 10f, 36f, 32f), "×", new GUIStyle(GUI.skin.button) { fontSize = 22 }))
        {
            showValidationResults = false;
            return;
        }

        string heading = validationErrors.Count == 0 ? "没有发现阻止测试的问题。" : $"发现 {validationErrors.Count} 个需要处理的问题：";
        GUI.Label(new Rect(rect.x + 18f, rect.y + 52f, width - 36f, 30f), heading,
            new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold });
        Rect contentRect = new Rect(rect.x + 14f, rect.y + 86f, width - 28f, height - 148f);
        int itemCount = validationErrors.Count + validationWarnings.Count;
        validationScrollPosition = GUI.BeginScrollView(contentRect, validationScrollPosition,
            new Rect(0f, 0f, contentRect.width - 22f, Mathf.Max(contentRect.height, itemCount * 36f)));
        GUIStyle errorStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
        errorStyle.normal.textColor = new Color(0.75f, 0.12f, 0.1f);
        GUIStyle warningStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
        warningStyle.normal.textColor = new Color(0.82f, 0.55f, 0.08f);
        float y = 0f;
        foreach (string error in validationErrors)
        {
            GUI.Label(new Rect(4f, y, contentRect.width - 36f, 34f), "✖ " + error, errorStyle);
            y += 36f;
        }
        foreach (string warning in validationWarnings)
        {
            GUI.Label(new Rect(4f, y, contentRect.width - 36f, 34f), "! " + warning, warningStyle);
            y += 36f;
        }
        GUI.EndScrollView();
        if (GUI.Button(new Rect(rect.xMax - 126f, rect.yMax - 48f, 108f, 34f), "关闭"))
            showValidationResults = false;
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
        SaveCurrentLevel();
        if (!levelName.IsNullOrWhitespace())
        {
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
