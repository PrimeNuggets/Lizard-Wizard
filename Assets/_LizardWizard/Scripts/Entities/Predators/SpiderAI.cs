using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(SpiderHearing))]
[RequireComponent(typeof(SpiderVision))]
[RequireComponent(typeof(Aspect))]
[RequireComponent(typeof(CharacterController))]
public class SpiderAI : MonoBehaviour
{
    public enum AIStates
    {
        Idle,
        Wander,
        Chase,
        Pounce,
        Retreat,
        Search, //Spider takes time to view it's surroundings
        Hurt,
    }
    [Serializable]
    public class Cooldown //How much time must pass before the state can be re-entered
    {
        [LabelOverride("Time")] public float maxTime = 2f;
        [NonSerialized] public float timer = 0f;
    }
    [Serializable]
    public class StateTimer
    {
        [LabelOverride("Time")] public float maxTime = 40f;
        [NonSerialized] public float timer = 0f;
    }
    //===========================================
    [Header("Entity Data")]
    [LabelOverride("Data")] public Entity entityTemplate;
    [NonSerialized] public Entity entityData;
    //===========================================
    [Header("AI Config")]
    [SerializeField] public Dictionary<AIStates, Cooldown> actionCooldowns = new Dictionary<AIStates, Cooldown>(); //SerializeField & Public cuz apparently Unity doesn't serialize just public for dictionary fields
    [SerializeField] public Dictionary<AIStates, StateTimer> actionStateTimes = new Dictionary<AIStates, StateTimer>();
    [SerializeField, Tooltip("Grace area in degrees for turning")] public float turnThreshold = 3f;
    //===========================================
    [Header("Misc")]
    [SerializeField] private Terrain terrain;
    //===========================================
        //Non-Serialized
    //===========================================
    [NonSerialized] public AIStates state = AIStates.Idle;
    [NonSerialized] public AIStates prevState = AIStates.Idle;
    [NonSerialized] public StateTimer stateTimer;
    [NonSerialized] public float stateTime;
    [NonSerialized] public Dictionary<AIStates, float> stateScores = new Dictionary<AIStates, float>();
    private Vector3 wanderDest;
    private bool hasDest;
    private Quaternion searchStartRot;
    private Quaternion searchTargetRot;

