using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackCollider : MonoBehaviour
{
    private Aspect entity;
    private Entity entityData;
    private bool hitOver = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        entity = GetComponentInParent<Aspect>();
        switch (entity.aspectName)
        {
            case Aspect.aspect.Enemy:
                entityData = entity.GetComponent<EnemyAI>().entityData;
                break;
            case Aspect.aspect.NPC:
                entityData = entity.GetComponent<NPC>().entityData;
                break;
            case Aspect.aspect.Player:
                entityData = entity.GetComponent<Player>().entityData;
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnEnable()
    {
        hitOver = false;
    }
    void OnDisable()
    {
        hitOver = true;
    }

    void ApplyHit(Entity otherData)
    {
        //=========================================
        // Initialization
        //=========================================
        if (entity.aspectName != Aspect.aspect.Enemy || otherData == null || hitOver || otherData == entityData) return;
        var attack = entity.GetComponent<EnemyAI>().currentAttack;
        if (attack == null) return;

        //=========================================
        // Palette
        //=========================================
        PaletteReciever reciever = otherData.entObj.GetComponent<PaletteReciever>();

        if (reciever != null) reciever.Inflict(attack.palFX);

        //=========================================
        // Damage
        //=========================================
        float damage = attack.power;
        //if (moveGuarded && !attack.flags.Contains("unguardable")) damage *= AttackData.GUARD_DAMAGE_REDUCTION;
        var targetCurrentWater = otherData.water.GetCurrent();
        var targetCurrentHealth = otherData.health.GetCurrent();
        float remainingDamage = damage;
        //Damage to Water
        if (targetCurrentWater > 0f)
        {
            remainingDamage -= targetCurrentWater;
            otherData.water.Set(
                targetCurrentWater -
                damage
            );
        }
        otherData.water.Set(
            targetCurrentWater -
            attack.drainWater
        );
        if (targetCurrentWater <= 0f && remainingDamage > 0f) //Separated from an if else statement to still run if 
        {
            otherData.health.Set(
                targetCurrentHealth -
                remainingDamage
            );
            remainingDamage -= targetCurrentHealth;
        }
        //=========================================
        // Knockback
        //=========================================
        if (!attack.flags.Contains("no_flinch")) {
            Transform attacker = entity.transform;

            Vector3 forward = Vector3.ProjectOnPlane(attacker.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;

            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector3 knockback = (
                right * attack.knockback.x +
                Vector3.up * attack.knockback.y + 
                forward * attack.knockback.z
            );
            if (otherData.grounded && knockback.y > 0f) {
                otherData.SetStateType('A', false);
            } else if (!otherData.grounded)
            {
                otherData.SetStateType(otherData.GetStateType(), false);
            }
            otherData.BeginHurt(attack.pauseTime, knockback, attack.recoverTime);
            //otherData.velocity += ; //Apply after pausetime
        }

        entityData.BeginHitTime(attack.hitTime);

        //=========================================
        // End of Hit
        //=========================================
        if (attack.type == AttackData.AttackType.OneHit) hitOver = true;
    }

    void OnTriggerEnter(Collider other)
    {
        Aspect entity = other.GetComponentInParent<Aspect>();
        if (entity == null) return;

        Entity otherData = null;
        switch (entity.aspectName)
        {
            case Aspect.aspect.Enemy:
                otherData = other.GetComponent<EnemyAI>().entityData;
                break;
            case Aspect.aspect.NPC:
                otherData = other.GetComponent<NPC>().entityData;
                break;
            case Aspect.aspect.Player:
                otherData = other.GetComponent<Player>().entityData;
                break;
        }
        ApplyHit(otherData);
    }
}
