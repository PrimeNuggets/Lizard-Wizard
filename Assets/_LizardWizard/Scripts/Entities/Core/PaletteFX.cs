using System;
using UnityEngine;

[Serializable]
public class PaletteFX
{
    public Color color;
    [Tooltip("Applies the selected color for this many frames"), Min(0f)] public float frames;
    public bool fadeOut;
    public bool blink = false;
    [Tooltip("How many flashes per second")]
    public float blinksPerSecond = 6f;
}
