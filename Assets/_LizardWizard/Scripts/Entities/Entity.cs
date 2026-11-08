using System;
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
            }
            else
            {
                this.max = value;
            }
        }
        public void Initialize(float current, [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (initialized)
            {
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
    [NonSerialized] public bool crouched = false;
    [NonSerialized] public bool ctrl = false; //Whether the entity is in control of their own actions
    private readonly Movement.Physics physics = new Movement.Physics();
    [NonSerialized] public UnityEngine.Vector3 momentum;
    [NonSerialized] public UnityEngine.Vector3 storedMomentum;
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
    private UnityEngine.Vector3 GroundNormal;
    private UnityEngine.Vector3 LastGroundNormal;

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
            visualRoot.forward,
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

    //OKAY SO i separated getting ground normals out and realized this is gonna be on every entity
    //which is, uh, maybe a few raycasts per frame. apparently first 100 raycasts per frame are basically free cpu wise
    //so! if performance starts suffering, or if you wanna make this bitch more efficient for the toasters among us
    //make this thing memoized, as in if it already ran this frame just throw the shit it already calculated and skip the raycast
    public Vector3 GetGroundNormal()
    {
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
        return normal;
    }
    public void AlignToGround(float deltaTime)
    {
        if (visualRoot == null)
        {
            Debug.LogError("AlignToGround: Entity Data references no GameObject");
            return;
        }

        UnityEngine.Vector3 movement = velocity;
        //movement.y = 0;

        if (movement.sqrMagnitude > 0.0001f)
        {
            facingDir = movement.normalized;
        }
        if (animator != null)
        {
            groundedTime = grounded ? groundedTime + deltaTime : 0f;
            animator.SetBool("Grounded", grounded && groundedTime >= groundContactDelay);
            animator.SetFloat("Speed", movement.magnitude);
        }

        Vector3 normal = GetGroundNormal();
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
        //If i can get this working:
        //Slide gains momentum from hills, but slides down
        //Run loses momentum over time, but can maintain it up hills

        switch (GetStateType())
        {
            //AIRSTRAFE
            case 'A':
                //  Debug.Log("air");
                break;
            //DIVING
            case 'D':
                // Debug.Log("dive");
                break;
            //RUNNING
            case 'S':
                //  Debug.Log("stand");
                break;
            //SLIDING
            case 'C':
                //  Debug.Log("Crouch");
                break;
        }

        LastGroundNormal = GroundNormal;
        GroundNormal = GetGroundNormal();
        float dt = Time.deltaTime;
        grounded = entChar.isGrounded;
        if (grounded && velocity.y < 0f)
        {
            //  velocity.y = -0.1f;
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
        if (ctrl)
        {
            UnityEngine.Vector3 direction = right * move.x + fwd * move.y;

            velocity.x = direction.x * speed.GetCurrent();
            velocity.z = direction.z * speed.GetCurrent();
            //yea take a look at this maybe idk
            UnityEngine.Vector3 downhillAcceleration = UnityEngine.Vector3.ProjectOnPlane(UnityEngine.Vector3.down * gravity * 1.5f, velocity);
            if (jumped && grounded)
            {
                PlayJump(gravity);
            }
        }
        velocity.y = physics.applyGravity(velocity.y, dt); //Handles gravity

        momentum = entChar.velocity * 0.9f;
        //Debug.Log(momentum);

        //Check if top of hill
        if (Vector3.Dot(GroundNormal, Vector3.up) < Vector3.Dot(LastGroundNormal, Vector3.up) && entChar.velocity.y > 0)
        {
            //Debug.Log("hilltop");
        }

        //
        CollisionFlags collisions = entChar.Move((momentum + storedMomentum + velocity) * dt);
        //
        // storedMomentum += UnityEngine.Vector3.ProjectOnPlane((momentum + velocity), GroundNormal).normalized;
        // momentum = UnityEngine.Vector3.ProjectOnPlane((momentum + velocity), GetGroundNormal());


        //okokokok new idea
        //storedMomentum keeps u going
        //use LERP
        //storedMomentum = storedMomentum + Vector3.Scale(velocity, Vector3.Lerp(storedMomentum.normalized, velocity.normalized, Time.deltaTime));
        //ok maybe not like that 
        //2am coding fail lmao

        grounded = entChar.isGrounded;
        if (grounded && velocity.y < 0f)
        {
            float landingSpeed = Mathf.Abs(velocity.y);
            // velocity.y = -0.1f;

            SetStateType('S', true);
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
        }
        else if (!grounded)
        {
            SetStateType('A', ctrl);
        }

        if ((collisions & CollisionFlags.Above) != 0 && velocity.y > 0f)
        {
            //velocity.y = 0f;
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
        if (crouched)
        {
            SetStateType('D', true);
            groundedTime = 0f;
        }
        else
        {
            SetStateType('A', true);
            groundedTime = 0f;
        }

        //Scoring
        Scoring.AddValue($"{entObj.name.ToLowerInvariant()}.jump", 1);

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
        if (!stateTypeChanged)
        {
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
        if (!moveTypeChanged)
        {
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
    public void CrouchDown()
    {
        Debug.Log("CrouchDown");
        if (GetStateType() == 'A') //Air --> Dive
        {
            SetStateType('D', true);
            Debug.Log(GetStateType());
        }
        if (GetStateType() == 'S') //Stand --> Crouch
        {
            SetStateType('C', true);
            Debug.Log(GetStateType());
        }
    }
    public void CrouchUp()
    {
        if (GetStateType() == 'C') //Crouch --> Stand
        {
            SetStateType('S', true);
            Debug.Log(GetStateType());
        }
        if (GetStateType() == 'D') //Dive --> Air
        {
            SetStateType('A', true);
            Debug.Log(GetStateType());
        }
    }

}