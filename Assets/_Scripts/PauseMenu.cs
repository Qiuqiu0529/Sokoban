using UnityEngine;
using UnityEngine.UI;
using TMPro;



public class PauseMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject pauseMenu;
    [SerializeField]
    private GameObject optionsMenu;

    public delegate void PauseEvent();
    public static event PauseEvent OnPause;

    private void Start()
    {
        OnPause?.Invoke();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void OpenOptionsMenu()
    {
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(true);
        Debug.Log("Opened Options menu.");

    }

    public void ToggleMenu()
    {
        if (optionsMenu != null && optionsMenu.activeSelf)
        {
            Debug.Log("closing options menu, opening pause menu");
            optionsMenu.SetActive(false);
            Debug.Log("closing options");

            if (pauseMenu != null)
            {
                pauseMenu.SetActive(true);
                Debug.Log("opening pause");

            }

            return;
        }

        if (pauseMenu != null)
        {
            bool isPauseActive = pauseMenu.activeSelf;
            Debug.Log("Toggling Pause menu: " + !isPauseActive);
            pauseMenu.SetActive(!isPauseActive);

        }
    }

}
