using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractionsManager : MonoBehaviour
{
    // Only one instance of this script is needed in any given scene
    public enum ParticipantKindRequirement
    {
        Any,
        Hazard,
        Projectile
    }

    public enum StatusRequirement
    {
        Any,
        On,
        Off
    }

    enum ParticipantKind
    {
        Hazard,
        Projectile
    }

    [Serializable]
    public class TrapInteractionRule
    {
        [Tooltip("Optional label to make this rule easier to identify in the inspector.")]
        public string interactionName;

        [Header("First Participant")]
        [Tooltip("Trap type for the first participant. Leave as None to match any trap type.")]
        public Hazard.TrapType firstTrapType = Hazard.TrapType.None;
        public ParticipantKindRequirement firstParticipantKind = ParticipantKindRequirement.Any;
        public StatusRequirement firstOnFire = StatusRequirement.Any;
        public StatusRequirement firstOnPoison = StatusRequirement.Any;

        [Header("Second Participant")]
        [Tooltip("Trap type for the second participant. Leave as None to match any trap type.")]
        public Hazard.TrapType secondTrapType = Hazard.TrapType.None;
        public ParticipantKindRequirement secondParticipantKind = ParticipantKindRequirement.Any;
        public StatusRequirement secondOnFire = StatusRequirement.Any;
        public StatusRequirement secondOnPoison = StatusRequirement.Any;

        [Header("Outcome")]
        [Tooltip("When enabled, the rule can match regardless of which participant reports the collision first.")]
        public bool matchInEitherOrder = true;
        [Tooltip("Prefab to spawn at the midpoint between both colliders.")]
        public GameObject collisionFxPrefab;
        public bool destroyFirstParticipant = true;
        public bool destroySecondParticipant = true;
    }

    struct InteractionParticipant
    {
        public ParticipantKind kind;
        public Hazard.TrapType trapType;
        public bool onFire;
        public bool onPoison;
        public GameObject gameObject;
        public Collider2D collider;
        public Projectile projectile;
        public HazardManager hazard;
    }

    static InteractionsManager instance;

    [SerializeField] List<TrapInteractionRule> trapInteractions = new List<TrapInteractionRule>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Multiple InteractionsManager instances found. Using the first enabled instance.", this);
            return;
        }

        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static bool TryResolveProjectileHazardInteraction(Projectile projectile, Collider2D hazardCollider, HazardManager hazard)
    {
        if (projectile == null || hazard == null)
        {
            return false;
        }

        return TryResolveInteraction(CreateProjectileParticipant(projectile), CreateHazardParticipant(hazard, hazardCollider));
    }

    public static bool TryResolveHazardHazardInteraction(HazardManager firstHazard, Collider2D firstCollider, HazardManager secondHazard, Collider2D secondCollider)
    {
        if (firstHazard == null || secondHazard == null || firstHazard == secondHazard)
        {
            return false;
        }

        return TryResolveInteraction(CreateHazardParticipant(firstHazard, firstCollider), CreateHazardParticipant(secondHazard, secondCollider));
    }

    static bool TryResolveInteraction(InteractionParticipant firstParticipant, InteractionParticipant secondParticipant)
    {
        InteractionsManager manager = GetInstance();
        if (manager == null)
        {
            return false;
        }

        return manager.ResolveInteraction(firstParticipant, secondParticipant);
    }

    static InteractionsManager GetInstance()
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<InteractionsManager>();
        }

        return instance;
    }

    bool ResolveInteraction(InteractionParticipant firstParticipant, InteractionParticipant secondParticipant)
    {
        if (!TryFindMatchingRule(firstParticipant, secondParticipant, out TrapInteractionRule matchingRule, out bool matchedInOriginalOrder))
        {
            return false;
        }

        InteractionParticipant ruleFirstParticipant = matchedInOriginalOrder ? firstParticipant : secondParticipant;
        InteractionParticipant ruleSecondParticipant = matchedInOriginalOrder ? secondParticipant : firstParticipant;

        SpawnCollisionFx(matchingRule.collisionFxPrefab, ruleFirstParticipant, ruleSecondParticipant);

        if (matchingRule.destroyFirstParticipant)
        {
            DestroyParticipant(ruleFirstParticipant);
        }

        if (matchingRule.destroySecondParticipant)
        {
            DestroyParticipant(ruleSecondParticipant);
        }

        return true;
    }

    bool TryFindMatchingRule(InteractionParticipant firstParticipant, InteractionParticipant secondParticipant, out TrapInteractionRule matchingRule, out bool matchedInOriginalOrder)
    {
        foreach (TrapInteractionRule rule in trapInteractions)
        {
            if (rule == null)
            {
                continue;
            }

            if (MatchesRule(rule, firstParticipant, secondParticipant))
            {
                matchingRule = rule;
                matchedInOriginalOrder = true;
                return true;
            }

            if (rule.matchInEitherOrder && MatchesRule(rule, secondParticipant, firstParticipant))
            {
                matchingRule = rule;
                matchedInOriginalOrder = false;
                return true;
            }
        }

        matchingRule = null;
        matchedInOriginalOrder = true;
        return false;
    }

    static bool MatchesRule(TrapInteractionRule rule, InteractionParticipant firstParticipant, InteractionParticipant secondParticipant)
    {
        return MatchesTrapType(rule.firstTrapType, firstParticipant.trapType)
            && MatchesKind(rule.firstParticipantKind, firstParticipant.kind)
            && MatchesStatus(rule.firstOnFire, firstParticipant.onFire)
            && MatchesStatus(rule.firstOnPoison, firstParticipant.onPoison)
            && MatchesTrapType(rule.secondTrapType, secondParticipant.trapType)
            && MatchesKind(rule.secondParticipantKind, secondParticipant.kind)
            && MatchesStatus(rule.secondOnFire, secondParticipant.onFire)
            && MatchesStatus(rule.secondOnPoison, secondParticipant.onPoison);
    }

    static bool MatchesTrapType(Hazard.TrapType requiredType, Hazard.TrapType currentType)
    {
        return requiredType == Hazard.TrapType.None || requiredType == currentType;
    }

    static bool MatchesKind(ParticipantKindRequirement requiredKind, ParticipantKind currentKind)
    {
        switch (requiredKind)
        {
            case ParticipantKindRequirement.Hazard:
                return currentKind == ParticipantKind.Hazard;
            case ParticipantKindRequirement.Projectile:
                return currentKind == ParticipantKind.Projectile;
            default:
                return true;
        }
    }

    static bool MatchesStatus(StatusRequirement requirement, bool currentValue)
    {
        switch (requirement)
        {
            case StatusRequirement.On:
                return currentValue;
            case StatusRequirement.Off:
                return !currentValue;
            default:
                return true;
        }
    }

    static InteractionParticipant CreateProjectileParticipant(Projectile projectile)
    {
        return new InteractionParticipant
        {
            kind = ParticipantKind.Projectile,
            trapType = projectile.SourceHazard != null ? projectile.SourceHazard.type : Hazard.TrapType.None,
            onFire = projectile.IsOnFire,
            onPoison = projectile.IsOnPoison,
            gameObject = projectile.gameObject,
            collider = projectile.GetComponent<Collider2D>(),
            projectile = projectile
        };
    }

    static InteractionParticipant CreateHazardParticipant(HazardManager hazard, Collider2D collider)
    {
        return new InteractionParticipant
        {
            kind = ParticipantKind.Hazard,
            trapType = hazard.hazardData != null ? hazard.hazardData.type : Hazard.TrapType.None,
            onFire = false,
            onPoison = false,
            gameObject = hazard.gameObject,
            collider = collider,
            hazard = hazard
        };
    }

    static void SpawnCollisionFx(GameObject collisionFxPrefab, InteractionParticipant firstParticipant, InteractionParticipant secondParticipant)
    {
        if (collisionFxPrefab == null)
        {
            return;
        }

        Vector3 firstPosition = firstParticipant.collider != null ? firstParticipant.collider.bounds.center : firstParticipant.gameObject.transform.position;
        Vector3 secondPosition = secondParticipant.collider != null ? secondParticipant.collider.bounds.center : secondParticipant.gameObject.transform.position;
        Vector3 spawnPosition = (firstPosition + secondPosition) * 0.5f;

        Instantiate(collisionFxPrefab, spawnPosition, Quaternion.identity);
    }

    static void DestroyParticipant(InteractionParticipant participant)
    {
        if (participant.projectile != null)
        {
            participant.projectile.Consume();
            return;
        }

        if (participant.hazard != null)
        {
            Destroy(participant.hazard.gameObject);
            return;
        }

        if (participant.gameObject != null)
        {
            Destroy(participant.gameObject);
        }
    }
}
