using UnityEngine;

public class BeetleScript : NPC
{
    [SerializeField] private Quests rescueQuest;
    [SerializeField] private Conversations rescuedConversation;
    [SerializeField] private RectTransform waterBarFill;
    [SerializeField] private BoxCollider waterHitbox;

    private bool needsWater;
    private bool rescued;

    public override Conversations GetConversation()
    {
        return rescued ? rescuedConversation : base.GetConversation();
    }

    public override void OnDialogueFinished(Player speaker)
    {
        if (rescued || needsWater) return;

        if (!speaker.ongoingQuests.Contains(rescueQuest) && !speaker.completedQuests.Contains(rescueQuest))
        {
            speaker.StartQuest(rescueQuest);
        }

        needsWater = true;
        //-- make the hitbox cover the whole beetle so its easier to hit
        Bounds bounds = promptBoundsRenderer.bounds;
        Vector3 scale = transform.lossyScale;
        waterHitbox.center = transform.InverseTransformPoint(bounds.center);
        waterHitbox.size = new Vector3(bounds.size.x / scale.x, bounds.size.y / scale.y, bounds.size.z / scale.z);
        waterHitbox.enabled = true;
    }

    protected override bool ShowAlternatePrompt(float distance)
    {
        bool showBar = needsWater && distance <= 8f;
        waterBarFill.parent.gameObject.SetActive(showBar);
        return showBar;
    }

    public bool IsWaterHitbox(Collider other) => other == waterHitbox;

    public void ReceiveWaterShot(float amount)
    {
        if (!needsWater || amount <= 0f) return;

        float capacity = entityData.water.GetMax();
        entityData.water.Set(entityData.water.GetCurrent() + amount);
        float progress = entityData.water.GetCurrent() / capacity;
        waterBarFill.anchorMax = new Vector2(progress, 1f);
        if (progress < 1f) return;

        needsWater = false;
        rescued = true;
        waterHitbox.enabled = false;
        RecordRescue("npc.beetle.saved"); //-- spawned beetles share the same rescue objective
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        UpdateAnim();
    }
    void UpdateAnim()
    {
        var animator = entityData.animator;
        if (animator == null) return;

        animator.SetBool("Hydrated", rescued);
    }
}
