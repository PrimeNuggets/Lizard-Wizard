using TMPro;
using UnityEngine;

// --// Ghost layer uses the same TMP mesh update path referenced here:
// --// CopyMeshInfoVertexData: https://docs.unity3d.com/ja/Packages/com.unity.textmeshpro%403.0/api/TMPro.TMP_TextInfo.CopyMeshInfoVertexData.html
// --// UpdateVertexData: https://docs.unity3d.com/ja/Packages/com.unity.textmeshpro%403.0/api/TMPro.TMP_Text.UpdateVertexData.html
// --// Visibility timing stays in sync with TMP maxVisibleCharacters:
// --// https://docs.unity3d.com/ja/Packages/com.unity.textmeshpro%403.0/api/TMPro.TMP_Text.maxVisibleCharacters.html
public partial class HorrorTextAnimator
{
    private void UpdateGhostTrail(int visibleCharacterCount)
    {
        EnsureGhostTrail();
        if (ghostText == null) return;

        ghostText.enabled = true;
        ghostText.text = dialogueText.text;
        ghostText.maxVisibleCharacters = visibleCharacterCount;
        ghostText.ForceMeshUpdate();

        TMP_TextInfo ghostTextInfo = ghostText.textInfo;
        if (ghostTextInfo.characterCount == 0) return;

        TMP_MeshInfo[] baseMeshInfo = ghostTextInfo.CopyMeshInfoVertexData();
        RestoreBaseMesh(ghostTextInfo, baseMeshInfo);
        HideAllCharacters(ghostTextInfo);

        ApplyToTargetText(ghostText.text, ghostTextInfo, visibleCharacterCount, ApplyToGhostCharacter);

        ghostText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        ghostText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void EnsureGhostTrail()
    {
        if (ghostText != null) return;

        TextMeshProUGUI uiText = dialogueText as TextMeshProUGUI;
        if (uiText == null) return;

        GameObject ghostObject = new GameObject("DialogueText_GhostTrail");
        ghostObject.layer = uiText.gameObject.layer;
        RectTransform ghostRect = ghostObject.AddComponent<RectTransform>();
        ghostObject.AddComponent<CanvasRenderer>();
        ghostText = ghostObject.AddComponent<TextMeshProUGUI>();

        ghostRect.SetParent(uiText.transform.parent, false);
        ghostRect.anchorMin = uiText.rectTransform.anchorMin;
        ghostRect.anchorMax = uiText.rectTransform.anchorMax;
        ghostRect.pivot = uiText.rectTransform.pivot;
        ghostRect.sizeDelta = uiText.rectTransform.sizeDelta;
        ghostRect.anchoredPosition = uiText.rectTransform.anchoredPosition;
        ghostRect.localRotation = uiText.rectTransform.localRotation;
        ghostRect.localScale = uiText.rectTransform.localScale;
        ghostRect.SetSiblingIndex(uiText.transform.GetSiblingIndex());

        ghostText.font = uiText.font;
        ghostText.fontSharedMaterial = uiText.fontSharedMaterial;
        ghostText.fontSize = uiText.fontSize;
        ghostText.enableAutoSizing = uiText.enableAutoSizing;
        ghostText.fontSizeMin = uiText.fontSizeMin;
        ghostText.fontSizeMax = uiText.fontSizeMax;
        ghostText.alignment = uiText.alignment;
        ghostText.overflowMode = uiText.overflowMode;
        ghostText.textWrappingMode = uiText.textWrappingMode;
        ghostText.wordWrappingRatios = uiText.wordWrappingRatios;
        ghostText.margin = uiText.margin;
        ghostText.lineSpacing = uiText.lineSpacing;
        ghostText.characterSpacing = uiText.characterSpacing;
        ghostText.wordSpacing = uiText.wordSpacing;
        ghostText.paragraphSpacing = uiText.paragraphSpacing;
        ghostText.richText = uiText.richText;
        ghostText.raycastTarget = false;
        ghostText.text = string.Empty;
        ghostText.enabled = false;
    }

    private void ApplyToGhostCharacter(TMP_TextInfo textInfo, int charIndex)
    {
        if (charIndex < 0 || charIndex >= textInfo.characterCount) return;

        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        Vector3[] verts = textInfo.meshInfo[materialIndex].vertices;
        Color32[] colors = textInfo.meshInfo[materialIndex].colors32;
        Vector3 charMidPoint = (verts[vertexIndex + 0] + verts[vertexIndex + 2]) * 0.5f;

        Vector3 ghostOffset = new Vector3(
            ghostTrailOffset.x + Mathf.Sin((Time.time + (charIndex * 0.17f)) * ghostTrailDriftSpeed) * ghostTrailDriftAmount,
            ghostTrailOffset.y + Mathf.Cos((Time.time + (charIndex * 0.13f)) * ghostTrailDriftSpeed) * (ghostTrailDriftAmount * 0.5f),
            0f
        );

        ApplyTransform(verts, vertexIndex, charMidPoint, ghostOffset, 0f, 1f);
        ApplyGhostColor(colors, vertexIndex);
    }

    private void ApplyGhostColor(Color32[] colors, int vertexIndex)
    {
        Color ghostColor = ghostTrailColor;

        for (int i = 0; i < 4; i++)
        {
            colors[vertexIndex + i] = new Color(
                ghostColor.r,
                ghostColor.g,
                ghostColor.b,
                ghostColor.a
            );
        }
    }

    private void HideGhostTrail()
    {
        if (ghostText != null)
        {
            ghostText.enabled = false;
        }
    }
}
