using System;
using TMPro;
using UnityEngine;

// --// Mesh copy / restore references:
// --// CopyMeshInfoVertexData: https://docs.unity3d.com/ja/Packages/com.unity.textmeshpro%403.0/api/TMPro.TMP_TextInfo.CopyMeshInfoVertexData.html
// --// UpdateVertexData: https://docs.unity3d.com/ja/Packages/com.unity.textmeshpro%403.0/api/TMPro.TMP_Text.UpdateVertexData.html
// --// Reddit threads used while sorting out the typewriter / vertex reset conflict:
// --// https://www.reddit.com/r/Unity3D/comments/12wirdg
// --// https://www.reddit.com/r/Unity3D/comments/1fdo4jk
// --// TMP mesh reset threads:
// --// https://www.reddit.com/r/Unity3D/comments/10r9ir8/textmeshpro_how_to_persist_changes/
// --// https://www.reddit.com/r/Unity3D/comments/1doiox5/textmeshpro_character_vertex_colors_being_reset/
public partial class HorrorTextAnimator
{
    private void RestoreBaseMesh(TMP_TextInfo textInfo, TMP_MeshInfo[] baseMeshInfo)
    {
        // --// Copy the untouched vertices and colors back into TMP before the new frame's effect pass starts.
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            if (textInfo.meshInfo[i].vertices.Length == baseMeshInfo[i].vertices.Length)
            {
                Array.Copy(baseMeshInfo[i].vertices, textInfo.meshInfo[i].vertices, baseMeshInfo[i].vertices.Length);
            }

            if (textInfo.meshInfo[i].colors32.Length == baseMeshInfo[i].colors32.Length)
            {
                Array.Copy(baseMeshInfo[i].colors32, textInfo.meshInfo[i].colors32, baseMeshInfo[i].colors32.Length);
            }
        }
    }

    private void HideCharactersPastLimit(TMP_TextInfo textInfo, int visibleCharacterCount)
    {
        // --// The typewriter still relies on maxVisibleCharacters, so anything past that index gets hidden here.
        for (int i = visibleCharacterCount; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

            colors[vertexIndex + 0].a = 0;
            colors[vertexIndex + 1].a = 0;
            colors[vertexIndex + 2].a = 0;
            colors[vertexIndex + 3].a = 0;
        }
    }

    private void HideAllCharacters(TMP_TextInfo textInfo)
    {
        // --// Used by duplicate layers like GhostTrail / BlackoutFocus so only the targeted text gets drawn there.
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

            colors[vertexIndex + 0].a = 0;
            colors[vertexIndex + 1].a = 0;
            colors[vertexIndex + 2].a = 0;
            colors[vertexIndex + 3].a = 0;
        }
    }
}
