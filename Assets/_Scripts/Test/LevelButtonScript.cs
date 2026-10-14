using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;

public class LevelButtonScript : MonoBehaviour
{
    public string sceneName;
    public GhostScript player;
    public MMF_Player mmf_Player;
    public TMP_Text tMP_Text;

    public Color defaultColour = new Color(41f, 59f, 65f);
    public Color litColour = new Color(0f, 0f, 0f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (player == null)
            player = FindAnyObjectByType<GhostScript>();

        tMP_Text.text = sceneName.Substring(6);
    }

    public void LoadScene()
    {
        mmf_Player.GetFeedbackOfType<MMF_LoadScene>().DestinationSceneName = sceneName;

        mmf_Player.PlayFeedbacks();
    }

    private void OnTriggerEnter(Collider other)
    {
        tMP_Text.color = litColour;
        Debug.Log("Trigger entered for button " + sceneName);

    }

    private void OnTriggerExit(Collider other)
    {
        tMP_Text.color = defaultColour;
        Debug.Log("Trigger exited for button " + sceneName);

    }
}
