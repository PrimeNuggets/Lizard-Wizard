using System;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "Entity", menuName = "Scriptable Objects/Entity")]
public class Entity : ScriptableObject
{
    [Serializable]
    public class Stat
    {
        float current;
        [SerializeField] float max;
        public float GetCurrent()
        {
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
    public Stat health;
    [Tooltip("Effectively Mana")] public Stat water;
    public Stat speed;
    public Stat jumpHeight;
    public NoiseChannel noiseChannel;

    //===================================================
        //Non-Serializable Variables
    //===================================================
    [NonSerialized] public bool grounded = false;
    [NonSerialized] public bool ctrl = false; //Whether the entity is in control of their own actions
    private readonly Movement.Physics physics = new Movement.Physics();
    [NonSerialized] public Vector3 velocity;
    [NonSerialized] public Terrain terrain;
    [NonSerialized] public GameObject entObj;
    [NonSerialized] public CharacterController entChar;
    [NonSerialized] public StateTypes stateType = StateTypes.Standing;
    [NonSerialized] public StateTypes prevStateType = StateTypes.Standing;
    [NonSerialized] public MoveTypes moveType = MoveTypes.Idling;
    [NonSerialized] public MoveTypes prevMoveType = MoveTypes.Idling;
    private Aspect aspect;
    //===================================================
        //TrapManager Plugin - Xavier
    //===================================================
    public class StatusEffects
    {
        public bool onFire;
        public bool onPoison;
    }
    [NonSerialized] public StatusEffects status;
    
    public void Initialize(GameObject obj, Terrain ter)
    {
        InitializeStats();
        ctrl = true;
        entObj = obj;
        terrain = ter;
        entChar = entObj.GetComponent<CharacterController>();
        aspect = entObj.GetComponent<Aspect>();
    }
    public void InitializeStats()
    {
        health.Set(health.GetMax());
        water.Set(water.GetMax() / 2);
        speed.Set(speed.GetMax());
        jumpHeight.Set(jumpHeight.GetMax());
    }
    public void ApplyMove(Vector2 move, bool jumped, Transform moveRef = null)
    {
        float dt = Time.deltaTime;
        grounded = entChar.isGrounded;
        prevStateType = stateType;
        if (grounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
        
        move = Vector2.ClampMagnitude(move, 1f); //Processes WASD
        if (!ctrl)
        {
            move = Vector2.zero;
        }

        Transform reference = moveRef == null ? entObj.transform : moveRef;
        Vector3 fwd = Vector3.ProjectOnPlane(reference.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        if (grounded && ctrl) {
            Vector3 direction = right * move.x + fwd * move.y;

            velocity.x = direction.x * speed.GetCurrent();
            velocity.z = direction.z * speed.GetCurrent();
            if (jumped)
            {
                float gravity = physics.applyGravity(0f, 1f);

                velocity.y = Mathf.Sqrt(
                    2f *
                    Mathf.Abs(gravity) *
                    Mathf.Max(0f, jumpHeight.GetCurrent())
                );
                SetStateType('A', false);
            }
        }
        velocity.y = physics.applyGravity(velocity.y, dt); //Handles gravity

        CollisionFlags collisions = entChar.Move(velocity * dt);

        grounded = entChar.isGrounded;
        if (grounded && velocity.y < 0f)
        {
            velocity.y = -2f;
            SetStateType('S', true);
            if (prevStateType == StateTypes.Air)
            {
                noiseChannel.Raise(entObj.transform.position, aspect);
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
    public void SetStateType(char state, bool ctrl)
    {
        switch (state)
        {
            case 'A':
                stateType = StateTypes.Air;
                grounded = false;
                break;
            case 'C':
                stateType = StateTypes.Crouching;
                grounded = true;
                break;
            case 'D':
                stateType = StateTypes.Diving;
                grounded = false;
                ctrl = true; //Unlike Air, Diving specifically sets control to true to allow for state cancels. Transitions to LyingDown (L) when colliding with the ground
                break;
            case 'S':
                stateType = StateTypes.Standing;
                grounded = true;
                break;
            case 'L':
                stateType = StateTypes.LyingDown;
                grounded = true;
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
}