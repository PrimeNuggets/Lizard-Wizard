using System;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Entity", menuName = "Scriptable Objects/Entity")]
public class Entity : ScriptableObject
{
    [Serializable]
    public class Stat
    {
        private bool initialized = false;
        float current;
        [SerializeField] float max;
        public float GetCurrent([CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!initialized) //Reference Entity.Initialize() in Start/Awake method before using
            {
                Debug.LogWarning($"{System.IO.Path.GetFileName(file)}:{line}\nGetCurrent(): Stats never initialized");
            }
            return current;
        }
        public float GetMax()
        {
            return max;
        }
        public void Set(float value, bool max = false)
        {
            if (!max)
            {
                float minimum = GetMax() < 0f ? GetMax() : 0f;
                float maximum = GetMax() >= 0f ? GetMax() : 0f;
                current = Mathf.Clamp(value, minimum, maximum);
            } else
            {
                this.max = value;
            }
        }
        public void Initialize(float current, [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (initialized) {
                Debug.LogWarning($"{System.IO.Path.GetFileName(file)}:{line}\nThis stat has already been initialized.\nAborting Method");
                return;
            }
            Set(current);
            initialized = true;
        }
    }
    public enum StateTypes // Sets what physics should be used based on the lizard's current animation. Will also help with future additions like stealth.
    {
        Unchanged,
        Standing,
        Crouching,
        Air,
        LyingDown,
        Diving,
    }
    public enum MoveTypes
    {
        Unchanged,
        Idling, 
        Attacking,
        Guarding,
        Hurt,
    }
    //===================================================
        //Serializable Variables
    //===================================================
    [Header("Stats")]
    public Stat health;
    [Tooltip("Effectively Mana")] public Stat water;
    public Stat speed;
    public Stat jumpHeight;

    [Header("Sound")]
    public NoiseChannel noiseChannel;

    [Header("Surface Alignment")]
    public LayerMask groundLayer = ~0;
    public float groundProbeDist = 0.3f;
    [Min(0f)] public float modelTurnSpeed = 540f;
    [Min(0f)] public float groundContactDelay = 0.08f;

    //===================================================
        //Non-Serializable Variables
    //===================================================
    [NonSerialized] public bool grounded = false;
    [NonSerialized] public bool ctrl = false; //Whether the entity is in control of their own actions
    private readonly Movement.Physics physics = new Movement.Physics();
    [NonSerialized] public UnityEngine.Vector3 velocity;
    [NonSerialized] public GameObject entObj;
    [NonSerialized] public CharacterController entChar;
    [NonSerialized] public StateTypes stateType = StateTypes.Standing;
    [NonSerialized] public StateTypes prevStateType = StateTypes.Standing;
    bool stateTypeChanged = false;
    [NonSerialized] public MoveTypes moveType = MoveTypes.Idling;
    [NonSerialized] public MoveTypes prevMoveType = MoveTypes.Idling;
    bool moveTypeChanged = false;
    private Aspect aspect;
    [NonSerialized] public Transform visualRoot;
    private UnityEngine.Vector3 facingDir;
    protected bool initialized = false;
    [NonSerialized] public Animator animator;
    private float groundedTime;
    [NonSerialized] public bool attackFacingLocked;
    private bool hurtActive;
    private float pauseRemaining;
    private float recoveryRemaining;
    private UnityEngine.Vector3 pendingKnockback;
    private bool hitPaused;
    private float animatorSpeed = 1f;
    [NonSerialized] public bool invulnerable = false;
    //===================================================
        //TrapManager Plugin - Xavier
    //===================================================
    public class StatusEffects
    {
        public bool onFire;
        public bool onPoison;
    }
    [NonSerialized] public StatusEffects status;
    
    public bool IsHitPaused => hitPaused;
    
