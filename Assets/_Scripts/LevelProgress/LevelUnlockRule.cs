using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LevelUnlockRule
{
    public string levelName;
    public string sceneName;
    public List<string> requiredLevels;
    public string nextScene;
}