using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

//-- Swaps the spell slot button hints (LMB, Q...) to the controller ones (L2, LT...) when you're on a pad
//-- Button names come from the input system itself so DualSense shows L2/R2 and xbox shows LT/RT
//-- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/api/UnityEngine.InputSystem.InputControl.html
//-- spells with an icon show the icon + cooldown sweep + button in the white circle
//-- no icon = old green box with "Spell 1 (LMB)"
public class SpellSlotPrompts : MonoBehaviour
{
    [Serializable]
    private class Slot
    {
        public Image background;
        public TMP_Text label;
        public Image icon;
        [Tooltip("Filled image on top of the icon")] public Image cooldown;
        [Tooltip("Text in the white circle")] public TMP_Text badge;
        [NonSerialized] public string labelStart;
        [NonSerialized] public InputAction action;
    }

    [SerializeField] private Slot[] slots;

    private PlayerInput playerInput;
    private Player player;

    private bool lastWasPad;
    private Gamepad lastPad;
    private bool shownOnce;

    void Start()
    {
        playerInput = FindAnyObjectByType<PlayerInput>();
        player = FindAnyObjectByType<Player>();

        for (int i = 0; i < slots.Length; i++)
        {
            //-- keep whatever the first line says, just swap the button part
            if (slots[i].label != null) slots[i].labelStart = slots[i].label.text.Split('\n')[0].Trim();
            if (playerInput != null) slots[i].action = playerInput.actions.FindAction("Player/Spell" + (i + 1));
        }
    }

    void Update()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            SpellData spell = player != null ? player.GetSpell(i) : null;
            ShowSpell(slots[i], spell);
        }

        bool onPad = playerInput != null && playerInput.currentControlScheme == "Gamepad";
        Gamepad pad = Gamepad.current;

        //-- only redo the text when something changed
        if (shownOnce && onPad == lastWasPad && pad == lastPad) return;
        shownOnce = true;
        lastWasPad = onPad;
        lastPad = pad;

        foreach (var slot in slots)
        {
            if (slot.action == null) continue;
            InputControl control = onPad ? ButtonIcons.FindControl(slot.action, pad) : ButtonIcons.FindControl(slot.action, Mouse.current, Keyboard.current);
            if (control == null) continue;
            string button = ButtonIcons.Icon(control);
            if (slot.label != null) slot.label.text = $"{slot.labelStart}\n({button})";
            if (slot.badge != null) slot.badge.text = button;
        }
    }

    private static void ShowSpell(Slot slot, SpellData spell)
    {
        bool hasIcon = spell != null && spell.icon != null && slot.icon != null;

        if (slot.background != null) slot.background.enabled = !hasIcon;
        if (slot.label != null) slot.label.gameObject.SetActive(!hasIcon);
        if (slot.icon != null) slot.icon.gameObject.SetActive(hasIcon);
        if (slot.cooldown != null) slot.cooldown.gameObject.SetActive(hasIcon);
        if (slot.badge != null) slot.badge.gameObject.SetActive(hasIcon);
        if (!hasIcon) return;

        slot.icon.sprite = spell.icon;
        if (slot.cooldown != null)
        {
            slot.cooldown.sprite = spell.icon; //-- same sprite so the sweep is round
            slot.cooldown.fillAmount = spell.CooldownPercent();
        }
    }
}
