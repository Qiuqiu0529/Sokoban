using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "LevelUnlockConfig", menuName = "Scriptable Objects/LevelUnlockConfig")]
public class LevelUnlockConfig : ScriptableObject
{
    public List<LevelUnlockRule> unlockRules;

    public LevelUnlockRule FindLevel(string levelName)
    {
        var rule = unlockRules.Find(x => x.levelName == levelName);
    if (rule == null)
    {
        Debug.LogWarning($"LevelUnlockRule not found for levelName: {levelName}");
    }
        return rule;
    }
}
