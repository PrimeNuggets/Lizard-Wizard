using UnityEngine;

public class DialogueTrigger : MonoBehaviour 
{
    // --// Drag a Conversation asset into this slot in the Inspector.
    public DialoguePlugin dialogue;

    public void TriggerDialogue()
    {
        FindAnyObjectByType<DialogueManager>().StartDialogue(dialogue);
    }

    // --// Validates dialogue lines as soon as the asset reference changes in the Inspector.
    private void OnValidate()
    {
        if (dialogue == null || dialogue.lines == null) return;

        foreach (DialogueLine line in dialogue.lines)
        {
            if (line.character != null)
            {
                // --// Keep the selected emotion valid for the assigned character profile.
                if (!line.character.HasEmotion(line.emotion))
                {
                    Debug.LogWarning($"[Horror VN Kit] {line.character.characterName} does not have the '{line.emotion}' expression! Snapping back to Neutral.");
                    
                    // --// Snap back to Neutral so the line never points at a missing portrait.
                    line.emotion = Emotion.Neutral; 
                }
            }
        }
    }
}
