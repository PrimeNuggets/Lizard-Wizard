using UnityEngine;
using System;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Projectile : MonoBehaviour
{
    [Serializable]
    public class Bound2D
    {
        public float left;
        public float right;
        public float bottom;
        public float top;

        public Bound2D(float left, float right, float bottom, float top)
        {
            this.left = left;
            this.right = right;
            this.bottom = bottom;
            this.top = top;
        }
    }
    public Bound2D bounds = new Bound2D(-20f, 20f, -20f, 20f); // left, right, bottom, top bounds
    public float Damage { get; private set; }
    public Hazard SourceHazard { get; private set; }
    public bool IsOnFire => entityData != null && entityData.status != null && entityData.status.onFire;
    public bool IsOnPoison => entityData != null && entityData.status != null && entityData.status.onPoison;
    [Tooltip("Health is the projectile's lifespan. When it reaches 0, the projectile is destroyed.")]
    public Entity entityData; // Reference to the entity data for stat modifications
    public Terrain terrain;

    public void Initialize(Hazard sourceHazard, float _speed = 10f)
    {
        SourceHazard = sourceHazard;
        Damage = sourceHazard != null ? sourceHazard.baseDamage : 0f;
        if (entityData != null)
        {
            entityData = Instantiate(entityData);
        }
        EnsureRuntimeState();
        entityData.Initialize(gameObject, terrain);
    }

    public void Consume()
    {
        Destroy(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.up * entityData.speed.GetCurrent() * Time.deltaTime, Space.Self);

        if (transform.position.x < bounds.left || transform.position.x > bounds.right || transform.position.y < bounds.bottom || transform.position.y > bounds.top)
        {
            Consume();
        }
        entityData.health.Set(entityData.health.GetCurrent() - Time.deltaTime);
        if (entityData.health.GetCurrent() <= 0f)
        {
            Consume();
        }
    }

    public float ApplyDamage()
    {
        EnsureRuntimeState();
        float baseDamage = Damage;
        if (entityData.status.onFire)
        {
            baseDamage += 5f; // Example of increased damage when on fire
        }
        float finalDamage = baseDamage; // Placeholder for any damage modifications based on entity stats or status effects
        return finalDamage;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        HazardManager _hazard = col.GetComponent<HazardManager>();
        if (_hazard != null)
        {
            EnsureRuntimeState();
            if (InteractionsManager.TryResolveProjectileHazardInteraction(this, col, _hazard))
            {
                return;
            }

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (_hazard.hazardData.type == Hazard.TrapType.Fire && !entityData.status.onFire)
            {
                entityData.status.onFire = true;
                sr.color = Color.orangeRed;
                entityData.speed.Set(entityData.speed.GetCurrent() + 2f); // Example of increasing speed when hitting a fire hazard
            }
            else if (_hazard.hazardData.type == Hazard.TrapType.PoisonFloor && !entityData.status.onPoison)
            {
                entityData.status.onPoison = true;
                sr.color = Color.purple;
                entityData.speed.Set(entityData.speed.GetCurrent() - 2f); // Example of decreasing speed when hitting a poison floor hazard
            }
        }
    }

    void EnsureRuntimeState()
    {
        if (entityData == null)
        {
            return;
        }

        if (entityData.status == null)
        {
            entityData.status = new Entity.StatusEffects();
        }
    }
}
