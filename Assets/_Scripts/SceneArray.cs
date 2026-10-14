using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneArray : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform buttonsPanel;

    //[HideInInspector]
    public List<string> levelSceneNames = new List<string>();

    void Start()
    {
        CreateButtons();
    }

    void CreateButtons()
    {
        foreach (string sceneName in levelSceneNames)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, buttonsPanel);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
                buttonText.text = sceneName;
            else
                Debug.LogWarning("No TextMeshProUGUI found in button prefab.");

            button.onClick.AddListener(() => LoadScene(sceneName));
        }
    }

    void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
