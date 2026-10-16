using UnityEngine;
using MoreMountains.Feedbacks;
using UnityEngine.SceneManagement;
public class LevelMgr : Singleton<LevelMgr>
{
    public LevelUnlockConfig unlockConfig;
    public MMF_Player loadScene;
    public MMF_LoadScene mMF_LoadScene;
    string sceneName;
    void Start()
    {
        mMF_LoadScene = loadScene.GetFeedbackOfType<MMF_LoadScene>();
    }

    public bool IsLevelUnlocked(string levelName)
    {
        var rule = unlockConfig.unlockRules.Find(r => r.levelName == levelName);
        if (rule == null) return true;
        sceneName = rule.sceneName;

        foreach (var required in rule.requiredLevels)
        {
            if (!ProgressMgr.Instance.IsLevelCleared(required))
                return false;
        }
        return true;
    }
    public void LoadLevelScene(string levelName)
    {
        LevelLoadBridge.IsEditorTestLevel = false;
        mMF_LoadScene.DestinationSceneName = levelName;
        loadScene.PlayFeedbacks();
    }

    public void TryLoadLevel(string levelName)
    {
        if (IsLevelUnlocked(levelName))
        {
            LoadLevelScene(levelName);
        }

    }

    public void ReturnHubworld()
    {
        mMF_LoadScene.DestinationSceneName = "HubWorld";

        loadScene.PlayFeedbacks();
    }

    public void AfterLevelClear(string levelName)
    {
        Debug.Log("AfterLevelClear" + levelName);
        LevelUnlockRule levelUnlockRule = unlockConfig != null ? unlockConfig.FindLevel(levelName) : null;
        GameMgr.Instance.EnterBlockMove();

        // Test/custom levels may not have an unlock rule. Treat them as the
        // end of a level chain and return to the hub instead of dereferencing
        // a missing rule.
        string nextScene = levelUnlockRule != null ? levelUnlockRule.nextScene : null;
        if (!string.IsNullOrEmpty(nextScene))
        {
            HubWorldManagerTest.Instance.exitScene = SceneManager.GetActiveScene().name;
            mMF_LoadScene.DestinationSceneName = nextScene;
        }
        else
        {
            mMF_LoadScene.DestinationSceneName = "HubWorld";

        }

        loadScene.PlayFeedbacks();

    }
}
