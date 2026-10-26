using UnityEngine;

// --// Branching logic adapted from NightRun Studio (Ep 7): https://www.youtube.com/watch?v=I1y-MOwi1w4
[System.Serializable]
public struct Choice {
    public string choiceText;
    public DialoguePlugin nextDialogue; 
}

public enum DialogueTextEffect {
    None,
    Jitter,
    Wobble,
    Flicker,
    Pulse,
    Sway,
    Sink,
    GlitchJump,
    ColorPulse,
    Rotate,
    GhostTrail,
    BlackoutFocus
}

[System.Serializable]
public class DialogueLine {
    public CharacterProfile character;
    public Emotion emotion;

    [Header("Manual Overrides")]
    public string characterNameOverride;
    public Sprite portraitOverride; 

    [Space]
    [TextArea(3, 10)]
    public string sentence;

    [Space]
    public DialogueTextEffect textEffect;

    [Tooltip("Leave blank to affect the whole line. Can be a word, phrase, or full sentence.")]
    public string targetWord;

    public bool useTextTint;
    public Color textTint = Color.red;

    public bool useCustomOpacity;

    [Range(0f, 1f)]
    public float opacityMultiplier = 1f;
}

[CreateAssetMenu(fileName = "New Conversation", menuName = "Horror VN/Conversation")]
public class DialoguePlugin : ScriptableObject {
    public DialogueLine[] lines;
    
    [Header("Branching Options")]
    public Choice[] choices; 

    // --// Keeps invalid emotion picks from sticking in the asset and trims effect input.
    private void OnValidate() {
        if (lines == null) return;

        foreach (DialogueLine line in lines) {
            if (line.character != null) {
                if (!line.character.HasEmotion(line.emotion)) {
                    Debug.LogWarning(line.character.characterName + " is missing " + line.emotion + ". Swapping to Neutral.");
                    line.emotion = Emotion.Neutral; 
                }
            }

            if (!string.IsNullOrWhiteSpace(line.targetWord)) {
                line.targetWord = line.targetWord.Trim();
            }

            line.opacityMultiplier = Mathf.Clamp01(line.opacityMultiplier);
        }
    }
}
