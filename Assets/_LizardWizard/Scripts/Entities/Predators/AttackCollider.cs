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
        FindData();
    }

    //-- EnemyAI might not have made its data yet on Start, so this gets called again on hit
    void FindData()
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

    void ApplyHit(Entity otherData, GameObject enemy)
    {
        //=========================================
        // Initialization
        //=========================================
        if (entity.aspectName != Aspect.aspect.Enemy || otherData == null || hitOver || otherData == entityData) return;
        var attack = entity.GetComponent<EnemyAI>().currentAttack;
        Aspect.aspect enemyType = enemy.GetComponent<Aspect>().aspectName;
        if (attack == null) return;
        //Debug.Log("Applying hit: " + attack.name);

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
        float absorbedDamage = Mathf.Min(otherData.water.GetCurrent(), damage);
        float remainingDamage = damage - absorbedDamage;
        //Damage to Water
        otherData.water.Set(
            otherData.water.GetCurrent() -
            absorbedDamage
        );
        otherData.health.Set(
            otherData.health.GetCurrent() -
            remainingDamage
        );

        otherData.water.Set(
            otherData.water.GetCurrent() -
            attack.drainWater
        );
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
        // Entity Specific
        //=========================================
        switch (enemyType)
        {
            case Aspect.aspect.Enemy:
                break;
            case Aspect.aspect.NPC:
                break;
            case Aspect.aspect.Player:
                var waterLevel = enemy.GetComponent<WaterLevelPercentScore>();
                waterLevel.DisplayGraphic(attack.recoverTime);
                break;
        }

        //=========================================
        // End of Hit
        //=========================================
        if (attack.type == AttackData.AttackType.OneHit) hitOver = true;
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Hit: " + other.GameObject().name);
        Aspect entity = other.GetComponentInParent<Aspect>();
        if (entity == null) return;
        var gameObj = entity.GameObject();
        //Debug.Log(gameObj.name + " is an entity");

        Entity otherData = null;
        switch (entity.aspectName)
        {
            case Aspect.aspect.Enemy:
                otherData = gameObj.GetComponent<EnemyAI>().entityData;
                //Debug.Log(gameObj.name + " is an enemy");
                break;
            case Aspect.aspect.NPC:
                otherData = gameObj.GetComponent<NPC>().entityData;
                //Debug.Log(gameObj.name + " is an NPC");
                break;
            case Aspect.aspect.Player:
                otherData = gameObj.GetComponent<Player>().entityData;
                //Debug.Log(gameObj.name + " is a player");
                break;
        }
        if (entityData == null) FindData();
        if (otherData == null || otherData == entityData) return;
        if (otherData.invulnerable)
        {
            //Debug.Log(gameObj.name + " is currently invulnerable");
            return;
        }
        ApplyHit(otherData, gameObj);
    }
}
