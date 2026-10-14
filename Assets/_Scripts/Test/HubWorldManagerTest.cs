using System;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

public class HubWorldManagerTest : Singleton<HubWorldManagerTest>
{
    public string exitScene;
    public List<string> sceneNames = new List<string>();
    public List<LevelButtonScript> levelButtonScripts=new();
    public GhostScript player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadButtons();
    }

    public void ReloadPlayerPos()
    {
        player = FindFirstObjectByType<GhostScript>();
        
        LoadButtons();
        
        if(sceneNames.Contains(exitScene))
        {
            int index = sceneNames.IndexOf(exitScene);
            float reloadX = levelButtonScripts[index].transform.position.x;
            float reloadZ = levelButtonScripts[index].transform.position.z;            
            player.transform.position = new Vector3(reloadX, player.transform.position.y,reloadZ);
        }
    }

    public void LoadButtons()
    {
        sceneNames.Clear();
        levelButtonScripts.Clear();
        
        LevelButtonScript[] scripts = GameObject.FindObjectsByType<LevelButtonScript>(FindObjectsSortMode.None);

        foreach (var script in scripts)
        {
            sceneNames.Add(script.sceneName);
            levelButtonScripts.Add(script);
        }
    }
}
