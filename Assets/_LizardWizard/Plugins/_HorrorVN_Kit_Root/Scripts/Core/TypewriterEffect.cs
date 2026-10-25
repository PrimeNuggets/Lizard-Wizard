using System.Collections;
using UnityEngine;
using TMPro;

// --// Skip logic from Top-Down Tutorial #22: https://www.youtube.com/watch?v=zbYuLu_8spI
// --// TMP reveal stays on maxVisibleCharacters so the mesh stays stable while text effects run.
// --// Reddit thread used for TMP reveal timing / characterCount quirks:
// --// https://www.reddit.com/r/Unity3D/comments/gzldxf
// --// TMP reveal timing threads:
// --// https://www.reddit.com/r/unity/comments/1p3bbdx/how_do_you_implement_typewriter_effect_in_dialogs/
// --// https://www.reddit.com/r/Unity3D/comments/k0mgmh/pokemonlike_dialogue_scrolling_how_to_check_the/
public class TypewriterEffect : MonoBehaviour
{
    [Header("Typewriter Settings")]
    [Tooltip("Delay between each letter appearing. Higher is slower.")]
    [Range(0.01f, 0.3f)]
    public float typingSpeed = 0.04f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip[] typingSounds; 
    
    [Tooltip("Play a sound every X letters. 1 = every letter, 2 = every other letter, etc.")]
    [Range(1, 5)]
    public int playSoundEveryXLetters = 2; 

    private Coroutine typingCoroutine;
    private string fullText;
    private TextMeshProUGUI label;

    // --// Read by DialogueManager to decide whether to skip or advance.
    public bool isTyping { get; private set; }

    public void Run(string textToType, TextMeshProUGUI textLabel)
    {
        label = textLabel;
        fullText = textToType;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // --// Set the new line immediately so the next sentence never flashes in early.
        label.text = fullText;
        label.maxVisibleCharacters = 0;
        label.ForceMeshUpdate();

        typingCoroutine = StartCoroutine(TypeText(textToType, textLabel));
    }

    // --// Shows the whole line immediately when the player skips ahead.
    public void QuickFinish()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (label != null)
        {
            label.text = fullText;
            label.maxVisibleCharacters = int.MaxValue;
        }

        isTyping = false;
    }

    private IEnumerator TypeText(string textToType, TextMeshProUGUI textLabel)
    {
        isTyping = true;

        // --// Keep the full sentence loaded and reveal it with maxVisibleCharacters.
        textLabel.text = textToType;
        textLabel.maxVisibleCharacters = 0;
        textLabel.ForceMeshUpdate();

        int totalCharacters = textLabel.textInfo.characterCount;
        int visibleCharacterCounter = 0;

        for (int i = 0; i < totalCharacters; i++)
        {
            textLabel.maxVisibleCharacters = i + 1;
            char currentChar = textLabel.textInfo.characterInfo[i].character;

            // --// Only count visible, non-whitespace letters for typing audio.
            if (!char.IsWhiteSpace(currentChar) && audioSource != null && typingSounds.Length > 0 && visibleCharacterCounter % playSoundEveryXLetters == 0)
            {
                int randomIndex = Random.Range(0, typingSounds.Length);
                audioSource.PlayOneShot(typingSounds[randomIndex]);
            }

            if (!char.IsWhiteSpace(currentChar))
            {
                visibleCharacterCounter++;
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        textLabel.maxVisibleCharacters = int.MaxValue;
        typingCoroutine = null;
        isTyping = false;
    }
}
