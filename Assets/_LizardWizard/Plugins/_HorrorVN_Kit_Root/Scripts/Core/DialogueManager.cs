using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// --// Combined logic from:
// --// NightRun Studio (Branching): https://www.youtube.com/watch?v=I1y-MOwi1w4
// --// Top-Down Tutorial (Skip): https://www.youtube.com/watch?v=zbYuLu_8spI
[RequireComponent(typeof(TypewriterEffect))]
public class DialogueManager : MonoBehaviour 
{
    [Header("UI Slots")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Image portraitImage;
    public Button nextButton;

    [Header("Choice Settings")]
    public GameObject choiceButtonPrefab;
    public Transform choiceParent; 

    private Queue<DialogueLine> dialogueLines; 
    private TypewriterEffect typewriter;
    private DialoguePlugin currentDialogue;
    private HorrorTextAnimator horrorTextAnimator;

    // --// Tracks finished conversation assets so repeat branches can stay hidden.
    private HashSet<string> seenDialogues = new HashSet<string>();

    void Awake() 
    {
        dialogueLines = new Queue<DialogueLine>();
        typewriter = GetComponent<TypewriterEffect>();

        if (dialogueText != null)
        {
            horrorTextAnimator = dialogueText.GetComponent<HorrorTextAnimator>();
        }

        choiceParent.gameObject.SetActive(false);
    }

    public void StartDialogue(DialoguePlugin dialogue)
    {
        currentDialogue = dialogue;

        if (horrorTextAnimator != null)
        {
            horrorTextAnimator.ClearSettings();
        }

        // --// Mark this asset right away so follow-up buttons can filter against it.
        if (dialogue != null && !seenDialogues.Contains(dialogue.name))
        {
            seenDialogues.Add(dialogue.name);
        }

        dialogueLines.Clear();
        choiceParent.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(true);

        foreach (DialogueLine line in dialogue.lines) 
        {
            dialogueLines.Enqueue(line);
        }
        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (typewriter.isTyping) 
        {
            typewriter.QuickFinish();
            return;
        }

        if (dialogueLines.Count == 0)
        {
            ShowChoices();
            return;
        }

        DialogueLine currentLine = dialogueLines.Dequeue();
        
        string finalName = currentLine.characterNameOverride;
        if (currentLine.character != null && string.IsNullOrEmpty(finalName)) 
        {
            finalName = currentLine.character.characterName;
        }
        nameText.text = finalName;

        Sprite finalPortrait = currentLine.portraitOverride;
        if (currentLine.character != null && finalPortrait == null) 
        {
            finalPortrait = currentLine.character.GetPortrait(currentLine.emotion);
        }

        if (finalPortrait != null)
        {
            portraitImage.sprite = finalPortrait;
            portraitImage.color = Color.white;
        }
        else 
        {
            portraitImage.color = Color.clear;
        }

        // --// Pass the line settings over before the typewriter starts.
        if (horrorTextAnimator != null)
        {
            horrorTextAnimator.ApplyLineSettings(currentLine);
        }

        typewriter.Run(currentLine.sentence, dialogueText);
    }

    void ShowChoices()
    {
        foreach (Transform child in choiceParent) 
        {
            Destroy(child.gameObject);
        }

        if (currentDialogue.choices == null || currentDialogue.choices.Length == 0)
        {
            EndDialogue();
            return;
        }

        int buttonsSpawned = 0;

        foreach (var choice in currentDialogue.choices)
        {
            // --// Skip buttons that would loop back into a finished branch.
            if (choice.nextDialogue != null && seenDialogues.Contains(choice.nextDialogue.name))
            {
                continue;
            }

            buttonsSpawned++;
            GameObject btn = Instantiate(choiceButtonPrefab, choiceParent);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = choice.choiceText;
            
            btn.GetComponent<Button>().onClick.AddListener(() => StartDialogue(choice.nextDialogue));
        }

        // --// Close the box if every follow-up branch has already been used.
        if (buttonsSpawned == 0)
        {
            EndDialogue();
            return;
        }

        nextButton.gameObject.SetActive(false);
        choiceParent.gameObject.SetActive(true);
    }

    void EndDialogue()
    {
        nextButton.gameObject.SetActive(false);
        choiceParent.gameObject.SetActive(false);
        if(portraitImage != null) portraitImage.color = Color.clear;

        if (horrorTextAnimator != null)
        {
            horrorTextAnimator.ClearSettings();
        }

        Debug.Log("Conversation Complete.");
    }

    public void ResetMemory()
    {
        seenDialogues.Clear();
    }
}
