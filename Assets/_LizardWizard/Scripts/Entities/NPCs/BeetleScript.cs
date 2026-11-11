using UnityEngine;

public class BeetleScript : NPC
{
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        UpdateAnim();
    }
    void UpdateAnim()
    {
        var animator = entityData.animator;
        if (animator == null) return;

        animator.SetBool("Hydrated", entityData.water.GetCurrent() > 0);
    }
}
