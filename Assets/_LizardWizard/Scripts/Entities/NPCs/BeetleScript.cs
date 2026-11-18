using UnityEngine;
using UnityEngine.UI;

public class BeetleScript : NPC
{
    [SerializeField] private Quests rescueQuest;
    [SerializeField] private Conversations rescuedConversation;
    [SerializeField] private RectTransform waterBarFill;
    [SerializeField] private RectTransform waterIcon;
    [SerializeField] private BoxCollider waterHitbox;
    [SerializeField, Min(1)] private int shotsNeeded = 5;

    private bool needsWater;
    private bool rescued;
    private int shotsTaken;

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

        //-- line between each chunk so you can tell how many shots are left
        for (int i = 1; i < shotsNeeded; i++)
        {
            var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            divider.SetParent(waterBarFill.parent, false);
            divider.anchorMin = new Vector2((float)i / shotsNeeded, 0f);
            divider.anchorMax = new Vector2((float)i / shotsNeeded, 1f);
            divider.sizeDelta = new Vector2(0.4f, 0f);
            divider.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.8f);
        }
    }

    protected override bool ShowAlternatePrompt(float distance)
    {
        bool showBar = needsWater && distance <= 8f;
        waterBarFill.parent.gameObject.SetActive(showBar);
        if (showBar) waterIcon.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 5f) * 0.12f); //-- pulse so you notice it
        return showBar;
    }

    public bool IsWaterHitbox(Collider other) => other == waterHitbox;

    public void ReceiveWaterShot()
    {
        if (!needsWater) return;

        shotsTaken++;
        float progress = (float)shotsTaken / shotsNeeded;
        entityData.water.Set(entityData.water.GetMax() * progress);
        waterBarFill.anchorMax = new Vector2(progress, 1f);
        if (shotsTaken < shotsNeeded) return;

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
