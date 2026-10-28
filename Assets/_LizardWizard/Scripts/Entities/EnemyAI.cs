using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;

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

    //===========================================
        [Header("Entity Data")]
    //===========================================
    [LabelOverride("Data")] public Entity entityTemplate;
    [NonSerialized] public Entity entityData;

    //===========================================
        [Header("AI Config")]
    //===========================================
    [SerializeField] public Dictionary<AIStates, Cooldown> actionCooldowns = new Dictionary<AIStates, Cooldown>(); //SerializeField & Public cuz apparently Unity doesn't serialize just public for dictionary fields
    [SerializeField] public Dictionary<AIStates, StateTimer> actionStateTimes = new Dictionary<AIStates, StateTimer>();
    [SerializeField, Tooltip("Grace area in degrees for turning")] protected float turnThreshold = 3f;
    [SerializeField, LabelOverride("Wander Speed Multiplier")] protected float wanderSpeedMult = 0.25f;
    [SerializeField] protected float searchTurnTime = 1f;
    [SerializeField, LabelOverride("Search Speed Multiplier")] protected float searchSpeedMult = 0.5f;

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

    public Aspect Target { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }
    public string LastSense { get; private set; }
    public float LastDetectionTime { get; private set; }

    //-- SearchState uses these without changing the Inspector fields
    internal bool HasSearchDestination => hasDest;
    internal Vector3 SearchDestination => wanderDest;
    internal float SearchTurnDuration => searchTurnTime;
    internal float SearchSpeedMultiplier => searchSpeedMult;

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
            Vector3 aim = TargetPosition;

            if (!TrackingTarget || targetController == null || maxLeadTime <= 0f)
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
        SearchAt(LastKnownPosition);

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
        searchState = new SearchState(this);

        states = new Dictionary<AIStates, EnemyState>
        {
            { idleState.Type, idleState },
            { wanderState.Type, wanderState },
            { chaseState.Type, chaseState },
            { searchState.Type, searchState }
        };

        //=========================================================
            //State Data
        //=========================================================
        OnStateChange();
    }

    //---------------------------------------------------------
    void FixedUpdate()
    {
        entityData?.BeginTick();
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
        Debug.Log(brain.Last<string>());
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

        entityData.ApplyMove(move, false);
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
        if (state == AIStates.Attack || state == AIStates.Retreat)
            stateScores[AIStates.Idle] = 100f;
    }

    //---------------------------------------------------------
    protected void WanderDestination()
    {
        if (terrain == null)
        {
            Debug.LogWarning(gameObject.name + " has no terrain to wander on");
            hasDest = false;
            return;
        }

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float terrainEdgeOffset = 2f;

        float marginX = Mathf.Clamp(terrainEdgeOffset, 0f, size.x * 0.49f);
        float marginZ = Mathf.Clamp(terrainEdgeOffset, 0f, size.z * 0.49f);

        wanderDest = new Vector3(
            UnityEngine.Random.Range(
                origin.x + marginX,
                origin.x + size.x - marginX
            ),
            transform.position.y,
            UnityEngine.Random.Range(
                origin.z + marginZ,
                origin.z + size.z - marginZ
            )
        );

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

        return offset.sqrMagnitude <= 0.5f * 0.5f;
    }

    //---------------------------------------------------------
    protected bool FacingWanderDest()
    {
        Vector3 direction = wanderDest - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001)
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