using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

//-- Swaps the spell slot button hints (LMB, Q...) to the controller ones (L2, LT...) when you're on a pad
//-- Put it on SpellSlotsUI, it finds SpellSlot1-4 by name and keeps the "Spell 1" part of the text
//-- Button names come from the input system itself so DualSense shows L2/R2 and xbox shows LT/RT
//-- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/api/UnityEngine.InputSystem.InputControl.html
public class SpellSlotPrompts : MonoBehaviour
{
    [SerializeField] private int slotCount = 4;

    private TMP_Text[] texts;
    private string[] labels;
    private InputAction[] actions;
    private PlayerInput playerInput;

    private bool lastWasPad;
    private Gamepad lastPad;
    private bool shownOnce;

    void Start()
    {
        playerInput = FindAnyObjectByType<PlayerInput>();

        texts = new TMP_Text[slotCount];
        labels = new string[slotCount];
        actions = new InputAction[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            Transform slot = transform.Find("SpellSlot" + (i + 1));
            if (slot == null) continue;
            texts[i] = slot.GetComponentInChildren<TMP_Text>(true);
            if (texts[i] == null) continue;

            //-- keep whatever the first line says, just swap the button part
            labels[i] = texts[i].text.Split('\n')[0].Trim();
            if (playerInput != null) actions[i] = playerInput.actions.FindAction("Player/Spell" + (i + 1));
        }
    }

    void Update()
    {
        bool onPad = playerInput != null && playerInput.currentControlScheme == "Gamepad";
        Gamepad pad = Gamepad.current;

        //-- only redo the text when something changed
        if (shownOnce && onPad == lastWasPad && pad == lastPad) return;
        shownOnce = true;
        lastWasPad = onPad;
        lastPad = pad;

        for (int i = 0; i < slotCount; i++)
        {
            if (texts[i] == null || actions[i] == null) continue;
            string button = onPad ? ButtonName(actions[i], pad) : ButtonName(actions[i], Mouse.current, Keyboard.current);
            if (button != null) texts[i].text = $"{labels[i]}\n({button})";
        }
    }

    //-- first binding that lands on one of these devices, short name if it has one (LMB, L2, LT...)
    private static string ButtonName(InputAction action, params InputDevice[] devices)
    {
        foreach (var binding in action.bindings)
        {
            foreach (var device in devices)
            {
                if (device == null) continue;
                InputControl control = InputControlPath.TryFindControl(device, binding.effectivePath);
                if (control != null) return string.IsNullOrEmpty(control.shortDisplayName) ? control.displayName : control.shortDisplayName;
            }
        }
        return null;
    }
}
