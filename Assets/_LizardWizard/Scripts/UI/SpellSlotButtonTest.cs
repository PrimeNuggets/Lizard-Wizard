using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.WSA;

public class SpellSlotButtonTest : MonoBehaviour
{
    [SerializeField] private Button SpellSlot1;
    [SerializeField] private Button SpellSlot2;
    [SerializeField] private Button SpellSlot3;
    [SerializeField] private Button SpellSlot4;

    // Update is called once per frame
    void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;

        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                SpellSelect(1);
            }
            else if (mouse.rightButton.wasPressedThisFrame) 
            {
                SpellSelect(2);
            }
        }

        if (keyboard != null)
        {
            if (keyboard.qKey.wasPressedThisFrame)
            {
                SpellSelect(3);
            }
            else if (keyboard.eKey.wasPressedThisFrame)
            {
                SpellSelect(4);
            }
        }
    }

    private void SpellSelect(int spellSlotNum)
    {
        Button selectedSpell;

        switch (spellSlotNum)
        {
            case 1:
                selectedSpell = SpellSlot1;
                break;

            case 2:
                selectedSpell = SpellSlot2;
                break;

            case 3:
                selectedSpell = SpellSlot3;
                break;

            case 4:
                selectedSpell = SpellSlot4;
                break;

            default:
                return;
        }

        if (selectedSpell != null && selectedSpell.isActiveAndEnabled && selectedSpell.IsInteractable())
        {
            selectedSpell.onClick.Invoke();
        }
    }

    public void Spellcast(int spellSlotNum)
    {
        Debug.Log("Spell " + spellSlotNum + " has been cast!");
    }
}
