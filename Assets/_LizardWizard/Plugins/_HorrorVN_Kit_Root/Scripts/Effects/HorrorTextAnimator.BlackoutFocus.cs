using TMPro;
using UnityEngine;
using UnityEngine.UI;

// --// Blackout overlay references:
// --// Canvas draw order: https://docs.unity3d.com/es/2019.4/Manual/UICanvas.html
// --// UI.Image: https://docs.unity3d.com/ru/2018.4/ScriptReference/UI.Image.html
// --// Graphic.color: https://docs.unity3d.com/es/2017.4/ScriptReference/UI.Graphic-color.html
// --// Reddit threads used while sorting out the fullscreen overlay setup:
// --// https://www.reddit.com/r/Unity3D/comments/jv9ju8
// --// https://www.reddit.com/r/Unity2D/comments/1n7skle
// --// TMP visibility timing reference:
// --// https://docs.unity3d.com/ja/Packages/com.unity.textmeshpro%403.0/api/TMPro.TMP_Text.maxVisibleCharacters.html
public partial class HorrorTextAnimator
{
    [Header("Blackout Focus Settings")]
    [Range(0f, 1f)]
    public float blackoutOverlayAlpha = 0.88f;
    public float blackoutFlickerSpeed = 22f;
    [Range(0f, 1f)]
    public float blackoutFlickerAmount = 0.18f;
    [Range(0f, 1f)]
    public float blackoutBackgroundTextAlpha = 0.06f;
    public float blackoutHoldDuration = 2f;
    public float blackoutHoldPerCharacter = 0.1f;
    public float blackoutMaxHoldDuration = 3f;
    public float blackoutIntroFlickerDuration = 0.3f;
    public float blackoutFadeDuration = 0.85f;

    private Image blackoutOverlay;
    private TextMeshProUGUI blackoutFocusText;
    private float blackoutStartedAt = -1f;
    private float currentBlackoutStrength = 0f;

    private void ApplyBlackoutBaseStyle(TMP_TextInfo textInfo, int visibleCharacterCount, float blackoutStrength)
    {
        // --// During the blackout, the main dialogue line gets pushed almost to invisible so the focus text can stand out.
        float visibleAlpha = Mathf.Lerp(1f, blackoutBackgroundTextAlpha, blackoutStrength);

        for (int i = 0; i < visibleCharacterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

            for (int j = 0; j < 4; j++)
            {
                Color32 currentColor = colors[vertexIndex + j];
                currentColor.r = 255;
                currentColor.g = 255;
                currentColor.b = 255;
                currentColor.a = (byte)(255 * visibleAlpha);
                colors[vertexIndex + j] = currentColor;
            }
        }
    }

