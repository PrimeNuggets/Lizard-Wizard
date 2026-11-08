using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;
using static DebugHandler;

[RequireComponent(typeof(Aspect))]
[RequireComponent(typeof(CharacterController))]
public abstract class EnemyAI : MonoBehaviour
{
    public enum AIStates
    {
        Idle,
        Wander,
        Chase,
        Attack,
        Retreat,
        Search,
        Hurt,
        //Add More Here
    }

    [Serializable]
    public class Cooldown //How much time must pass before the state can be re-entered
    {
        [LabelOverride("Time")] public float maxTime = 2f;
        [NonSerialized] public float timer = 0f;
    }

    [Serializable]
    public class StateTimer //How long the state is active for
    {
        [LabelOverride("Time")] public float maxTime = 40f;
        [NonSerialized] public float timer = 0f;
    }
    [Serializable]
    public class AttackList
    {
        public AttackData attack;
        public float weight;

        public string animatorBool;
    }

    //===========================================
        //[Header("Debug Options")]
    //===========================================
    [SerializeField] private bool debugAttacks;
    //===========================================
        [Header("Entity Data")]
    //===========================================
    [LabelOverride("Data")] public Entity entityTemplate;
    [NonSerialized] public Entity entityData;

    //===========================================
        //[Header("AI Config")]
    //===========================================
    [SerializeField] public Dictionary<AIStates, Cooldown> actionCooldowns = new Dictionary<AIStates, Cooldown>(); //SerializeField & Public cuz apparently Unity doesn't serialize just public for dictionary fields
    [SerializeField] public Dictionary<AIStates, StateTimer> actionStateTimes = new Dictionary<AIStates, StateTimer>();
    [SerializeField, Tooltip("Grace area in degrees for turning")] protected float turnThreshold = 3f;
    [SerializeField, LabelOverride("Wander Speed Multiplier")] protected float wanderSpeedMult = 0.25f;
    [SerializeField] protected float searchTurnTime = 1f;
    [SerializeField, LabelOverride("Search Speed Multiplier")] protected float searchSpeedMult = 0.5f;
    [SerializeField, Tooltip("How far from where it spawned it wanders")] protected float wanderRadius = 15f;

    //-- What it does once it gets to the spot it's searching
    [Header("Search")]
    [SerializeField, Tooltip("How far from the spot it checks around")] protected float searchRadius = 6f;
    [SerializeField, Tooltip("How many extra spots it checks nearby")] protected int searchPoints = 2;
    [SerializeField] protected float lookAroundTime = 1.5f;
    [SerializeField, Tooltip("How far it turns each way when looking around")] protected float lookAroundAngle = 70f;

    //===========================================
        [Header("Chase")]
    //===========================================
    [SerializeField, LabelOverride("Chase Speed Multiplier")] protected float chaseSpeedMult = 0.6f;
    [SerializeField, Tooltip("Pause to notice the player before running at them")] protected float chaseReactionTime = 0.4f;
    [SerializeField, Tooltip("Seconds to get up to full chase speed")] protected float chaseAccelTime = 0.75f;
    [SerializeField, Tooltip("Degrees per second when turning toward the player")] protected float chaseTurnSpeed = 540f;
    [SerializeField, Tooltip("Seconds without seeing or hearing the player before giving up the chase")] protected float loseTargetTime = 5f;
    [SerializeField, Tooltip("Stops moving when this close to the player")] protected float chaseStopDistance = 1.5f;
    [SerializeField, Tooltip("Gives up if the player gets this far away")] protected float giveUpDistance = 25f;
    [SerializeField, Tooltip("Max seconds ahead to aim where the player is going. 0 = aim right at them")] protected float maxLeadTime = 0.75f;
    [SerializeField, Tooltip("How long after a detection it still knows exactly where the player is")] protected float trackingTime = 0.5f;
    //===========================================
        //[Header("Attacks")]
    //===========================================
    [SerializeField] protected AttackList[] attacks;
    [NonSerialized] public AttackData currentAttack;
    [NonSerialized] public AttackList currentAttackEntry;
    [NonSerialized] public string prevAttack;

    [SerializeField, Tooltip("While chasing, it can feel the player this close even through walls")] protected float senseRadius = 6f;
    [SerializeField, Tooltip("When it loses the player it keeps going this far the way they were heading")] protected float followThroughDistance = 5f;

