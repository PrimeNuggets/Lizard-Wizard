using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuUIScript : MonoBehaviour
{
    public GameObject PauseMenu;
    private bool gameIsPaused;

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (gameIsPaused)
            {
                ResumeGameplay();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        gameIsPaused = true;
        PauseMenu.SetActive(true);
        Time.timeScale = 0f;
    }
    
    public void ResumeGameplay()
    {
        gameIsPaused = false;
        PauseMenu.SetActive(false);
        Time.timeScale = 1.0f;
    }

    public void lizardFunny()
    {
        Debug.Log("Lizard...");
    }

    public void saveGame()
    {
        Debug.Log("Your game will save... eventually.");
    }

    public void quitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