    private void UpdateBlackoutFocus(int visibleCharacterCount, float blackoutStrength)
    {
        // --// This effect uses two extra UI layers:
        // --// 1. a fullscreen black image
        // --// 2. a second TMP label that only redraws the focused word or phrase in white
        EnsureBlackoutOverlay();
        EnsureBlackoutFocusText();
        currentBlackoutStrength = blackoutStrength;

        if (blackoutOverlay != null)
        {
            float alpha = blackoutOverlayAlpha;

            // --// Let the effect flicker only during the intro beat, then hold steady so the highlighted text is readable.
            if (ShouldFlickerBlackout())
            {
                float flicker = Mathf.PerlinNoise(Time.time * blackoutFlickerSpeed, 0.5f);
                alpha -= flicker * blackoutFlickerAmount;
            }

            alpha = Mathf.Clamp01(alpha * blackoutStrength);
            blackoutOverlay.enabled = true;
            blackoutOverlay.color = new Color(0f, 0f, 0f, alpha);
        }

        if (blackoutFocusText == null) return;

        blackoutFocusText.enabled = true;
        blackoutFocusText.text = dialogueText.text;
        blackoutFocusText.maxVisibleCharacters = visibleCharacterCount;
        blackoutFocusText.ForceMeshUpdate();

        TMP_TextInfo focusTextInfo = blackoutFocusText.textInfo;
        if (focusTextInfo.characterCount == 0) return;

        TMP_MeshInfo[] baseMeshInfo = focusTextInfo.CopyMeshInfoVertexData();
        RestoreBaseMesh(focusTextInfo, baseMeshInfo);
        HideAllCharacters(focusTextInfo);
        ApplyToTargetText(blackoutFocusText.text, focusTextInfo, visibleCharacterCount, ApplyToBlackoutFocusCharacter);

        blackoutFocusText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        blackoutFocusText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void EnsureBlackoutOverlay()
    {
        if (blackoutOverlay != null) return;

        TextMeshProUGUI uiText = dialogueText as TextMeshProUGUI;
        if (uiText == null) return;

        Canvas canvas = uiText.canvas != null ? uiText.canvas.rootCanvas : uiText.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // --// Spawn this once and reuse it for later lines instead of rebuilding overlay objects every time.
        GameObject overlayObject = new GameObject("DialogueText_BlackoutOverlay");
        overlayObject.layer = canvas.gameObject.layer;
        RectTransform overlayRect = overlayObject.AddComponent<RectTransform>();
        CanvasRenderer overlayRenderer = overlayObject.AddComponent<CanvasRenderer>();
        blackoutOverlay = overlayObject.AddComponent<Image>();

        overlayRect.SetParent(canvas.transform, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.SetAsLastSibling();

        overlayRenderer.cullTransparentMesh = true;
        blackoutOverlay.raycastTarget = false;
        blackoutOverlay.color = new Color(0f, 0f, 0f, 0f);
        blackoutOverlay.enabled = false;
    }

    private void EnsureBlackoutFocusText()
    {
        if (blackoutFocusText != null) return;

        TextMeshProUGUI uiText = dialogueText as TextMeshProUGUI;
        if (uiText == null) return;

        Canvas canvas = uiText.canvas != null ? uiText.canvas.rootCanvas : uiText.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // --// This duplicate TMP object only draws the targeted text that should survive the blackout.
        GameObject focusObject = new GameObject("DialogueText_BlackoutFocus");
        focusObject.layer = canvas.gameObject.layer;
        RectTransform focusRect = focusObject.AddComponent<RectTransform>();
        focusObject.AddComponent<CanvasRenderer>();
        blackoutFocusText = focusObject.AddComponent<TextMeshProUGUI>();

        focusRect.SetParent(canvas.transform, false);
        focusRect.anchorMin = uiText.rectTransform.anchorMin;
        focusRect.anchorMax = uiText.rectTransform.anchorMax;
        focusRect.pivot = uiText.rectTransform.pivot;
        focusRect.sizeDelta = uiText.rectTransform.sizeDelta;
        focusRect.anchoredPosition = uiText.rectTransform.anchoredPosition;
        focusRect.localRotation = uiText.rectTransform.localRotation;
        focusRect.localScale = uiText.rectTransform.localScale;
        focusRect.SetAsLastSibling();

        blackoutFocusText.font = uiText.font;
        blackoutFocusText.fontSharedMaterial = uiText.fontSharedMaterial;
        blackoutFocusText.fontSize = uiText.fontSize;
        blackoutFocusText.enableAutoSizing = uiText.enableAutoSizing;
        blackoutFocusText.fontSizeMin = uiText.fontSizeMin;
        blackoutFocusText.fontSizeMax = uiText.fontSizeMax;
        blackoutFocusText.alignment = uiText.alignment;
        blackoutFocusText.overflowMode = uiText.overflowMode;
        blackoutFocusText.textWrappingMode = uiText.textWrappingMode;
        blackoutFocusText.wordWrappingRatios = uiText.wordWrappingRatios;
        blackoutFocusText.margin = uiText.margin;
        blackoutFocusText.lineSpacing = uiText.lineSpacing;
        blackoutFocusText.characterSpacing = uiText.characterSpacing;
        blackoutFocusText.wordSpacing = uiText.wordSpacing;
        blackoutFocusText.paragraphSpacing = uiText.paragraphSpacing;
        blackoutFocusText.richText = uiText.richText;
        blackoutFocusText.raycastTarget = false;
        blackoutFocusText.color = Color.white;
        blackoutFocusText.text = string.Empty;
        blackoutFocusText.enabled = false;
    }

    private void ApplyToBlackoutFocusCharacter(TMP_TextInfo textInfo, int charIndex)
    {
        if (charIndex < 0 || charIndex >= textInfo.characterCount) return;

        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;
        Color32[] colors = textInfo.meshInfo[materialIndex].colors32;
        // --// Tie the highlight alpha to the blackout strength so the word fades out with the room.
        byte alpha = (byte)(255 * Mathf.Clamp01(currentBlackoutStrength));

        for (int i = 0; i < 4; i++)
        {
            colors[vertexIndex + i] = new Color32(255, 255, 255, alpha);
        }
    }

    private void ResetBlackoutFocusTimer()
    {
        if (activeEffect == DialogueTextEffect.BlackoutFocus)
        {
            // --// Keep the room dark right away, but do not start counting the hold until the target text is actually visible.
            blackoutStartedAt = -1f;
            currentBlackoutStrength = 1f;
        }
        else
        {
            blackoutStartedAt = -1f;
            currentBlackoutStrength = 0f;
        }
    }

    private void UpdateBlackoutFocusTimer(int visibleCharacterCount)
    {
        if (activeEffect != DialogueTextEffect.BlackoutFocus) return;
        if (blackoutStartedAt >= 0f) return;

        // --// Start the hold only after the focused word or phrase has finished typing in.
        if (visibleCharacterCount >= GetBlackoutReadableEndIndex())
        {
            blackoutStartedAt = Time.time;
        }
    }

    private float GetBlackoutStrength()
    {
        if (blackoutStartedAt < 0f)
        {
            // --// Stay fully dark while the target text is still typing in.
            return 1f;
        }

        float elapsed = Time.time - blackoutStartedAt;
        float effectiveHoldDuration = GetBlackoutHoldDuration();

        if (elapsed <= effectiveHoldDuration)
        {
            return 1f;
        }

        if (blackoutFadeDuration <= 0f)
        {
            return 0f;
        }

        float fadeProgress = (elapsed - effectiveHoldDuration) / blackoutFadeDuration;
        return 1f - Mathf.Clamp01(fadeProgress);
    }

    private bool ShouldFlickerBlackout()
    {
        if (blackoutStartedAt < 0f) return true;
        if (blackoutIntroFlickerDuration <= 0f) return false;

        return (Time.time - blackoutStartedAt) <= blackoutIntroFlickerDuration;
    }

    private int GetBlackoutReadableEndIndex()
    {
        // --// Finds where the highlighted target ends so the hold does not begin too early.
        if (dialogueText == null) return 0;

        string sourceText = dialogueText.text;
        if (string.IsNullOrEmpty(sourceText)) return 0;

        if (string.IsNullOrEmpty(activeTargetText))
        {
            return sourceText.Length;
        }

        int matchIndex = sourceText.IndexOf(activeTargetText, System.StringComparison.OrdinalIgnoreCase);
        if (matchIndex < 0)
        {
            return sourceText.Length;
        }

        return matchIndex + activeTargetText.Length;
    }

    private float GetBlackoutHoldDuration()
    {
        int readableCharacters = 0;

        if (string.IsNullOrEmpty(activeTargetText))
        {
            if (dialogueText != null && !string.IsNullOrEmpty(dialogueText.text))
            {
                readableCharacters = dialogueText.text.Length;
            }
        }
        else
        {
            foreach (char c in activeTargetText)
            {
                if (!char.IsWhiteSpace(c))
                {
                    readableCharacters++;
                }
            }
        }

        // --// Longer target text gets a slightly longer hold so the word or phrase can actually be read.
        float scaledHold = blackoutHoldDuration + (readableCharacters * blackoutHoldPerCharacter);
        return Mathf.Clamp(scaledHold, blackoutHoldDuration, blackoutMaxHoldDuration);
    }

    private void HideBlackoutFocus()
    {
        currentBlackoutStrength = 0f;

        if (blackoutOverlay != null)
        {
            blackoutOverlay.enabled = false;
            blackoutOverlay.color = new Color(0f, 0f, 0f, 0f);
        }

        if (blackoutFocusText != null)
        {
            blackoutFocusText.enabled = false;
        }
    }
}
