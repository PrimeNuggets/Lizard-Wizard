using System;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "NoiseChannel", menuName = "Scriptable Objects/Noise Channel")]
public class NoiseChannel : ScriptableObject
{
    [NonSerialized] public UnityEvent<Vector3, Aspect> heard = new UnityEvent<Vector3, Aspect>();
    public void Raise(Vector3 pos, Aspect src)
    {
        heard.Invoke(pos, src);
    }
}
