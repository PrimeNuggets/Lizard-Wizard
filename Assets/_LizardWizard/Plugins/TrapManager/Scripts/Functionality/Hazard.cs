using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Hazard", menuName = "Hazard Manager/Hazard")]
public class Hazard : ScriptableObject
{
    //===============================================================
    // Enums / Sub-Classes
    //===============================================================
    public enum TrapType {
        None,
        Spikes,
        Pitfall,
        Arrow,
        Fire,
        PoisonFloor,
        HazardousGas,
        ThornyBramble,
        HealSpot,
        Fountain,
        MedicinalHerbPatch
    }
    public enum DamageType {
        None,
        Instant,
        DoT,
        Delayed,
        Summon
    }

    [Serializable]
    public class HazardSummon
    {
        [Tooltip("Prefab to spawn when this hazard is triggered.")]
        public GameObject objectToSummon;
        [Tooltip("Local offset from the hazard instance where the summon should appear.")]
        public Vector3 summonPosition;
        [Tooltip("Local rotation offset for the spawned object. Leave at 0,0,0,0 to use the prefab rotation.")]
        public Quaternion summonRotation;
    }

    //===============================================================
    // Variables
    //===============================================================
    [Header("General")]
    public TrapType type;
    [Tooltip("The base damage value for this hazard. Set to negative to heal.")]
    public float baseDamage = 0f;
    [Tooltip("The type of damage this hazard inflicts: Instant (on collision), Damage over Time (DoT), Delayed, or Summon.")]
    public DamageType damageType;
    [Tooltip("Time in seconds before the hazard despawns. 0 means it never despawns."), Min(0f)]
    public float despawnTime = 0f;
    [Tooltip("Time in seconds before the hazard begins to deal damage."), Min(1f)]

    [Header("Delay Parameters")]
    public float delayTime = 1f;

    [Header("DoT Parameters")]
    [Tooltip("Damage of x (Time in seconds)."), Min(1f)]
    public float damageInterval = 1f;
    
    [Header("Summon Parameters")]
    public HazardSummon summonObject;

}
