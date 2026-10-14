using MoreMountains.Tools;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // Necessary to manage scenes
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;


public class SceneMGR : MonoBehaviour
{
    public static SceneMGR Instance { get; private set; }
    public delegate void LoadingScreen(bool screenOn);
    public static event LoadingScreen OnLoadingScreen;
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
        

        DontDestroyOnLoad(this.gameObject);

    }

   

    private void OnDestroy()
    {
        // place holder!
        HubWorldRaycaster.OnSceneChange -= ChangeScene;
        
    }

    // Change the string to a reference later
    public void OnPlayButton(string sceneName)
    {
        Debug.Log("On play button");
        StartCoroutine(SceneChanging(sceneName));
    }

    public void OnExitButton()
    {
        Application.Quit();
    }

    public void ChangeScene(string sceneName)
    {
        Debug.Log("change scene!");
        StartCoroutine(SceneChanging(sceneName));
    }

    public IEnumerator SceneChanging(string sceneName)
    {
        OnLoadingScreen?.Invoke(true);
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        Debug.Log("Changing scene to: " + sceneName);
        while (!operation.isDone)
        {
            yield return null;
        }
        OnLoadingScreen?.Invoke(false);
    }

}
