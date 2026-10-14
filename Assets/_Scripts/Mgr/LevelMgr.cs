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
        LevelUnlockRule levelUnlockRule = unlockConfig.FindLevel(levelName);
        GameMgr.Instance.EnterBlockMove();

        if (levelUnlockRule.nextScene != "")
        {
            HubWorldManagerTest.Instance.exitScene = SceneManager.GetActiveScene().name;
            mMF_LoadScene.DestinationSceneName = levelUnlockRule.nextScene;
        }
        else
        {
            mMF_LoadScene.DestinationSceneName = "HubWorld";

        }

        loadScene.PlayFeedbacks();

    }
}
