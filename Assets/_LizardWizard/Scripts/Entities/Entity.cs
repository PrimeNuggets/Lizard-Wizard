using System;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using System.Runtime.CompilerServices;

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
                current = value;
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
    public enum MoveTypes // UNUSED: Defines the action that the player is taking
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
    private Animator animator;
    private float groundedTime;
    //===================================================
        //TrapManager Plugin - Xavier
    //===================================================
    public class StatusEffects
    {
        public bool onFire;
        public bool onPoison;
    }
    [NonSerialized] public StatusEffects status;
    
    public void Initialize(GameObject obj, Transform model = null)
    {
        InitializeStats();
        ctrl = true;
        entObj = obj;
        entChar = entObj.GetComponent<CharacterController>();
        aspect = entObj.GetComponent<Aspect>();
        visualRoot = model != null ? model.transform : entObj.transform;
        facingDir = UnityEngine.Vector3.ProjectOnPlane(
            model.forward, //reference the model's forward direction separate from the visualRoot
            UnityEngine.Vector3.up
        );
        try
        {
            animator = entObj.GetComponentInChildren<Animator>();
        }
        catch
        {
            Debug.LogWarning("No animator was found under the object: " + entObj.name);
        }
        try
        {
            animator = entObj.GetComponentInChildren<Animator>();
        }
        catch
        {
            Debug.LogWarning("No animator was found under the object: " + entObj.name); //Occurs so that unity doesn't treat this as a fatal error
        }
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
        AlignToGround(Time.deltaTime);
    }
    public void Disable()
    {   
        groundedTime = 0f;

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

        if (movement.sqrMagnitude > 0.0001f)
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
            SetStateType('S', true);
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
        //Physics
        velocity.y = Mathf.Sqrt(
            2f *
            Mathf.Abs(gravity) *
            Mathf.Max(0f, jumpHeight.GetCurrent())
        );
        SetStateType('A', false);
        groundedTime = 0f;

        //Animation
        if (animator == null) return;
        animator.SetBool("Grounded", false);
        animator.ResetTrigger("Jump");
        animator.SetTrigger("Jump");
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