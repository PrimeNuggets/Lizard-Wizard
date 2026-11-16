using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PaletteReciever : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;

    private readonly List<Material> materials = new();
    private readonly List<Color> originalColors = new();
    private readonly List<Material[]> originalAssignments = new();

    private Coroutine sequence;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        foreach (var render in renderers)
        {
            if (render == null)
            {
                originalAssignments.Add(null);
                continue;
            }

            originalAssignments.Add(render.sharedMaterials);

            Material[] copies = render.sharedMaterials;

            for (int i = 0; i < copies.Length; i++)
            {
                if (copies[i] == null) continue;

                copies[i] = new Material(copies[i]);
                materials.Add(copies[i]);
                originalColors.Add(copies[i].color);
            }

            render.sharedMaterials = copies;
        }
    }

    public void Inflict(AttackData.PalFX[] palette)
    {
        if (!isActiveAndEnabled || palette == null || palette.Length == 0) return;

        if (sequence != null)
        {
            StopCoroutine(sequence);
            sequence = null;
        }

        RestoreColors();
        sequence = StartCoroutine(PlayPalette(palette));
    }

    private IEnumerator PlayPalette(AttackData.PalFX[] palette)
    {
        yield return null;

        foreach (var entry in palette)
        {
            if (entry == null || entry.frames <= 0f) continue;

            float duration = entry.frames / 60f;
            float elapsed = 0f;

            ApplyColor(entry.color, 0f);
            while (elapsed < duration)
            {
                yield return null;

                elapsed += Time.deltaTime;
                float fade = entry.fadeOut ? Mathf.Clamp01(elapsed / duration) : 0f;
                ApplyColor(entry.color, fade);
            }
        }

        RestoreColors();
        sequence = null;
    }

    private void ApplyColor(Color color, float fade)
    {
        for (int i = 0; i < materials.Count; i++)
        {
            if (materials[i] == null) continue;

            Color tint = color;
            tint.a = originalColors[i].a;

            materials[i].color = Color.Lerp(tint, originalColors[i], fade);
        }
    }
    private void RestoreColors()
    {
        for (int i = 0; i < materials.Count; i++)
        {
            if (materials[i] != null)
            {
                materials[i].color = originalColors[i];
            }
        }
    }
    void OnDisable()
    {
        if (sequence != null)
        {
            StopCoroutine(sequence);
            sequence = null;
        }
        RestoreColors();
    }
    void OnDestroy()
    {
        for (int i = 0; i < originalAssignments.Count; i++)
        {
            if (renderers[i] == null || originalAssignments[i] == null) continue;

            renderers[i].sharedMaterials = originalAssignments[i];
        }
        foreach (var material in materials)
        {
            if (material != null) Destroy(material);
        }
    }
}
