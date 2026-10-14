using UnityEngine;
using Nova;
using NovaSamples.UIControls;
using System.Collections.Generic;
using System;
using UnityEngine.Rendering;
using MoreMountains.Feedbacks;
using Sirenix.Utilities;

public class LevelEditor : Singleton<LevelEditor>
{
    public LevelData levelData;
    private MapMgr mapMgr;

    public Slider length, width;
    public TextBlock rename;
    public LevelUI levelUI;
    public string levelName;

    public MMF_Player loadTestScene;

    void Start()
    {
        if (!ValidateReferences()) return;
        // InitializeEditor();
        if (string.IsNullOrEmpty(levelName))
        {
            levelName = LevelLoadBridge.LevelNameToLoad;
        }
        if (!string.IsNullOrEmpty(levelName))
        {
            LoadLevel(levelName);
        }
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

        // if (levelData.insideWorld.Count < 1 || levelData.outsideWorld.Count < 1)
        // {
        //     Debug.LogError("[LevelEditor] No maps assigned in LevelData!");
        //     return false;
        // }

        return true;
    }

    private void InitializeEditor()
    {
        mapMgr.Initialize(levelData, true);

        levelData.ResetObjectCounts();
        PlaceMgr.Instance.ClearAllCommands();
    }

    void OnDestroy()
    {
        // if (levelData != null)
        // {
        //     levelData.SaveData();
        //     SaveAllMaps();
        // }
    }

    private void SaveAllMaps()
    {
        foreach (var map in levelData.insideWorld)
        {
            map.SaveData();
        }
        foreach (var map in levelData.outsideWorld)
        {
            map.SaveData();
        }
    }

    public void SaveLevelToJson()
    {
        levelName = LevelDataIO.SaveLevelData(levelData,levelName,true);
    }
    public void SaveCurrentLevel()
    {
        levelName = LevelDataIO.SaveLevelData(levelData,levelName,true,true);
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
        CreateNewLevelData((int)length.Value, (int)width.Value);
        
        SaveLevelToJson();
        InitializeEditor();
    }

    public static MapData CreateNewMapData(int length, int width)
    {
        MapData newMap = ScriptableObject.CreateInstance<MapData>();
        newMap.gridLength = length;
        newMap.gridWidth = width;
        newMap.Initialize();
        return newMap;
    }

    public void CreateNewLevelData(int length, int width)
    {
        levelData.outsideWorld.Clear();
        levelData.insideWorld.Clear();

        MapData outside = CreateNewMapData(length, width);
        MapData inside = CreateNewMapData(length, width);

        levelData.outsideWorld.Add(outside);
        levelData.insideWorld.Add(inside);

        // levelData.playerStart.startMapID = outside.mapID;
        // levelData.playerStart.startGridPos = new Vector2Int(0, 0);
    }

    public void PlayThisLevel()
    {
        if (!levelName.IsNullOrWhitespace())
        {
            LevelLoadBridge.LevelNameToLoad = levelName;
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