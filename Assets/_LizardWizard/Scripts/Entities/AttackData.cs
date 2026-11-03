using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Scriptable Objects/Attack Data")]
public class AttackData : ScriptableObject
{
    public string attackId;
    public string attackName;
    public AnimationClip anim;
    public float power;
    [Tooltip("Negative values fling the target forward")] public float knockback;
    public float range;
    [Tooltip("The amount of time the attacker is paused for after landing a hit")] public float hitTime;
    [Tooltip("The amount of time the target is stunned for after the hit")] public float pauseTime;
    public Color inflictColor = Color.red;
    public GameObject[] projectiles;
    //public GameObject vfx;
}
