using UnityEngine;
using TMPro;
using System;

// --// TMP vertex animation setup based on:
// --// https://www.youtube.com/watch?v=FgWVW2PL1bQ
// --// Mesh and transform references:
// --// https://docs.unity3d.com/ja/Packages/com.unity.textmeshpro%403.0/api/TMPro.TMP_TextInfo.CopyMeshInfoVertexData.html
// --// https://docs.unity3d.com/ja/Packages/com.unity.textmeshpro%403.0/api/TMPro.TMP_Text.UpdateVertexData.html
// --// https://docs.unity.cn/560/Documentation/ScriptReference/Matrix4x4.html
// --// https://docs.unity3d.com/ru/2018.4/ScriptReference/Quaternion.Euler.html
// --// Related notes and extra references live in the split partial files under Scripts/Effects.
// --// This main file only handles the runtime flow: reading the line settings, checking what text is visible,
// --// restoring the mesh each frame, and routing the visible characters into the right effect pass.
[RequireComponent(typeof(TMP_Text))]
public partial class HorrorTextAnimator : MonoBehaviour
{
    [Header("Jitter Settings")]
    public float jitterAmount = 2f;

    [Header("Wobble Settings")]
    public float wobbleSpeed = 5f;
    public float wobbleHeight = 3f;

    [Header("Flicker Settings")]
    public float flickerSpeed = 18f;
    [Range(0f, 1f)]
    public float flickerMinimumOpacity = 0.2f;

    [Header("Pulse Settings")]
    public float pulseSpeed = 4f;
    public float pulseScaleAmount = 0.12f;

    [Header("Sway Settings")]
    public float swaySpeed = 3f;
    public float swayDistance = 3f;

    [Header("Sink Settings")]
    public float sinkSpeed = 4f;
    public float sinkDepth = 4f;

    [Header("Glitch Jump Settings")]
    public float glitchJumpAmount = 5f;
    public float glitchJumpHoldTime = 0.06f;

    [Header("Color Pulse Settings")]
    public Color colorPulseTint = new Color(0.85f, 0.1f, 0.1f, 1f);
    public float colorPulseSpeed = 5f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 4f;
    public float rotationAngle = 10f;

    [Header("Ghost Trail Settings")]
    public Color ghostTrailColor = new Color(0.65f, 0.85f, 1f, 0.35f);
    public Vector2 ghostTrailOffset = new Vector2(3f, -2f);
    public float ghostTrailDriftSpeed = 2.5f;
    public float ghostTrailDriftAmount = 1.2f;

    private TMP_Text dialogueText;
    private TextMeshProUGUI ghostText;

    private DialogueTextEffect activeEffect = DialogueTextEffect.None;
    private string activeTargetText = "";
    private bool useTextTint = false;
    private Color activeTint = Color.white;
    private bool useCustomOpacity = false;
    private float activeOpacityMultiplier = 1f;
    private Vector3[] glitchOffsets = Array.Empty<Vector3>();
    private float[] glitchRefreshTimes = Array.Empty<float>();

    void Awake()
    {
        // --// Cache the TMP component once so every effect pass can work off the same text object.
        dialogueText = GetComponent<TMP_Text>();
    }

    public void ApplyLineSettings(DialogueLine line)
    {
        // --// Called by DialogueManager before the typewriter starts the new line.
        // --// This is where the animator picks up the per-line settings from the ScriptableObject data.
        if (line == null)
        {
            ClearSettings();
            return;
        }

        activeEffect = line.textEffect;

        // --// The serialized field name is still targetWord, but it can match a word, phrase, or full line.
        activeTargetText = string.IsNullOrWhiteSpace(line.targetWord) ? "" : line.targetWord.Trim();

        useTextTint = line.useTextTint;
        activeTint = line.textTint;
        useCustomOpacity = line.useCustomOpacity;
        activeOpacityMultiplier = Mathf.Clamp01(line.opacityMultiplier);

        ResetBlackoutFocusTimer();
        ResetToBaseMesh();
    }

    public void ClearSettings()
    {
        // --// Reset everything back to a plain dialogue line so old effects never leak into the next sentence.
        activeEffect = DialogueTextEffect.None;
        activeTargetText = "";
        useTextTint = false;
        activeTint = Color.white;
        useCustomOpacity = false;
        activeOpacityMultiplier = 1f;

        ResetToBaseMesh();
        HideBlackoutFocus();
        HideGhostTrail();
    }

