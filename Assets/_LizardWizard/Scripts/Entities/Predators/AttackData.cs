using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Scriptable Objects/Attack Data")]
public class AttackData : ScriptableObject
{
    [NonSerialized] public const float GUARD_DAMAGE_REDUCTION = 0.75f; //multiplier to power if the attack was guarded.
    public enum AttackType
    {
        OneHit,
        Continuous,
    }
    [Serializable]
    public class PalFX
    {
        public Color color;
        [Tooltip("Applies the selected color for this many frames"), Min(0f)] public float frames;
        public bool fadeOut;
    }
    public string attackId;
    public string attackName;
    public AttackType type;
    public AnimationClip anim;
    [Min(0f)] public float cooldown;
    [Range(-180f, 180f)] public float facingOffset = 0f;
    public float power;
    [Tooltip("X flings the target left/right of the attacker\nY flings the target vertically from the attacker\nZ flings the target towards/away from the attacker")] public Vector3 knockback;
    public float range = 5f;
    [Tooltip("The amount of time in frames the attacker is paused for after landing a hit")] public int hitTime = 5;
    [Tooltip("The amount of time in frames the target is stunned for after the hit")] public int pauseTime = 10;
    [Tooltip("Frames after knockback is applied for the target to be able to recover control")] public int recoverTime = 12;
    [Tooltip("Negative value gives the target water.")] public float drainWater = 0f; //Exists in case draining water is the only desired effect or to deal modify damage while having water, applies after damage calculation
    [LabelOverride("Inflict Palette")] public PalFX[] palFX;
    //public GameObject[] projectiles;
    [Tooltip("Additional properties to add to the attack, such as bypassing guard or defining an element type")] public string[] flags;
    //public GameObject vfx;
}
