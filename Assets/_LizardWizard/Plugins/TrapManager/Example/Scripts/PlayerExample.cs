using UnityEngine;
using System;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerExample : MonoBehaviour
{
    //==================================================
    // Classes and Structs
    //==================================================
    [Serializable]
    public class AnimationManager
    {
        public AnimationClip fullAnim;
        public AnimationClip animStart;
        public AnimationClip animEnd;
    }

    //==================================================
    // Non-Serialized Variables
    //==================================================
    Rigidbody2D _rigidbody;
    Animator _animator;
    bool controlsLocked = false;

    //==================================================
    // Variables
    //==================================================
    public Entity entityData;
    public GameObject healthBar; // Reference to the health bar UI element
    public AnimationManager pitfallAnim;
    public Terrain terrain;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        entityData.Initialize(gameObject, terrain);
    }

    void OnEnable()
    {
        HazardManager.PitfallTriggered += HandlePitfallTriggered;
    }

    void OnDisable()
    {
        HazardManager.PitfallTriggered -= HandlePitfallTriggered;
    }

    // Update is called once per frame
    void Update()
    {
        if (!controlsLocked)
        {
            Movement();
        }
    }

    void FixedUpdate()
    {
        
    }

    void Movement()
    {
        // Implement player movement logic here
        if (Input.GetKey(KeyCode.A))
        {
            //_rigidbody.linearVelocity = new Vector2(-stats.speed * _rigidbody.mass * Time.deltaTime, _rigidbody.linearVelocity.y);
            transform.Translate(Vector3.left * entityData.speed.GetCurrent() * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            //_rigidbody.linearVelocity = new Vector2(stats.speed * _rigidbody.mass * Time.deltaTime, _rigidbody.linearVelocity.y);
            transform.Translate(Vector3.right * entityData.speed.GetCurrent() * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.Space) && entityData.grounded)
        {
            _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, entityData.jumpHeight.GetCurrent());
            entityData.grounded = false;
        }
    }
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground"))
        {
            entityData.grounded = true;
        }
    }

    public void AddHealth(float amount, int operation)
    {
        entityData.health.Set(amount);

        //UI Update
        healthBar.transform.localScale = new Vector3(entityData.health.GetCurrent() / entityData.health.GetMax(), healthBar.transform.localScale.y, healthBar.transform.localScale.z); // Update health bar scale
        healthBar.GetComponentInChildren<SpriteRenderer>().color = Color.Lerp(Color.red, Color.green, entityData.health.GetCurrent() / entityData.health.GetMax()); // Update health bar color
    }

    void HandlePitfallTriggered(HazardManager hazard, Collider2D other)
    {
        if (other.attachedRigidbody != _rigidbody)
        {
            return;
        }

        _animator.Play(pitfallAnim.animStart.name);
    }

    //Hazard Detection
    void OnTriggerEnter2D(Collider2D other)
    {
        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null)
        {
            AddHealth(-proj.ApplyDamage(), 1);
            proj.Consume();
            return;
        }

        HazardManager hazard = other.GetComponent<HazardManager>();
        if (hazard != null)
        {
            if (hazard.hazardData.damageType == Hazard.DamageType.Instant)
            {
                AddHealth(-hazard.ApplyDamage(), 1);
            }
            if (hazard.hazardData.damageType == Hazard.DamageType.Delayed)
            {
                if (hazard.nextDamageTime == 0f) // If the timer hasn't started yet
                {
                    controlsLocked = hazard.StartDamageTimer(_rigidbody); // Lock player controls while waiting for delayed damage
                }
            }
        }

        InteractionCollision intCol = other.GetComponent<InteractionCollision>();
        if (intCol != null)
        {
            if (intCol.collisionFXData.damageType == CollisionEffects.DamageType.Instant)
            {
                AddHealth(-intCol.collisionFXData.damage, 1);
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        HazardManager hazard = other.GetComponent<HazardManager>();
        if (hazard != null)
        {
            if (hazard.hazardData.damageType == Hazard.DamageType.DoT) {
                _rigidbody.sleepMode = RigidbodySleepMode2D.NeverSleep; // Ensure the rigidbody doesn't go to sleep while in a DoT hazard
                if (Time.time >= hazard.nextDamageTime)
                {
                    AddHealth(-hazard.ApplyDamage(), 1);
                    hazard.nextDamageTime = Time.time + hazard.hazardData.damageInterval;
                }
            }
            if (hazard.hazardData.damageType == Hazard.DamageType.Delayed)
            {
                //Animation Event Trigger
                if (Time.time >= hazard.nextDamageTime && hazard.nextDamageTime != 0f) // Check if the timer has finished and was actually started
                {
                    AddHealth(-hazard.ApplyDamage(), 1);
                    controlsLocked = hazard.ResetDamageTimer(_rigidbody); // Unlock controls after applying delayed damage
                    if (hazard.hazardData.type == Hazard.TrapType.Pitfall)
                    {
                        _animator.Play(pitfallAnim.animEnd.name); // Make the player visible again after falling into a pit
                    }
                }
            }
        }

        InteractionCollision intCol = other.GetComponent<InteractionCollision>();
        if (intCol != null)
        {
            if (intCol.collisionFXData.damageType == CollisionEffects.DamageType.DoT)
            {
                _rigidbody.sleepMode = RigidbodySleepMode2D.NeverSleep; // Ensure the rigidbody doesn't go to sleep while in a DoT hazard
                if (Time.time >= intCol.nextDamageTime)
                {
                    AddHealth(intCol.collisionFXData.damage, 1);
                    intCol.nextDamageTime = Time.time + intCol.collisionFXData.dotInterval;
                }
            }
        }
    }
    void OnTriggerExit2D(Collider2D other)
    {
        HazardManager hazard = other.GetComponent<HazardManager>();
        if (hazard != null && hazard.hazardData.damageType == Hazard.DamageType.DoT)
        {
            _rigidbody.sleepMode = RigidbodySleepMode2D.StartAwake; // Allow the rigidbody to sleep when exiting a DoT hazard
        }
    }
}