    void LateUpdate()
    {
        if (dialogueText == null) return;
        if (!HasAnyActiveEffect())
        {
            HideBlackoutFocus();
            HideGhostTrail();
            return;
        }

        // --// Pull the current TMP data before applying this frame's offsets and colors.
        // --// Running this in LateUpdate keeps the effect pass behind the typewriter reveal for the same frame.
        dialogueText.ForceMeshUpdate();
        TMP_TextInfo textInfo = dialogueText.textInfo;

        if (textInfo.characterCount == 0) return;

        EnsureGlitchBuffers(textInfo.characterCount);

        int visibleCharacterCount = dialogueText.maxVisibleCharacters;

        // --// Nothing has been revealed yet.
        if (visibleCharacterCount == 0)
        {
            HideBlackoutFocus();
            HideGhostTrail();
            return;
        }

        // --// QuickFinish and TMP defaults both mean "treat the whole line as visible."
        if (visibleCharacterCount < 0 || visibleCharacterCount > textInfo.characterCount)
        {
            visibleCharacterCount = textInfo.characterCount;
        }

        // --// Start from a clean mesh so old offsets and colors never stack frame to frame.
        // --// Without this reset step, shaky words would drift or keep stale tint data from the previous frame.
        TMP_MeshInfo[] baseMeshInfo = textInfo.CopyMeshInfoVertexData();
        RestoreBaseMesh(textInfo, baseMeshInfo);
        HideCharactersPastLimit(textInfo, visibleCharacterCount);

        if (activeEffect == DialogueTextEffect.BlackoutFocus)
        {
            // --// BlackoutFocus uses its own overlay + focus-text path, so it exits early after that special pass.
            UpdateBlackoutFocusTimer(visibleCharacterCount);
            float blackoutStrength = GetBlackoutStrength();

            if (blackoutStrength <= 0f)
            {
                PushVertexChanges();
                HideBlackoutFocus();
                HideGhostTrail();
                return;
            }

            ApplyBlackoutBaseStyle(textInfo, visibleCharacterCount, blackoutStrength);
            PushVertexChanges();
            UpdateBlackoutFocus(visibleCharacterCount, blackoutStrength);
            HideGhostTrail();
            return;
        }

        // --// All regular effects go through the same matcher so one word, a phrase, or the whole line can be targeted.
        ApplyToTargetText(dialogueText.text, textInfo, visibleCharacterCount, ApplyToCharacter);

        PushVertexChanges();
        HideBlackoutFocus();

        if (activeEffect == DialogueTextEffect.GhostTrail)
        {
            UpdateGhostTrail(visibleCharacterCount);
        }
        else
        {
            HideGhostTrail();
        }
    }

    private void ApplyToTargetText(string sourceText, TMP_TextInfo textInfo, int visibleCharacterCount, Action<TMP_TextInfo, int> applyAction)
    {
        if (string.IsNullOrWhiteSpace(sourceText)) return;

        // --// A blank target applies the effect to the whole visible line.
        if (string.IsNullOrEmpty(activeTargetText))
        {
            for (int i = 0; i < visibleCharacterCount; i++)
            {
                applyAction(textInfo, i);
            }

            return;
        }

        int searchStart = 0;

        // --// Substring matching allows single words, phrases, or full-sentence targets.
        // --// This keeps the designer out of TMP tag syntax and lets the Inspector stay drag-and-drop.
        while (searchStart < sourceText.Length)
        {
            int matchIndex = sourceText.IndexOf(activeTargetText, searchStart, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0) break;
            if (matchIndex >= visibleCharacterCount) break;

            int matchEnd = Mathf.Min(matchIndex + activeTargetText.Length, visibleCharacterCount);

            for (int i = matchIndex; i < matchEnd; i++)
            {
                applyAction(textInfo, i);
            }

            searchStart = matchIndex + Mathf.Max(1, activeTargetText.Length);
        }
    }

    private void ApplyToCharacter(TMP_TextInfo textInfo, int charIndex)
    {
        if (charIndex < 0 || charIndex >= textInfo.characterCount) return;

        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        Vector3[] verts = textInfo.meshInfo[materialIndex].vertices;
        Color32[] colors = textInfo.meshInfo[materialIndex].colors32;
        Vector3 charMidPoint = (verts[vertexIndex + 0] + verts[vertexIndex + 2]) * 0.5f;

        // --// Every effect ultimately turns into either a transform, a color change, or both on this character quad.
        ApplyTransform(verts, vertexIndex, charMidPoint, GetOffset(charIndex), GetRotation(charIndex), GetScale(charIndex));

        if (useTextTint || useCustomOpacity || activeEffect == DialogueTextEffect.Flicker || activeEffect == DialogueTextEffect.ColorPulse)
        {
            ApplyColor(colors, vertexIndex, charIndex);
        }
    }

    private void ResetToBaseMesh()
    {
        if (dialogueText == null) return;

        // --// Push the untouched TMP mesh back out so the text returns to normal when an effect is cleared.
        dialogueText.ForceMeshUpdate();
        dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private bool HasAnyActiveEffect()
    {
        return activeEffect != DialogueTextEffect.None || useTextTint || useCustomOpacity;
    }

    private void PushVertexChanges()
    {
        // --// Flush the vertex/color edits for this frame back into TMP.
        dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}
