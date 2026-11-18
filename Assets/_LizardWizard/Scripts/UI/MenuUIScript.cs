using System.Collections.Generic;
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
    [SerializeField] private TMP_Text assistedCritters;
    private bool gameIsPaused;
    private InputAction pauseAction; //-- Esc or Start, from the player's input actions
    //-- settings submenu, built from the pause menu buttons at start
    private DisplaySettings displaySettings = new DisplaySettings();
    private Button[] pauseButtons;
    private Button[] settingsButtons;
    private Button openSettingsButton;
    private TMP_Text pauseHeading;
    private TMP_Text frameRateLabel;
    private TMP_Text resolutionLabel;
    private string pauseHeadingText;
    private TextAlignmentOptions pauseHeadingAlignment;
    private bool settingsOpen;
    private TextMeshProUGUI trickBoostLabel;
    private float trickBoostLabelTime;

    void Start()
    {
        //=========================================================
            //Cursor Locking
        //=========================================================
        SetCursorLocked(false); //Start unlocked so that it's easier to test in the editor

        var input = FindAnyObjectByType<PlayerInput>();
        if (input != null) pauseAction = input.actions.FindAction("Player/Pause");

        displaySettings.Load();
        CreateSettingsMenu();
    }
    void Update()
    {
        #region Pause
            if (PausePressed())
            {
                if (settingsOpen)
                {
                    ShowSettings(false);
                }
                else if (gameIsPaused)
                {
                    ResumeGameplay();
                }
                else
                {
                    PauseGame();
                }
            }
        #endregion
        if (!gameIsPaused)
        {
            assistedCritters.text = "Critters Assisted: " + Scoring.FindValue("npc.saved").ToString();
            //-- fade out the trick boost text
            if (trickBoostLabelTime > 0f)
            {
                trickBoostLabelTime = Mathf.Max(0f, trickBoostLabelTime - Time.deltaTime);
                trickBoostLabel.alpha = Mathf.Clamp01(trickBoostLabelTime / 0.25f);
                if (trickBoostLabelTime == 0f) trickBoostLabel.gameObject.SetActive(false);
            }
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || GamepadUsed())
                {
                    SetCursorLocked(true);
                }
            }
        }
    }

    //-- "TRICK BOOST" text at the top of the screen, made the first time you land a flip
    public void ShowTrickBoost()
    {
        if (trickBoostLabel == null)
        {
            var canvas = assistedCritters.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var labelObject = new GameObject("Trick Boost", typeof(RectTransform));
            labelObject.transform.SetParent(canvas.transform, false);
            trickBoostLabel = labelObject.AddComponent<TextMeshProUGUI>();
            trickBoostLabel.text = "TRICK BOOST";
            trickBoostLabel.font = assistedCritters.font;
            trickBoostLabel.fontSize = 32f;
            trickBoostLabel.fontStyle = FontStyles.Bold;
            trickBoostLabel.alignment = TextAlignmentOptions.Center;
            trickBoostLabel.raycastTarget = false;

            RectTransform rect = trickBoostLabel.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -70f);
            rect.sizeDelta = new Vector2(360f, 52f);
        }

        trickBoostLabelTime = 0.9f;
        trickBoostLabel.alpha = 1f;
        trickBoostLabel.gameObject.SetActive(true);
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
            Selectable firstButton = pauseButtons != null && pauseButtons.Length > 0
                ? pauseButtons[0]
                : PauseMenu.GetComponentInChildren<Selectable>();
            if (firstButton != null) EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }
    }
    
    public void ResumeGameplay()
    {
        if (settingsOpen) ShowSettings(false);
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
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX("Lizard");
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

    //-- copies the existing pause buttons so the settings ones match the style
    //-- settings button goes right above the last one (quit)
    private void CreateSettingsMenu()
    {
        if (PauseMenu == null) return;
        var originalButtons = new List<Button>(PauseMenu.GetComponentsInChildren<Button>(true));
        if (originalButtons.Count == 0) return;
        //-- top to bottom
        originalButtons.Sort((a, b) =>
            ((RectTransform)b.transform).anchoredPosition.y.CompareTo(((RectTransform)a.transform).anchoredPosition.y));
        var template = originalButtons[0];

        //-- the heading is the text sitting right under the menu (not inside a button)
        foreach (var text in PauseMenu.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.transform.parent == PauseMenu.transform)
            {
                pauseHeading = text;
                pauseHeadingText = text.text;
                pauseHeadingAlignment = text.alignment;
                break;
            }
        }

        openSettingsButton = CreateButton(template, "Settings", () => ShowSettings(true));
        originalButtons.Insert(originalButtons.Count - 1, openSettingsButton);
        pauseButtons = originalButtons.ToArray();
        for (int i = 0; i < pauseButtons.Length; i++)
        {
            PositionButton(pauseButtons[i], 122 - 65 * i);
        }

        Button frameRate = CreateButton(template, "Frame Rate", ToggleFrameRate);
        Button resolution = CreateButton(template, "Resolution", CycleResolution);
        Button back = CreateButton(template, "Back", () => ShowSettings(false));
        frameRateLabel = frameRate.GetComponentInChildren<TMP_Text>(true);
        resolutionLabel = resolution.GetComponentInChildren<TMP_Text>(true);
        settingsButtons = new[] { frameRate, resolution, back };
        for (int i = 0; i < settingsButtons.Length; i++)
        {
            PositionButton(settingsButtons[i], 110 - 80 * i);
            settingsButtons[i].gameObject.SetActive(false);
        }
    }

    private Button CreateButton(Button template, string label, UnityEngine.Events.UnityAction action)
    {
        Button button = Instantiate(template, PauseMenu.transform);
        button.name = label;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        text.text = label;
        text.enableAutoSizing = true;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.fontSizeMin = 16f;
        text.fontSizeMax = 24f;
        return button;
    }

    private static void PositionButton(Button button, float y)
    {
        var rect = (RectTransform)button.transform;
        rect.anchoredPosition = new Vector2(0, y);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 340f);
    }

    private void ShowSettings(bool show)
    {
        if (settingsButtons == null) return;
        settingsOpen = show;
        if (pauseHeading != null)
        {
            pauseHeading.text = show ? "Settings" : pauseHeadingText;
            pauseHeading.alignment = show ? TextAlignmentOptions.Center : pauseHeadingAlignment;
        }
        foreach (Button button in pauseButtons) button.gameObject.SetActive(!show);
        foreach (Button button in settingsButtons) button.gameObject.SetActive(show);
        if (show) RefreshSettingsLabels();
        if (EventSystem.current != null)
        {
            Button selectedButton = show ? settingsButtons[0] : openSettingsButton;
            EventSystem.current.SetSelectedGameObject(selectedButton.gameObject);
        }
    }

    private void ToggleFrameRate()
    {
        displaySettings.ToggleBatterySaving();
        RefreshSettingsLabels();
    }

    private void CycleResolution()
    {
        displaySettings.CycleResolution();
        RefreshSettingsLabels();
    }

    private void RefreshSettingsLabels()
    {
        frameRateLabel.text = displaySettings.BatterySaving ? "Frame Rate: Battery Saving (60 FPS)" : "Frame Rate: Uncapped";
        Vector2Int resolution = displaySettings.Resolution;
        resolutionLabel.text = "Resolution: " + resolution.x + "x" + resolution.y;
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