    //===========================================
        [Header("Misc")]
    //===========================================
    [SerializeField] private Terrain terrain;
    [SerializeField] private Transform mesh;

    //===========================================
        //Non-Serialized
    //===========================================
    [NonSerialized] public AIStates state = AIStates.Idle;
    [NonSerialized] public AIStates prevState = AIStates.Idle;
    [NonSerialized] public StateTimer stateTimer;
    [NonSerialized] public float stateTime;
    [NonSerialized] public Dictionary<AIStates, float> stateScores = new Dictionary<AIStates, float>();

    protected Queue<string> brain = new Queue<string>();
    protected Vector3 wanderDest;
    protected bool hasDest;

    private Dictionary<AIStates, EnemyState> states;
    private EnemyState currentState;
    private SearchState searchState;

    private CharacterController targetController;
    private Vector3 lastKnownHeading;

    private Vector3 homePosition;
    private ObstacleAvoidance avoidance;

    public Aspect Target { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }
    public string LastSense { get; private set; }
    public float LastDetectionTime { get; private set; }

    //-- SearchState uses these without changing the Inspector fields
    internal bool HasSearchDestination => hasDest;
    internal Vector3 SearchDestination => wanderDest;
    internal float SearchTurnDuration => searchTurnTime;
    internal float SearchSpeedMultiplier => searchSpeedMult;
    internal float SearchRadius => searchRadius;
    internal int SearchPoints => searchPoints;
    internal float LookAroundTime => lookAroundTime;
    internal float LookAroundAngle => lookAroundAngle;

    internal void PickSearchDestination()
    {
        WanderDestination();
    }

    //-- WanderState uses the same destination as Search
    internal bool HasWanderDestination => hasDest;
    internal Vector3 WanderDestinationPoint => wanderDest;
    internal float WanderSpeedMultiplier => wanderSpeedMult;

    internal void PickWanderDestination()
    {
        WanderDestination();
    }

    internal bool ReachedWanderDestination()
    {
        return WanderDestinationReached();
    }

    internal void ClearWanderDestination()
    {
        hasDest = false;
    }

    //-- ChaseState uses these
    internal float ChaseSpeedMultiplier => chaseSpeedMult;
    internal float ChaseTurnSpeed => chaseTurnSpeed;
    internal float ChaseReactionTime => chaseReactionTime;
    internal float ChaseAccelTime => chaseAccelTime;

    //-- Only knows the live position right after seeing or hearing the player
    internal bool TrackingTarget => Target != null
        && Time.time - LastDetectionTime <= trackingTime;

    internal Vector3 TargetPosition => TrackingTarget
        ? Target.transform.position
        : LastKnownPosition;

    //-- Aim a bit ahead of where the player is going, more the faster they move
    internal Vector3 ChaseAimPoint
    {
        get
        {
            //-- Can't see them, go where they were heading
            if (!TrackingTarget)
                return FollowThroughPoint;

            Vector3 aim = TargetPosition;

            if (targetController == null || maxLeadTime <= 0f)
                return aim;

            Vector3 targetVelocity = targetController.velocity;
            targetVelocity.y = 0f;

            Vector3 offset = aim - transform.position;
            offset.y = 0f;

            float mySpeed = Mathf.Max(entityData.speed.GetCurrent() * chaseSpeedMult, 0.1f);
            float leadTime = Mathf.Min(offset.magnitude / mySpeed, maxLeadTime);

            return aim + targetVelocity * leadTime;
        }
    }

    internal float ChaseStopDistance => chaseStopDistance;

    internal Vector3 FollowThroughPoint => LastKnownPosition + lastKnownHeading * followThroughDistance;

    //-- Up close it can feel the player through walls so corners don't lose them
    internal void SenseNearbyTarget()
    {
        if (Target == null)
            return;

        Vector3 offset = Target.transform.position - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude > senseRadius * senseRadius)
            return;

