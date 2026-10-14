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

    //===========================================
        [Header("Misc")]
    //===========================================
    [SerializeField] private Terrain terrain;

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

    public Aspect Target { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }
    public string LastSense { get; private set; }
    public float LastDetectionTime { get; private set; }

    //-- SearchState uses these without changing the Inspector fields
    internal bool HasSearchDestination => hasDest;
    internal Vector3 SearchDestination => wanderDest;
    internal float SearchTurnDuration => searchTurnTime;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //=========================================================
            //Entity Instantiation
        //=========================================================
        entityData = Instantiate(entityTemplate);
        entityData.Initialize(gameObject, terrain);

        //-- Create the states before entering Idle
        EnemyState idleState = new IdleState(this);
        EnemyState wanderState = new WanderState(this);
        searchState = new SearchState(this);

        states = new Dictionary<AIStates, EnemyState>
        {
            { idleState.Type, idleState },
            { wanderState.Type, wanderState },
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
        entityData.BeginTick();
        stateTime += Time.fixedDeltaTime;

        UpdateDecision();
        ProcessState();
    }

    //---------------------------------------------------------
    public void PlayerDetected(Aspect aspect, Vector3 position, string sense)
    {
        if (aspect == null || aspect.aspectName != Aspect.aspect.Player)
            return;

        //-- Remember where the player was detected
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
        //-- TODO: Chase doesn't move yet, so they just stand there. We should make a ChaseState that goes after LastKnownPosition, kinda like Wander but faster
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
        actionCooldowns[_state].timer =
            Time.time + Mathf.Max(0f, actionCooldowns[_state].maxTime);
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

        if (state == AIStates.Attack || state == AIStates.Retreat || state == AIStates.Chase)
            stateScores[AIStates.Idle] = 100f;
    }

    //---------------------------------------------------------
    protected void WanderDestination()
    {
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