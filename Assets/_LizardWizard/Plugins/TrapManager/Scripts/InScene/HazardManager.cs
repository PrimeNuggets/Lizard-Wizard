using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HazardManager : MonoBehaviour
{
    public static event Action<HazardManager, Collider2D> PitfallTriggered;

    //=======================================
    // Programmer-Accessible Variables
    //=======================================
    private float despawnTimer;
    [NonSerialized] public float nextDamageTime; //For DoT
    Collider2D hazardCollider;
    //=======================================
    // Designer-Accessible Variables
    //=======================================
    [Tooltip("The Hazard ScriptableObject defining this trap's behavior.")]
    public Hazard hazardData;
    [SerializeField, Tooltip("The point where projectiles are summoned. Only works with hazard type: Arrow")] Transform summonPoint;
    
    void Awake()
    {
        hazardCollider = GetComponent<Collider2D>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        despawnTimer = hazardData != null ? hazardData.despawnTime : 0f;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (hazardData == null || hazardData.despawnTime <= 0f)
        {
            return;
        }

        despawnTimer -= Time.fixedDeltaTime;
        if (despawnTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Debug.Log("Despawning " + gameObject.name + " in: " + despawnTimer.ToString("F2"));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hazardData == null)
        {
            return;
        }

        HazardManager otherHazard = other.GetComponent<HazardManager>();
        if (otherHazard != null && otherHazard != this && GetEntityId() < otherHazard.GetEntityId())
        {
            if (InteractionsManager.TryResolveHazardHazardInteraction(this, hazardCollider, otherHazard, other))
            {
                return;
            }
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (hazardData.type == Hazard.TrapType.Pitfall)
        {
            PitfallTriggered?.Invoke(this, other);
        }

        if (hazardData.damageType != Hazard.DamageType.Summon)
        {
            return;
        }

        SpawnSummonedObject();
    }

    void SpawnSummonedObject()
    {
        if (hazardData.summonObject == null || hazardData.summonObject.objectToSummon == null)
        {
            Debug.LogWarning($"{name} is set to summon a projectile, but no prefab is assigned.", this);
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        Quaternion spawnRotation = GetSpawnRotation();
        GameObject spawnedObject = Instantiate(hazardData.summonObject.objectToSummon, spawnPosition, spawnRotation);

        Projectile projectile = spawnedObject.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(hazardData);
        }
    }

    Vector3 GetSpawnPosition()
    {
        if (summonPoint != null)
        {
            return summonPoint.position;
        }

        return transform.TransformPoint(hazardData.summonObject.summonPosition);
    }

    Quaternion GetSpawnRotation()
    {
        if (summonPoint != null)
        {
            return summonPoint.rotation;
        }

        Quaternion configuredRotation = hazardData.summonObject.summonRotation;
        Quaternion prefabRotation = hazardData.summonObject.objectToSummon.transform.rotation;

        if (configuredRotation == default)
        {
            return transform.rotation * prefabRotation;
        }

        return transform.rotation * configuredRotation;
    }
    public float ApplyDamage()
    {
        float baseDamage = hazardData != null ? hazardData.baseDamage : 0f;
        float finalDamage = baseDamage; // Placeholder for any damage modifications based on hazard properties
        return finalDamage;
    }
    public bool StartDamageTimer(Rigidbody2D _rigidbody)
    {
        if (hazardData != null && hazardData.damageType == Hazard.DamageType.Delayed)
        {
            _rigidbody.sleepMode = RigidbodySleepMode2D.NeverSleep; // Ensure the rigidbody doesn't go to sleep while timer is active
            nextDamageTime = Time.time + hazardData.delayTime;
            return true;
        }
        return false;
    }
    public bool ResetDamageTimer(Rigidbody2D _rigidbody)
    {
        nextDamageTime = 0f;
        _rigidbody.sleepMode = RigidbodySleepMode2D.StartAwake; // Allow the rigidbody to sleep again
        return false;
    }
}
