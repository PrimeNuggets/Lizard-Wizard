using System;
using TMPro;
using UnityEngine;

// --// Effect math references:
// --// Madalaski TMP vertex animation tutorial: https://www.youtube.com/watch?v=FgWVW2PL1bQ
// --// Unity Matrix4x4 docs: https://docs.unity.cn/560/Documentation/ScriptReference/Matrix4x4.html
// --// Unity Quaternion.Euler docs: https://docs.unity3d.com/ru/2018.4/ScriptReference/Quaternion.Euler.html
// --// TMP vertex animation threads:
// --// https://www.reddit.com/r/Unity3D/comments/1rkj6pu/what_are_the_possible_ways_to_animate_text_like/
public partial class HorrorTextAnimator
{
    private Vector3 GetOffset(int charIndex)
    {
        // --// Position-based effects all route through this one method so the main file stays cleaner.
        if (activeEffect == DialogueTextEffect.Jitter)
        {
            return new Vector3(
                UnityEngine.Random.Range(-0.25f, 0.25f),
                UnityEngine.Random.Range(-0.25f, 0.25f),
                0f
            ) * jitterAmount;
        }

        if (activeEffect == DialogueTextEffect.Wobble)
        {
            return new Vector3(
                0f,
                Mathf.Sin((Time.time + charIndex) * wobbleSpeed) * wobbleHeight,
                0f
            );
        }

        if (activeEffect == DialogueTextEffect.Sway)
        {
            return new Vector3(
                Mathf.Sin((Time.time + (charIndex * 0.15f)) * swaySpeed) * swayDistance,
                0f,
                0f
            );
        }

        if (activeEffect == DialogueTextEffect.Sink)
        {
            return new Vector3(
                0f,
                -Mathf.Abs(Mathf.Sin((Time.time + (charIndex * 0.12f)) * sinkSpeed)) * sinkDepth,
                0f
            );
        }

        if (activeEffect == DialogueTextEffect.GlitchJump)
        {
            return GetGlitchJumpOffset(charIndex);
        }

        return Vector3.zero;
    }

    private float GetRotation(int charIndex)
    {
        // --// Rotate swings each letter around its midpoint for a more unstable horror read.
        if (activeEffect == DialogueTextEffect.Rotate)
        {
            return Mathf.Sin((Time.time + (charIndex * 0.1f)) * rotationSpeed) * rotationAngle;
        }

        return 0f;
    }

    private float GetScale(int charIndex)
    {
        // --// Pulse scales the character quad uniformly so the word looks like it is throbbing.
        if (activeEffect == DialogueTextEffect.Pulse)
        {
            float pulseWave = (Mathf.Sin((Time.time + (charIndex * 0.12f)) * pulseSpeed) + 1f) * 0.5f;
            return 1f + (pulseWave * pulseScaleAmount);
        }

        return 1f;
    }

    private void ApplyTransform(Vector3[] verts, int vertexIndex, Vector3 charMidPoint, Vector3 offset, float rotationZ, float uniformScale)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(
            offset,
            Quaternion.Euler(0f, 0f, rotationZ),
            new Vector3(uniformScale, uniformScale, 1f)
        );

        // --// Shift to origin, apply the transform, then move the quad back in place.
        verts[vertexIndex + 0] = matrix.MultiplyPoint3x4(verts[vertexIndex + 0] - charMidPoint) + charMidPoint;
        verts[vertexIndex + 1] = matrix.MultiplyPoint3x4(verts[vertexIndex + 1] - charMidPoint) + charMidPoint;
        verts[vertexIndex + 2] = matrix.MultiplyPoint3x4(verts[vertexIndex + 2] - charMidPoint) + charMidPoint;
        verts[vertexIndex + 3] = matrix.MultiplyPoint3x4(verts[vertexIndex + 3] - charMidPoint) + charMidPoint;
    }

    private void ApplyColor(Color32[] colors, int vertexIndex, int charIndex)
    {
        // --// Static tint, opacity, flicker, and color-pulse all stack here so color logic stays in one place.
        float alphaMultiplier = useCustomOpacity ? activeOpacityMultiplier : 1f;

        if (activeEffect == DialogueTextEffect.Flicker)
        {
            alphaMultiplier *= GetFlickerAlpha(charIndex);
        }

        Color tintColor = activeTint;

        if (activeEffect == DialogueTextEffect.ColorPulse)
        {
            tintColor = GetColorPulseColor(charIndex);
        }

        for (int i = 0; i < 4; i++)
        {
            Color32 currentColor = colors[vertexIndex + i];

            if (useTextTint || activeEffect == DialogueTextEffect.ColorPulse)
            {
                currentColor.r = (byte)(Mathf.Clamp01(tintColor.r) * 255);
                currentColor.g = (byte)(Mathf.Clamp01(tintColor.g) * 255);
                currentColor.b = (byte)(Mathf.Clamp01(tintColor.b) * 255);
            }

            currentColor.a = (byte)(currentColor.a * Mathf.Clamp01(alphaMultiplier));
            colors[vertexIndex + i] = currentColor;
        }
    }

    private float GetFlickerAlpha(int charIndex)
    {
        // --// Perlin noise gives the opacity change a less mechanical feel than a simple sine wave.
        float noise = Mathf.PerlinNoise((charIndex * 0.37f) + 0.5f, Time.time * flickerSpeed);
        return Mathf.Lerp(flickerMinimumOpacity, 1f, noise);
    }

    private Color GetColorPulseColor(int charIndex)
    {
        // --// Pulse between white and the target tint so the word feels alive without becoming unreadable.
        Color pulseTarget = useTextTint ? activeTint : colorPulseTint;
        float pulseWave = (Mathf.Sin((Time.time + (charIndex * 0.12f)) * colorPulseSpeed) + 1f) * 0.5f;
        return Color.Lerp(Color.white, pulseTarget, pulseWave);
    }

    private void EnsureGlitchBuffers(int characterCount)
    {
        // --// GlitchJump keeps a small cached offset per character so the jumps feel discrete instead of smoothing out.
        if (glitchOffsets.Length == characterCount) return;

        Array.Resize(ref glitchOffsets, characterCount);
        Array.Resize(ref glitchRefreshTimes, characterCount);
    }

    private Vector3 GetGlitchJumpOffset(int charIndex)
    {
        if (charIndex < 0 || charIndex >= glitchOffsets.Length)
        {
            return Vector3.zero;
        }

        // --// Refresh the random jump on a timer so the letters snap, hold for a beat, then snap again.
        if (Time.time >= glitchRefreshTimes[charIndex])
        {
            glitchOffsets[charIndex] = new Vector3(
                UnityEngine.Random.Range(-0.25f, 0.25f),
                UnityEngine.Random.Range(-0.25f, 0.25f),
                0f
            ) * glitchJumpAmount;

            glitchRefreshTimes[charIndex] = Time.time + glitchJumpHoldTime + UnityEngine.Random.Range(0f, glitchJumpHoldTime);
        }

        return glitchOffsets[charIndex];
    }
}
