using UnityEngine;

public class MainMenu : MonoBehaviour
{
    // Is this unused?

    //  public void StartGame()
    // {
    //     SceneManager.LoadScene("Game");
    //     Time.timeScale = 1f;
        
    // }
    // Start is called before the first frame update
    public void QuitGame() 
    {
        Application.Quit();
    }
    public void PauseMenu()
    {
        
         Time.timeScale = 0f;
    }
    public void ResumeGame()
    {
         Time.timeScale = 1f;
    }
    // public void Menu()
    // {
    //     SceneManager.LoadScene("Main");

    // }
}