    public void Initialize(GameObject obj, Transform model = null)
    {
        InitializeStats();
        ctrl = true;
        entObj = obj;
        entChar = entObj.GetComponent<CharacterController>();
        aspect = entObj.GetComponent<Aspect>();
        visualRoot = model != null ? model.transform : entObj.transform;
        facingDir = UnityEngine.Vector3.ProjectOnPlane(
            visualRoot.forward,
            UnityEngine.Vector3.up
        );
        try
        {
            animator = entObj.GetComponentInChildren<Animator>();
        }
        catch
        {
            Debug.LogWarning("No animator was found under the object: " + entObj.name); //Occurs so that unity doesn't treat this as a fatal error
        }
        attackFacingLocked = false;
        ResetHurt();
        initialized = true;
    }
    public void InitializeStats()
    {
        health.Initialize(health.GetMax());
        water.Initialize(water.GetMax() / 2);
        speed.Initialize(speed.GetMax());
        jumpHeight.Initialize(jumpHeight.GetMax());
    }
    public void BeginTick() //reference in every entity's fixed update method
    {
        stateTypeChanged = false;
        moveTypeChanged = false;

        prevStateType = stateType;
        prevMoveType = moveType;
    }
    public void AfterTick()
    {   
        if (hitPaused) return;
        AlignToGround(Time.deltaTime);
    }
    public void Disable()
    {   
        groundedTime = 0f;
        attackFacingLocked = false;
        ResetHurt();

        if (animator == null || !animator.isInitialized) return;
        animator.SetFloat("Speed", 0f);
        animator.SetBool("Grounded", false);
        animator.ResetTrigger("Jump");
    }
    public void AlignToGround(float deltaTime)
    {
        if (visualRoot == null) {
            Debug.LogError("AlignToGround: Entity Data references no GameObject");
            return;
        }

        UnityEngine.Vector3 movement = velocity;
        movement.y = 0;

        if (!attackFacingLocked && movement.sqrMagnitude > 0.0001f)
        {
            facingDir = movement.normalized;
        }
        if (animator != null) {
            groundedTime = grounded ? groundedTime + deltaTime : 0f;
            animator.SetBool("Grounded", grounded && groundedTime >= groundContactDelay);
            animator.SetFloat("Speed", movement.magnitude);
        }

        UnityEngine.Vector3 normal = UnityEngine.Vector3.up;
        if (grounded)
        {
            Bounds bounds = entChar.bounds;
            float dist = bounds.extents.y + groundProbeDist;

            bool foundGround = UnityEngine.Physics.Raycast(
                bounds.center,
                UnityEngine.Vector3.down,
                out RaycastHit hit,
                dist,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );

            Debug.DrawRay(
                bounds.center,
                UnityEngine.Vector3.down * (bounds.extents.y + groundProbeDist),
                foundGround ? Color.darkGreen : Color.red
            );

            if (foundGround)
            {
                normal = hit.normal;
                Debug.DrawRay(hit.point, normal * 0.5f);
            }
        }
        UnityEngine.Vector3 forward = UnityEngine.Vector3.ProjectOnPlane(facingDir, normal);
        if (forward.sqrMagnitude < 0.0001f) return;

        UnityEngine.Quaternion target = UnityEngine.Quaternion.LookRotation(forward, normal);

        visualRoot.rotation = UnityEngine.Quaternion.RotateTowards(
            visualRoot.rotation,
            target,
            modelTurnSpeed * deltaTime
        );
    }
    public void ApplyMove(UnityEngine.Vector2 move, bool jumped, Transform moveRef = null)
    {
        float dt = Time.deltaTime;
        grounded = entChar.isGrounded;
        if (TickHurt(dt)) return;

        if (grounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
        
        move = UnityEngine.Vector2.ClampMagnitude(move, 1f); //Processes WASD
        if (!ctrl)
        {
            move = UnityEngine.Vector2.zero;
        }

        Transform reference = moveRef == null ? entObj.transform : moveRef;
        UnityEngine.Vector3 fwd = UnityEngine.Vector3.ProjectOnPlane(reference.forward, UnityEngine.Vector3.up).normalized;
        UnityEngine.Vector3 right = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, fwd);
        float gravity = physics.applyGravity(0f, 1f);
        if (grounded && ctrl) {
            UnityEngine.Vector3 direction = right * move.x + fwd * move.y;

            velocity.x = direction.x * speed.GetCurrent();
            velocity.z = direction.z * speed.GetCurrent();
            UnityEngine.Vector3 downhillAcceleration = UnityEngine.Vector3.ProjectOnPlane(UnityEngine.Vector3.down * gravity * 1.5f, velocity);
            if (jumped)
            {
                PlayJump(gravity);
            }
        }
        velocity.y = physics.applyGravity(velocity.y, dt); //Handles gravity

        CollisionFlags collisions = entChar.Move(velocity * dt);

        grounded = entChar.isGrounded;
        if (grounded && velocity.y < 0f)
        {
            float landingSpeed = Mathf.Abs(velocity.y);
            velocity.y = -2f;
            SetStateType('S', GetMoveType() == 'A' ? ctrl : true); //Force grounded if not attacking
            //-- TODO: landing check if we were spinning. within ~35 of upright = normal landing, otherwise animator.SetTrigger("Crash")
            //-- TODO: on crash turn off ctrl for the Crash clip (~2.2s) and reset combo
            //-- TODO: clean landing after a flip = small speed boost + add to combo
            //-- TODO: sand puff particles on landing (bigger on crash)
            if (prevStateType == StateTypes.Air && noiseChannel != null)
            {
                //-- Harder landings make more noise
                float strength = Mathf.Clamp(landingSpeed / 15f, 0.5f, 2f);
                noiseChannel.Raise(entObj.transform.position, aspect, strength);
            }
        } else if (!grounded)
        {
            SetStateType('A', ctrl);
        }

        if ((collisions & CollisionFlags.Above) != 0 && velocity.y > 0f)
        {
            velocity.y = 0f;
        }
    }

