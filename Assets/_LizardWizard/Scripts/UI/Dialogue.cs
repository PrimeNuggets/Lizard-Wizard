using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Dialogue : MonoBehaviour
{
    [Serializable]
    public class ConvoConds
    {
        public string label;
        public Conversations convo;
        [TextArea] public string conditions;

        public bool ParseConditions()
        {
            if (string.IsNullOrWhiteSpace(conditions))
            {
                return true;
            }
            string[] rows = conditions.Split('\n');
            bool passed = true;

            foreach (string row in rows)
            {
                string condition = row.Trim();
                if (condition.Length == 0) continue;

                // Extract: score key, comparison operator, integer.
                Match match = Regex.Match(
                    condition,
                    @"^([A-Za-z_][A-Za-z0-9_.]*)\s*(>=|<=|==|!=|>|<)\s*(-?[0-9]+)$"
                );

                if (!match.Success || !int.TryParse(match.Groups[3].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int required))
                {
                    Debug.LogWarning($"Invalid conversation condition:\n{condition}");
                    return false;
                }

                string key = match.Groups[1].Value;
                string comparison = match.Groups[2].Value;
                int value = Scoring.FindValue(key);

                bool rowPassed;

                switch (comparison)
                {
                    case ">=":
                        rowPassed = value >= required;
                        break;
                    case "<=":
                        rowPassed = value <= required;
                        break;
                    case "==":
                        rowPassed = value == required;
                        break;
                    case "!=":
                        rowPassed = value != required;
                        break;
                    case ">":
                        rowPassed = value > required;
                        break;
                    case "<":
                        rowPassed = value < required;
                        break;
                    default:
                        return false;
                }

                passed &= rowPassed;
            }
            return passed;
        }
    }
    [LabelOverride("Character Limit")] public const int CHARLIMIT = 165;
    [SerializeField] private TMP_Text textBox;
    private Conversations currentConvo;
    public int currentLineIndex { get; private set; } = 0;
    private InputAction interactAction;
    [SerializeField] private GameObject player; //Since only one instance of this script will exist per scene, serializing this field is ok
    [SerializeField] private QuestRunner questRunner;
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
        UpdateCurrentDialogue(this.npc);

        /*if (currentConvo == null ||
            currentConvo.lines == null ||
            currentConvo.lines.Count == 0
        )
        {
            EndDialogue();
            return;
        }*/

        currentLineIndex = 0;
        inDialogue = true;

        UpdateText();
    }

    public void UpdateCurrentDialogue(NPC npc)
    {
        currentConvo = null;
        //if (npc.dialogue == null) return;

        foreach (var condition in npc.dialogue)
        {
            if (condition.ParseConditions()) { //Sets the conversation to the first available convo
                currentConvo = condition.convo;
                break;
            }
        }
    }

    public void ContinueDialogue()
    {
        UpdateQuest();
        if (currentLineIndex + 1 < currentConvo.lines.Count)
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
        currentConvo = null;
        textBox.text = "";
        currentLineIndex = 0;
        gameObject.SetActive(false);
    }

    void UpdateText()
    {
        //Check if the line is greater than 165 characters
        //If so, split at the word before 165 characters (or if the word is 10+ characters, split & hyphenate the word at character 165)
        textBox.text = currentConvo.GetLine(currentLineIndex);
    }

    void UpdateQuest()
    {
        var quest = currentConvo.GetProgression(currentLineIndex);
        if (quest != null)
        {
            Player playerComp = player.GetComponent<Player>();
            bool ongoing = playerComp.ongoingQuests.Contains(quest);
            bool completed = playerComp.completedQuests.Contains(quest);

            if (ongoing && !completed) { //Checks if the quest is an ongoing quest
                questRunner.ProgressQuest(quest);
            } else if (!ongoing && !completed)
            {
                playerComp.StartQuest(quest);
            }
        }
    }
}
