using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuUIScript : MonoBehaviour
{
    public GameObject PauseMenu;
    private bool gameIsPaused;

    void Start()
    {
        //=========================================================
            //Cursor Locking
        //=========================================================
        SetCursorLocked(false); //Start unlocked so that it's easier to test in the editor
    }
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
        if (!gameIsPaused)
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    SetCursorLocked(true);
                }
            }
        }
    }

    public void PauseGame()
    {
        gameIsPaused = true;
        PauseMenu.SetActive(true);
        Time.timeScale = 0f;
        //===========================================================
            //Cursor Locking
        //===========================================================
        if (Cursor.lockState != CursorLockMode.None)
        {
            SetCursorLocked(false);
        }
    }
    
    public void ResumeGameplay()
    {
        gameIsPaused = false;
        PauseMenu.SetActive(false);
        Time.timeScale = 1.0f;
        //===========================================================
            //Cursor Locking
        //===========================================================
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            SetCursorLocked(true);
        }
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

    public static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
