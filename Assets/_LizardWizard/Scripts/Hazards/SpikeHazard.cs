using UnityEngine;

//-- rework of xaviers spikes from his Heal-N-Hazards kit, his were 2d only so they couldnt hit the lizard
//-- still uses his Hazard asset for the damage numbers. goes on the cacti with a trigger collider
[RequireComponent(typeof(Collider))]
public class SpikeHazard : MonoBehaviour
{
    [Tooltip("Hazard asset from xaviers trap kit. Uses baseDamage, and damageInterval if set to DoT")]
    [SerializeField] private Hazard hazard;

    [Header("Knockback")]
    [SerializeField] private float pushBack = 8f;
    [SerializeField] private float pushUp = 4f;
    [Tooltip("Frames the lizard freezes when hit (60 = 1 sec)")]
    [SerializeField] private int stunFrames = 5;
    [Tooltip("Frames before the lizard can move again (60 = 1 sec)")]
    [SerializeField] private int recoverFrames = 15;

    [Tooltip("Seconds before this cactus can hurt again")]
    [SerializeField] private float hitCooldown = 1f;
    [SerializeField] private PaletteFX[] palFXs =
    {
        new PaletteFX
        {
            color = new Color(0.9f, 0.05f, 0.05f, 1f),
            frames = 10f,
            blink = true,
            blinksPerSecond = 6f
        }
    };

    private float nextHitTime;

    void OnTriggerStay(Collider other)
    {
        //-- only the lizards body counts, not its attack hitboxes
        if (other is not CharacterController || Time.time < nextHitTime) return;
        if (!other.TryGetComponent(out Player player) || player.entityData == null) return;

        Entity target = player.entityData;
        if (target.invulnerable) return;

        nextHitTime = Time.time + Cooldown();
        Hurt(target, player.gameObject);
    }

    float Cooldown()
    {
        if (hazard != null && hazard.damageType == Hazard.DamageType.DoT) return hazard.damageInterval;
        return hitCooldown;
    }

    void Hurt(Entity target, GameObject lizard)
    {
        //-- water soaks the damage first, same as enemy attacks
        float damage = hazard != null ? Mathf.Max(0f, hazard.baseDamage) : 0f;
        float soaked = Mathf.Min(target.water.GetCurrent(), damage);
        target.water.Set(target.water.GetCurrent() - soaked);
        target.health.Set(target.health.GetCurrent() - (damage - soaked));

        //-- push away from the center
        Vector3 away = lizard.transform.position - transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = -lizard.transform.forward;
        away.Normalize();

        //-- stop the speed into the cactus so the knockback actually works
        target.momentum -= away * Mathf.Min(0f, Vector3.Dot(target.momentum, away));
        target.storedMomentum -= away * Mathf.Min(0f, Vector3.Dot(target.storedMomentum, away));

        Vector3 knockback = away * pushBack + Vector3.up * pushUp;

        if (target.grounded && knockback.y > 0f) target.SetStateType('A', false);
        target.BeginHurt(stunFrames, knockback, recoverFrames);

        if (damage > 0f && lizard.TryGetComponent(out PaletteReciever reciever))
        {
            reciever.Inflict(palFXs);
        }
        if (lizard.TryGetComponent(out WaterLevelPercentScore waterLevel)) waterLevel.DisplayGraphic(recoverFrames);
    }
}
