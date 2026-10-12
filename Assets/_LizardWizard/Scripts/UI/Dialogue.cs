using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Dialogue : MonoBehaviour
{
    [SerializeField] private TMP_Text textBox;
    [SerializeField] private int charLimit = 165;
    private List<string> lines;
    public int currentLineIndex { get; private set; } = 0;
    private InputAction interactAction;
    [SerializeField] private GameObject player; //Since only one instance of this script will exist per scene, serializing this field is ok
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [NonSerialized] public static bool inDialogue = false;
    private bool waitForRelease = false;
    public NPC npc { get; private set; }
    [SerializeField, LabelOverride("Interaction Button Text")] private TMP_Text iButtonText;
    void Start()
    {
        var inputs = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        interactAction = inputs.actions.FindAction("Player/Interact", true);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        iButtonText.text = interactAction.GetBindingDisplayString(0, InputBinding.DisplayStringOptions.DontIncludeInteractions);
        if (interactAction.IsPressed()) {
            if (!waitForRelease && inDialogue) //Despite being in fixed update, this only runs while the object is enabled
            {
                ContinueDialogue();
            }
            waitForRelease = true;
        } else {
            waitForRelease = false;
        }
    }

    public void StartDialogue(NPC npc)
    {
        gameObject.SetActive(true);
        waitForRelease = true;
        this.npc = npc;
        lines = new List<string>(npc.dialogue);
        currentLineIndex = 0;
        inDialogue = true;
        UpdateText();
    }

    public void ContinueDialogue()
    {
        if (currentLineIndex + 1 < lines.Count)
        {
            currentLineIndex += 1;
            UpdateText();
        } else
        {
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        inDialogue = false;
        npc = null;
        lines?.Clear();
        textBox.text = "";
        currentLineIndex = 0;
        gameObject.SetActive(false);
    }

    void UpdateText()
    {
        //Check if the line is greater than 165 characters
        //If so, split at the word before 165 characters (or if the word is 10+ characters, split & hyphenate the word at character 165)
        textBox.text = lines[currentLineIndex];
    }
}
