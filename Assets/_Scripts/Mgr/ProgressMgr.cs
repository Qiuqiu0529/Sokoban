using UnityEngine;

public class ProgressMgr : Singleton<ProgressMgr>
{
    private const string ProgressKey = "PlayerProgress";
    public PlayerProgress Progress { get; private set; } = new PlayerProgress();
    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    private void Start()
    {
        LoadProgress();
    }

    public void MarkLevelCleared(string levelName)
    {
        if (!Progress.clearedLevels.Contains(levelName))
        {
            Progress.clearedLevels.Add(levelName);
            SaveProgress();
        }
    }

    public bool IsLevelCleared(string levelName)
    {
        return Progress.clearedLevels.Contains(levelName);
    }

    public void SaveProgress()
    {
        string json = JsonUtility.ToJson(Progress);
        PlayerPrefs.SetString(ProgressKey, json);
        PlayerPrefs.Save();
    }

    public void LoadProgress()
    {
        if (PlayerPrefs.HasKey(ProgressKey))
        {
            string json = PlayerPrefs.GetString(ProgressKey);
            Progress = JsonUtility.FromJson<PlayerProgress>(json);
        }
    }

    public void ResetProgress()
    {
        Progress = new PlayerProgress();
        SaveProgress();
    }
}
