using MoreMountains.Tools;
using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.SceneManagement; // Necessary to manage scenes


public class SceneMGR : MonoBehaviour
{
    public static SceneMGR Instance { get; private set; }
    public delegate void LoadingScreen(bool screenOn);
    public static event LoadingScreen OnLoadingScreen;
    [SerializeField] private MMF_Player sceneTransitionPlayer;
    private bool waitingForSceneLoad;
    // let's change this later to scene paths or something else

    private void Awake()
    {

        Debug.Log("SceneMGR start");
        if (Instance != null && Instance != this)
        {
            Debug.Log("Destroying duplicate SceneMGR");
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        HubWorldRaycaster.OnSceneChange += ChangeScene;

        SceneManager.sceneLoaded += HandleSceneLoaded;

        DontDestroyOnLoad(this.gameObject);

    }

   

    private void OnDestroy()
    {
        // place holder!
        HubWorldRaycaster.OnSceneChange -= ChangeScene;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // Change the string to a reference later
    public void OnPlayButton(string sceneName)
    {
        Debug.Log("On play button");
        ChangeScene(sceneName);
    }

    public void OnExitButton()
    {
        Application.Quit();
    }

    public void ChangeScene(string sceneName)
    {
        Debug.Log("change scene!");
        PlaySceneTransition(sceneName);
    }

    public IEnumerator SceneChanging(string sceneName)
    {
        PlaySceneTransition(sceneName);
        yield break;
    }

    private void PlaySceneTransition(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Cannot change to an empty scene name.", this);
            return;
        }

        MMF_Player player = sceneTransitionPlayer != null ? sceneTransitionPlayer : FindSceneTransitionPlayer();
        if (player == null)
        {
            Debug.LogError("No MMF_Player with an MMF_LoadScene feedback was found in the active scene.", this);
            return;
        }

        MMF_LoadScene loadSceneFeedback = player.GetFeedbackOfType<MMF_LoadScene>();
        if (loadSceneFeedback == null)
        {
            Debug.LogError($"MMF_Player '{player.name}' does not contain an MMF_LoadScene feedback.", player);
            return;
        }

        if (waitingForSceneLoad) SceneManager.sceneLoaded -= HandleSceneLoaded;
        waitingForSceneLoad = true;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        OnLoadingScreen?.Invoke(true);

        loadSceneFeedback.DestinationSceneName = sceneName;
        player.PlayFeedbacks();
    }

    private MMF_Player FindSceneTransitionPlayer()
    {
        MMF_Player fallback = null;
        MMF_Player[] players = FindObjectsOfType<MMF_Player>();
        foreach (MMF_Player player in players)
        {
            if (player.GetFeedbackOfType<MMF_LoadScene>() == null) continue;
            if (player.gameObject.name.StartsWith("ChangeSceneFB")) return player;
            if (fallback == null) fallback = player;
        }
        return fallback;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!waitingForSceneLoad) return;
        waitingForSceneLoad = false;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        OnLoadingScreen?.Invoke(false);
    }

}