    //Now that jumping logic is separate, this opens opportunities for uncontrolled/mid-air jumps as long as a gravity value is given
    public void PlayJump(float gravity)
    {
        //-- TODO: one flip takes 0.7s, so most height should come from launching off dune crests, not a taller jump. maybe less gravity while held
        //Physics
        velocity.y = Mathf.Sqrt(
            2f *
            Mathf.Abs(gravity) *
            Mathf.Max(0f, jumpHeight.GetCurrent())
        );
        SetStateType('A', false);
        groundedTime = 0f;

        //Scoring
        Scoring.AddValue($"{entObj.name.ToLowerInvariant()}.jump", 1);

        //Animation
        if (animator == null) return;
        animator.SetBool("Grounded", false);
        animator.ResetTrigger("Jump");
        animator.SetTrigger("Jump");
    }
    public void SetAttackFacing(UnityEngine.Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) return;
        facingDir = direction.normalized;
        attackFacingLocked = true;

        if (visualRoot != null) {
            visualRoot.rotation = UnityEngine.Quaternion.LookRotation(facingDir, UnityEngine.Vector3.up);
        }
    }
    public void BeginHurt(int stunFrames, UnityEngine.Vector3 knockback, int recoveryFrames)
    {
        hurtActive = true;
        pauseRemaining = Mathf.Max(0, stunFrames) / 60f;
        recoveryRemaining = Mathf.Max(1, recoveryFrames) / 60f;
        pendingKnockback = knockback;

        ctrl = false;
        SetMoveType('H');

        BeginHitTime(stunFrames);
        
        if (!hitPaused)
        {
            velocity += pendingKnockback;
            pendingKnockback = UnityEngine.Vector3.zero;
        }
    }
    private bool TickHurt(float dt)
    {
        if (hitPaused)
        {
            pauseRemaining = Mathf.Max(0f, pauseRemaining - dt);
            if (pauseRemaining > 0f) return true;

            hitPaused = false;
            if (animator != null) animator.speed = animatorSpeed;
            if (hurtActive) {
                velocity += pendingKnockback;
                pendingKnockback = UnityEngine.Vector3.zero;
            }

            return false;
        }
        if (!hurtActive) return false;

        ctrl = false;
        recoveryRemaining = Mathf.Max(0f, recoveryRemaining - dt);
        if (recoveryRemaining <= 0f)
        {
            hurtActive = false;
            SetMoveType('I');
            ctrl = true;
        }

        return false;
    }
    private void ResetHurt()
    {
        if (hitPaused && animator != null) animator.speed = animatorSpeed;

        invulnerable = false;
        hitPaused = false;
        hurtActive = false;
        pauseRemaining = 0f;
        recoveryRemaining = 0f;
        pendingKnockback = UnityEngine.Vector3.zero;
    }
    public void BeginHitTime(int frames)
    {
        float duration = Mathf.Max(0, frames) / 60f;
        if (duration <= 0f) return;

        if (!hitPaused && animator != null) animatorSpeed = animator.speed;

        pauseRemaining = Mathf.Max(pauseRemaining, duration);
        hitPaused = true;
        if (animator != null) animator.speed = 0f;
    }
    public char GetStateType()
    {
        char ret = 'U';
        switch (stateType)
        {
            case StateTypes.Standing:
                ret = 'S';
                break;
            case StateTypes.Air:
                ret = 'A';
                break;
            case StateTypes.Crouching:
                ret = 'C';
                break;
            case StateTypes.LyingDown:
                ret = 'L';
                break;
            case StateTypes.Diving:
                ret = 'D';
                break;
        }
        return ret;
    }
    public char GetMoveType()
    {
        char ret = 'U';
        switch (moveType)
        {
            case MoveTypes.Idling:
                ret = 'I';
                break;
            case MoveTypes.Attacking:
                ret = 'A';
                break;
            case MoveTypes.Guarding:
                ret = 'G';
                break;
            case MoveTypes.Hurt:
                ret = 'H';
                break;
        }
        return ret;
    }
    public void SetStateType(char state, bool ctrl)
    {
        if (!stateTypeChanged) {
            prevStateType = stateType;
        }
        switch (state)
        {
            case 'A':
                stateType = StateTypes.Air;
                grounded = false;
                stateTypeChanged = true;
                break;
            case 'C':
                stateType = StateTypes.Crouching;
                grounded = true;
                stateTypeChanged = true;
                break;
            case 'D':
                stateType = StateTypes.Diving;
                grounded = false;
                ctrl = true; //Unlike Air, Diving specifically sets control to true to allow for state cancels. Transitions to LyingDown (L) when colliding with the ground
                stateTypeChanged = true;
                break;
            case 'S':
                stateType = StateTypes.Standing;
                grounded = true;
                stateTypeChanged = true;
                break;
            case 'L':
                stateType = StateTypes.LyingDown;
                grounded = true;
                stateTypeChanged = true;
                break;
            default:
                stateType = StateTypes.Unchanged;
                if (state != 'U')
                {
                    Debug.LogWarning("StateType \"" + state + "\" doesn't exist.\nKeeping the previous state to prevent errors.");
                }
                break;
        }
        if (stateType == StateTypes.Unchanged)
        {
            stateType = prevStateType;
        }
        this.ctrl = ctrl;
    }
    public void SetMoveType(char move)
    {
        if (!moveTypeChanged) {
            prevMoveType = moveType;
        }
        switch (move)
        {
            case 'I':
                moveType = MoveTypes.Idling;
                moveTypeChanged = true;
                break;
            case 'H':
                moveType = MoveTypes.Hurt;
                moveTypeChanged = true;
                break;
            case 'A':
                moveType = MoveTypes.Attacking;
                moveTypeChanged = true;
                break;
            case 'G':
                moveType = MoveTypes.Guarding;
                moveTypeChanged = true;
                break;
            default:
                moveType = MoveTypes.Unchanged;
                if (move != 'U')
                {
                    Debug.LogWarning("MoveType \"" + move + "\" doesn't exist.\nKeeping the previous type to prevent errors.");
                }
                break;
        }
        if (moveType == MoveTypes.Unchanged)
        {
            moveType = prevMoveType;
        }
    }
}