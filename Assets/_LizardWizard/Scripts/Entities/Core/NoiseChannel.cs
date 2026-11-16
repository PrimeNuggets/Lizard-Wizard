using System;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "NoiseChannel", menuName = "Scriptable Objects/Noise Channel")]
public class NoiseChannel : ScriptableObject
{
    [NonSerialized]
    public UnityEvent<Vector3, Aspect, float> heard =
        new UnityEvent<Vector3, Aspect, float>();

    public void Raise(Vector3 pos, Aspect src, float strength = 1f)
    {
        heard.Invoke(pos, src, Mathf.Max(0f, strength));
    }
}