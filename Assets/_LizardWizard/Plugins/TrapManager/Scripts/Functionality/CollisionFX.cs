using UnityEngine;

[CreateAssetMenu(fileName = "CollisionFX", menuName = "CollisionFX")]
public class CollisionEffects : ScriptableObject
{
    public enum EffectType
    {
        None,
        Spark,
        Explosion,
        PoisonCloud,
        FireBurst,
        HealEffect
    }
    public enum DamageType {
        None,
        Instant,
        DoT
    }
    [Tooltip("Type of visual/audio effect to play on collision.")]
    public EffectType effectType;
    [Tooltip("Damage dealt by this effect. Negative values heal.")]
    public float damage = 0f;
    [Tooltip("How damage is applied: Instant (on collision) or DoT (Damage over time).")]
    public DamageType damageType;
    [Tooltip("Time between damage ticks for DoT effects.")]
    public float dotInterval = 1f;
    [Tooltip("How long the effect lasts before disappearing.")]
    public float collisionLife = 1f;
}
