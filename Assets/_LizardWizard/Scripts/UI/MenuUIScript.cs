using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.EventSystems;

public class MenuUIScript : MonoBehaviour
{
    [SerializeField] private GameObject PauseMenu;
    [SerializeField] private Image PauseOverlay;
    private bool gameIsPaused;
    private InputAction pauseAction; //-- Esc or Start, from the player's input actions

    void Start()
    {
        //=========================================================
            //Cursor Locking
        //=========================================================
        SetCursorLocked(false); //Start unlocked so that it's easier to test in the editor

        var input = FindAnyObjectByType<PlayerInput>();
        if (input != null) pauseAction = input.actions.FindAction("Player/Pause");
    }
    void Update()
    {
        if (PausePressed())
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
                if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || GamepadUsed())
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

        //-- no mouse on a controller so highlight the first button to navigate from
        if (Gamepad.current != null && EventSystem.current != null)
        {
            var firstButton = PauseMenu.GetComponentInChildren<Selectable>();
            if (firstButton != null) EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
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

    private bool PausePressed()
    {
        if (pauseAction != null) return pauseAction.WasPressedThisFrame();
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    //-- lets you grab the camera with a controller, otherwise you'd need a mouse click first
    private static bool GamepadUsed()
    {
        var pad = Gamepad.current;
        if (pad == null) return false;
        if (pad.leftStick.ReadValue().sqrMagnitude > 0.04f || pad.rightStick.ReadValue().sqrMagnitude > 0.04f) return true;
        foreach (var control in pad.allControls)
            if (control is ButtonControl button && button.wasPressedThisFrame) return true;
        return false;
    }

    public static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
