using UnityEngine;

public class LoadingScreenMGR : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        SceneMGR.OnLoadingScreen += EnableScreen;
    }

    private void EnableScreen(bool isOn)
    {
        // This is reliant on that the loading screen has the loadingGO as the first GO
        transform.GetChild(0).gameObject.SetActive(isOn);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