        LastKnownPosition = Target.transform.position;
        LastDetectionTime = Time.time;
        RememberHeading();
    }

    void RememberHeading()
    {
        if (targetController == null)
            return;

        Vector3 velocity = targetController.velocity;
        velocity.y = 0f;

        lastKnownHeading = velocity.sqrMagnitude > 0.25f ? velocity.normalized : Vector3.zero;
    }

    internal bool LostTarget()
    {
        if (Time.time - LastDetectionTime > loseTargetTime)
            return true;

        Vector3 offset = TargetPosition - transform.position;
        offset.y = 0f;

        return offset.sqrMagnitude > giveUpDistance * giveUpDistance;
    }

    internal void GiveUpChase()
    {
        SearchAt(FollowThroughPoint);

        //-- Search can be on cooldown, so fall back to Idle
        if (state == AIStates.Chase)
            ChangeState(AIStates.Idle);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //=========================================================
            //Entity Instantiation
        //=========================================================
        entityData = Instantiate(entityTemplate);
        entityData.Initialize(gameObject, mesh);

        //-- Use the scene's terrain if none was dragged in
        if (terrain == null)
            terrain = Terrain.activeTerrain;

        //-- Create the states before entering Idle
        EnemyState idleState = new IdleState(this);
        EnemyState wanderState = new WanderState(this);
        EnemyState chaseState = new ChaseState(this);
        EnemyState attackState = new AttackState(this);
        searchState = new SearchState(this);

        states = new Dictionary<AIStates, EnemyState>
        {
            { idleState.Type, idleState },
            { wanderState.Type, wanderState },
            { chaseState.Type, chaseState },
            { attackState.Type, attackState },
            { searchState.Type, searchState }
        };

        //-- Wander stays around where it spawned
        homePosition = transform.position;
        avoidance = GetComponent<ObstacleAvoidance>();

        //=========================================================
            //State Data
        //=========================================================
        OnStateChange();
    }

    //---------------------------------------------------------
    void FixedUpdate()
    {
        entityData?.BeginTick();
        if (entityData.IsHitPaused)
        {
            entityData.ApplyMove(Vector2.zero, false);
            return;
        }
        stateTime += Time.fixedDeltaTime;

        UpdateDecision();
        ProcessState();
    }

    void LateUpdate()
    {
        entityData?.AfterTick();
    }

    void OnDisable()
    {
        entityData?.Disable();
    }

    //---------------------------------------------------------
    public void PlayerDetected(Aspect aspect, Vector3 position, string sense)
    {
        if (aspect == null || aspect.aspectName != Aspect.aspect.Player)
            return;

        //-- Remember where the player was detected
        if (aspect != Target)
            targetController = aspect.GetComponentInParent<CharacterController>();

        Target = aspect;
        LastKnownPosition = position;
        LastSense = sense;
        LastDetectionTime = Time.time;
        RememberHeading();

        //-- Saw them again right after losing them, just keep chasing
        if (sense == "Vision" && state == AIStates.Search && prevState == AIStates.Chase)
        {
            ChangeState(AIStates.Chase);
            return;
        }

        OnPlayerDetected();
    }

    //---------------------------------------------------------
    protected abstract void OnPlayerDetected();

    //---------------------------------------------------------
    protected void Remember(string memory)
    {
        if (brain.Count >= 10)
        {
            brain.Dequeue();
        }

        brain.Enqueue(memory);
        //Debug.Log(brain.Last<string>());
    }

    //---------------------------------------------------------
    protected virtual void ProcessState() //Supports Override
    {
        Vector2 move = currentState != null
            ? currentState.Tick()
            : Vector2.zero;

        switch (state)
        {
            case AIStates.Idle:
                IdleActions();
                break;

            case AIStates.Wander:
                WanderActions();
                break;

            case AIStates.Chase:
                ChaseActions();
                break;

            case AIStates.Retreat:
                RetreatActions();
                break;

            case AIStates.Attack:
                AttackActions();
                break;

            case AIStates.Search:
                SearchActions();
                break;
        }

        if (avoidance != null)
            avoidance.CheckStuck(move, entityData.speed.GetCurrent());

        entityData.ApplyMove(move, false);
    }

    //-- States call this, the ObstacleAvoidance component does the actual work
    internal Vector3 SteerAround(Vector3 desired)
    {
        desired.y = 0f;

        if (avoidance == null)
            return desired;

        return avoidance.SteerAround(desired, Target != null ? Target.transform : null);
    }

    //-- Direction to walk to get to a point, goes around blocks if there's a NavMesh
    internal Vector3 PathTowards(Vector3 point)
    {
        Vector3 next = avoidance != null ? avoidance.NextCorner(point) : point;
        return SteerAround(next - transform.position);
    }

    //---------------------------------------------------------
    protected virtual void IdleActions() //Supports Override
    {

    }

    //---------------------------------------------------------
    protected virtual void ChaseActions() //Supports Override
    {

    }

    //---------------------------------------------------------
    protected virtual void AttackActions() //Supports Override
    {
        
    }

    //---------------------------------------------------------
    protected virtual void RetreatActions() //Supports Override
    {

    }

    //---------------------------------------------------------
    protected virtual void WanderActions() //Supports Override
    {

    }

    //---------------------------------------------------------
    protected virtual void SearchActions() //Supports Override
    {

    }

    //---------------------------------------------------------
    protected void UpdateDecision()
    {
        AIStates nextState = DecideState();

        if (nextState != state)
        {
            if (nextState == AIStates.Search)
                hasDest = false;

            ChangeState(nextState);
        }
    }

    //---------------------------------------------------------
    public bool AttackChooser()
    {
        currentAttack = null;
        currentAttackEntry = null;
        if (Target == null || attacks == null || attacks.Length <= 0) return false; //Omit if no attacks exist or no target exists
        if (IsOnCooldown(AIStates.Attack)) return false;

        float distance = Vector3.Distance(transform.position, Target.transform.position);
        float totalWeight = 0f;

        foreach (var atk in attacks)
        {
            if (atk == null || atk.attack == null || atk.weight <= 0f || atk.attack.range < distance) continue;

            totalWeight += atk.weight;
        }
        if (totalWeight <= 0f) return false;
        float roll = UnityEngine.Random.value * totalWeight;
        float accumulatedWeight = 0f;
        foreach (var atk in attacks)
        {
            if (atk == null || atk.attack == null || atk.weight <= 0f || atk.attack.range < distance) continue;

            accumulatedWeight += atk.weight;

            currentAttack = atk.attack;
            currentAttackEntry = atk;
            if (roll < accumulatedWeight) return true;
        }
        return currentAttack != null;
    }
    //---------------------------------------------------------
    bool DebugAttacks()
    {
        return debugAttacks && DebugHandler.DevMode();
    }
    //---------------------------------------------------------
    protected virtual void OnDrawGizmosSelected()
    {
        if (state == AIStates.Wander && DebugHandler.DevMode()) {
            //-- Cyan circle is the area it wanders in
            Gizmos.color = Color.cyan;
            Vector3 home = Application.isPlaying ? homePosition : transform.position;
            Gizmos.DrawWireSphere(home, wanderRadius);
        }

        if (attacks == null || !DebugAttacks()) return;
        Gizmos.color = Color.gray; //Gray shows all possible attacks

        foreach (var atk in attacks)
        {
            if (atk == null || atk.attack == null) continue;
            float range = atk.attack.range;
            if (range <= 0f) continue;
            Gizmos.DrawWireSphere(transform.position, range);
        }

        if (currentAttack != null && currentAttack.range > 0f)
        {
            Gizmos.color = Color.red; //Red shows the selected attack
            Gizmos.DrawWireSphere(transform.position, currentAttack.range);
        }
    }
    //---------------------------------------------------------
    protected void OnStateChange()
    {
        stateTimer = GetStateTime(state);
        stateTime = 0f;
        Remember(gameObject.name + " switched to the " + state + " State");

        states.TryGetValue(state, out currentState);
        currentState?.Enter();
    }

    //---------------------------------------------------------
    protected void SearchAt(Vector3 position)
    {
        if (state != AIStates.Search && IsOnCooldown(AIStates.Search))
            return;

        wanderDest = position;
        hasDest = true;

        if (state == AIStates.Search)
        {
            stateTime = 0f;
            searchState.Enter();
        }
        else
        {
            ChangeState(AIStates.Search);
        }
    }

    //---------------------------------------------------------
    public StateTimer GetStateTime(AIStates _state)
    {
        if (actionStateTimes.TryGetValue(_state, out StateTimer time))
        {
            return time;
        }

        return null;
    }

    //---------------------------------------------------------
    public bool StateTimeTicked()
    {
        return stateTime >= stateTimer.maxTime;
    }

    //---------------------------------------------------------
    public bool IsOnCooldown(AIStates _state)
    {
        return actionCooldowns.TryGetValue(_state, out Cooldown time)
            && Time.time < time.timer;
    }

    //---------------------------------------------------------
    protected void StartCooldown(AIStates _state)
    {
        if (!actionCooldowns.TryGetValue(_state, out Cooldown cooldown))
            return;

        cooldown.timer = Time.time + Mathf.Max(0f, cooldown.maxTime);
    }

    //---------------------------------------------------------
        //Decision Making
    //---------------------------------------------------------
    protected AIStates DecideState()
    {
        stateScores.Clear();
        CalculateScores();

        var ret = state;
        float highestScore = float.NegativeInfinity;

        foreach (var key in stateScores.Keys)
        {
            bool canChange = key == AIStates.Idle
                ? CanChangeState('U')
                : CanChangeState();

            if (IsOnCooldown(key) || !canChange)
                continue;

            if (stateScores[key] > highestScore)
            {
                highestScore = stateScores[key];
                ret = key;
            }
        }

        return ret;
    }

    //---------------------------------------------------------
    protected virtual void CalculateScores() //Supports Overriding
    {
        currentState?.Score(stateScores);

        //-- Chase ends itself now
        if (state == AIStates.Retreat)
            stateScores[AIStates.Idle] = 100f;
    }

    //---------------------------------------------------------
    protected void WanderDestination()
    {
        //-- Pick a spot around home instead of anywhere on the map
        Vector2 random = UnityEngine.Random.insideUnitCircle * wanderRadius;

        wanderDest = new Vector3(
            homePosition.x + random.x,
            transform.position.y,
            homePosition.z + random.y
        );

        //-- Keep it on the terrain
        if (terrain != null)
        {
            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            float terrainEdgeOffset = 2f;

            float marginX = Mathf.Clamp(terrainEdgeOffset, 0f, size.x * 0.49f);
            float marginZ = Mathf.Clamp(terrainEdgeOffset, 0f, size.z * 0.49f);

            wanderDest.x = Mathf.Clamp(wanderDest.x, origin.x + marginX, origin.x + size.x - marginX);
            wanderDest.z = Mathf.Clamp(wanderDest.z, origin.z + marginZ, origin.z + size.z - marginZ);
        }

        hasDest = true;
        Remember(gameObject.name + " decided to wander towards " + wanderDest);
    }

    //---------------------------------------------------------
    protected bool WanderDestinationReached()
    {
        if (!hasDest)
            return true;

        Vector3 offset = wanderDest - transform.position;
        offset.y = 0;

        //-- 1.5 so it doesn't have to hit the exact spot
        return offset.sqrMagnitude <= 1.5f * 1.5f;
    }

    //---------------------------------------------------------
    protected bool FacingWanderDest()
    {
        Vector3 direction = wanderDest - transform.position;
        direction.y = 0f;

        if (!VerifyVector(direction))
            return true;

        return Vector3.Angle(transform.forward, direction) <= turnThreshold;
    }

    //---------------------------------------------------------
    public bool CanChangeState(char moveType = 'I', bool grounded = true)
    {
        bool moveTypeCheck = moveType == 'U'
            ? true
            : entityData.GetMoveType() == moveType;

        return entityData.grounded == grounded
            && entityData.ctrl
            && moveTypeCheck
            && StateTimeTicked();
    }

    //---------------------------------------------------------
    public void ChangeState(AIStates _state)
    {
        if (IsOnCooldown(_state))
        {
            Debug.LogWarning("State " + _state + " is currently on cooldown");
            return;
        }

        if (_state == state)
            return;

        currentState?.Exit();

        prevState = state;
        StartCooldown(prevState);

        switch (_state)
        {
            case AIStates.Idle:
                entityData.SetMoveType('I');
                entityData.ctrl = true;
                state = _state;
                break;

            case AIStates.Wander:
                entityData.SetMoveType('I');
                entityData.ctrl = true;
                state = _state;
                break;

            case AIStates.Chase:
                entityData.SetMoveType('A');
                entityData.ctrl = true;
                state = _state;
                break;

            case AIStates.Attack:
                entityData.SetMoveType('A');
                entityData.ctrl = false;
                state = _state;
                break;

            case AIStates.Search:
                entityData.SetMoveType('I');
                entityData.ctrl = true;
                state = _state;
                break;

            case AIStates.Retreat:
                entityData.SetMoveType('I');
                entityData.ctrl = true;
                state = _state;
                break;

            case AIStates.Hurt:
                entityData.SetMoveType('H');
                entityData.ctrl = false;
                state = _state;
                break;

            default:
                entityData.SetMoveType('U');
                break;
        }

        OnStateChange();
    }
}