    //=========================================================
        //General Methods
    //=========================================================
    void Start()
    {
        // -- Start in Idle
        //Debug.Log("Spider is Idle");
        //=========================================================
            //Entity Instantiation
        //=========================================================
        entityData = Instantiate(entityTemplate);
        entityData.Initialize(gameObject, terrain);
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
        ProcessState();
    }
    //---------------------------------------------------------
    void ProcessState()
    {
        switch (state)
        {
            case AIStates.Idle:
                entityData.ApplyMove(Vector2.zero, false); //To ensure gravity is applyed
                break;
            case AIStates.Wander:
                if (!WanderDestinationReached())
                    entityData.ApplyMove(GetWanderInput(), false);
                else
                    hasDest = false;
                break;
            case AIStates.Chase:
                entityData.ApplyMove(Vector2.zero, false);
                break;
            case AIStates.Retreat:
                entityData.ApplyMove(Vector2.zero, false);
                break;
            case AIStates.Pounce:
                entityData.ApplyMove(Vector2.zero, false);
                break;
            case AIStates.Search:
                entityData.ApplyMove(Vector2.zero, false);
                TurnToWander();
                break;
        }

        var next_state = DecideState();
        switch (next_state)
        {
            case AIStates.Idle:
                if (CanChangeState('U'))
                {
                    ChangeState(next_state);
                }
                break;
            case AIStates.Wander:
                if (CanChangeState())
                {
                    ChangeState(next_state);
                }
                break;
            case AIStates.Chase:
                if (CanChangeState())
                {
                    ChangeState(next_state);
                }
                break;
            case AIStates.Retreat:
                if (CanChangeState())
                {
                    ChangeState(next_state);
                }
                break;
            case AIStates.Pounce:
                if (CanChangeState())
                {
                    ChangeState(next_state);
                }
                break;
            case AIStates.Search:
                if (CanChangeState())
                {
                    ChangeState(next_state);
                }
                break;
        }
    }
    //---------------------------------------------------------
    void OnStateChange()
    {
        stateTimer = GetStateTime(state);
        stateTime = 0f;
        Debug.Log("Current State: " + state);
        switch (state)
        {
            case AIStates.Idle:
                break;
            case AIStates.Wander:
                if (!hasDest)
                {
                    WanderDestination();
                }
                break;
            case AIStates.Chase:
                break;
            case AIStates.Retreat:
                break;
            case AIStates.Pounce:
                break;
            case AIStates.Search:
                WanderDestination();

                searchStartRot = transform.rotation;

                Vector3 direction = wanderDest - transform.position;
                direction.y = 0f;

                searchTargetRot = direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction, Vector3.up) : searchStartRot;
                break;
        }
    }
    //---------------------------------------------------------
    public StateTimer GetStateTime(AIStates _state)
    {
        if (actionStateTimes.TryGetValue(_state, out StateTimer time)) {
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
        return actionCooldowns.TryGetValue(_state, out Cooldown time) && Time.time < time.timer;
    }
    //---------------------------------------------------------
    public void StartCooldown(AIStates _state)
    {
        actionCooldowns[_state].timer = Time.time + Mathf.Max(0f, actionCooldowns[_state].maxTime);
    }
    //---------------------------------------------------------
    void TurnToWander()
    {
        if (!hasDest)
            return;
        float duration = stateTimer.maxTime;

        float progress = duration > 0f ? Mathf.Clamp01(stateTime / duration) : 1f;
        
        transform.rotation = Quaternion.Slerp(searchStartRot, searchTargetRot, progress);
    }
    //---------------------------------------------------------
        //Decision Making
    //---------------------------------------------------------
    AIStates DecideState()
    {
        var ret = state;
        stateScores.Clear();
        switch (state)
        {
            case AIStates.Idle:
                stateScores[AIStates.Wander] = (
                    UnityEngine.Random.Range(0f, 10f) +
                    (!WanderDestinationReached() ? 7f : 0f)
                );

                stateScores[AIStates.Search] = (
                    UnityEngine.Random.Range(0f, 10f) +
                    (WanderDestinationReached() ? 7f : 0f)
                );
                stateScores[AIStates.Idle] = (
                    UnityEngine.Random.Range(0f, 10f) +
                    (WanderDestinationReached() ? 5f : 0f)
                );
                break;
            case AIStates.Wander:
                if (WanderDestinationReached())
                {
                    stateScores[AIStates.Idle] = 100f;
                }
                break;
            case AIStates.Search:
                stateScores[AIStates.Wander] = (
                    UnityEngine.Random.Range(0f, 10f) +
                    (hasDest ? 5f : 0f)
                );

                stateScores[AIStates.Search] = (
                    UnityEngine.Random.Range(0f, 10f) +
                    (!hasDest ? 5f : 0f)
                );
                break;
            case AIStates.Pounce:
                stateScores[AIStates.Idle] = 100f;
                break;
            case AIStates.Retreat:
                stateScores[AIStates.Idle] = 100f;
                break;
        }
        float highestScore = float.NegativeInfinity;
        foreach (var key in stateScores.Keys)
        {
            bool canChange = key == AIStates.Idle ? CanChangeState('U') : CanChangeState();
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
    void WanderDestination()
    {
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float terrainEdgeOffset = 2f;

        float marginX = Mathf.Clamp(terrainEdgeOffset, 0f, size.x * 0.49f);
        float marginZ = Mathf.Clamp(terrainEdgeOffset, 0f, size.z * 0.49f);

        wanderDest = new Vector3(
            UnityEngine.Random.Range(origin.x + marginX, origin.x + size.x - marginX),
            transform.position.y,
            UnityEngine.Random.Range(origin.z + marginZ, origin.z + size.z - marginZ)
        );
        hasDest = true;
    }
    //---------------------------------------------------------
    Vector2 GetWanderInput()
    {
        Vector3 offset = wanderDest - transform.position;
        offset.y = 0;
        float arrivalRadius = 0.5f;

        float distance = offset.magnitude;
        if (distance <= arrivalRadius)
        {
            WanderDestination();
            return Vector2.zero;
        }

        transform.rotation = Quaternion.LookRotation(offset / distance, Vector3.up); //Instantly look towards destination

        float step = entityData.speed.GetCurrent() * Time.fixedDeltaTime; //distance / frame

        float inputAmount = step > 0f ? Mathf.Clamp01(distance / step) : 0f;
        return new Vector2(0f, inputAmount);
    }
    //---------------------------------------------------------
    bool WanderDestinationReached()
    {
        if (!hasDest)
            return true;
        Vector3 offset = wanderDest - transform.position;
        offset.y = 0;
        return offset.sqrMagnitude <= 0.5f * 0.5f;
    }
    //---------------------------------------------------------
    bool FacingWanderDest()
    {
        Vector3 direction = wanderDest - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001) 
            return true;
        
        return Vector3.Angle(transform.forward, direction) <= turnThreshold;
    }
    //---------------------------------------------------------
    public void PlayerDetected(Aspect aspect, string sense)
    {
        // -- Only react while state change is possible
        if (!CanChangeState())
            return;

        // -- Check for Player aspect
        if (aspect != null && aspect.aspectName == Aspect.aspect.Player)
        {
            Debug.Log("Player detected by " + sense);
            ChooseReaction();
        }
    }
    //---------------------------------------------------------
    void ChooseReaction()
    {
        // -- Random roll from 0 to 1
        float roll = UnityEngine.Random.Range(0f, 1f);

        Debug.Log("Random Roll: " + roll);

        // -- 75% Pounce
        if (roll < 0.75f)
        {
            ChangeState(AIStates.Chase);
        }
        // -- 25% Retreat
        else
        {
            ChangeState(AIStates.Retreat);
        }

        //timer = 0f;
    }
    //---------------------------------------------------------
    public bool CanChangeState(char moveType = 'I', bool grounded = true)
    {
        bool moveTypeCheck = moveType == 'U' ? true : entityData.GetMoveType() == moveType;
        return entityData.grounded == grounded && entityData.ctrl && moveTypeCheck && StateTimeTicked();
    }
    //---------------------------------------------------------
    public void ChangeState(AIStates _state)
    {
        if (IsOnCooldown(_state)) {
            Debug.LogWarning("State " + _state + " is currently on cooldown");
            return;
        }
        if (_state == state)
            return;
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
            case AIStates.Pounce:
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