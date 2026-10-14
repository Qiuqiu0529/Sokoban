using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonSceneChanger : MonoBehaviour
{
    [SerializeField]
    private string scenePath = "HubWorld";



    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void changeToHub()
    {
        Debug.Log("Changing scene to: " + scenePath);
        SceneManager.LoadScene(scenePath);
    }
}
