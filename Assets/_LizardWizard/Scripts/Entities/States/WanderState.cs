using UnityEngine;
using System.Collections.Generic;

public class WanderState : EnemyState
{
    public WanderState(EnemyAI enemy) : base(enemy)
    {
    }

    public override EnemyAI.AIStates Type => EnemyAI.AIStates.Wander;

    public override void Enter()
    {
        if (!enemy.HasWanderDestination)
            enemy.PickWanderDestination();
    }

    public override Vector2 Tick()
    {
        if (enemy.ReachedWanderDestination())
        {
            enemy.ClearWanderDestination();
            return Vector2.zero;
        }

        Vector3 offset = enemy.WanderDestinationPoint - enemy.transform.position;
        offset.y = 0;
        float arrivalRadius = 0.5f;
        float mult = Mathf.Clamp01(enemy.WanderSpeedMultiplier);
        float distance = offset.magnitude;

        if (distance <= arrivalRadius)
        {
            enemy.PickWanderDestination();
            return Vector2.zero;
        }

        enemy.transform.rotation =
            Quaternion.LookRotation(offset / distance, Vector3.up); //Instantly look towards destination

        float step =
            enemy.entityData.speed.GetCurrent() * mult * Time.fixedDeltaTime;

        float inputAmount = step > 0f
            ? Mathf.Clamp01(distance / step)
            : 0f;

        return new Vector2(0f, inputAmount * mult);
    }

    public override void Score(Dictionary<EnemyAI.AIStates, float> scores)
    {
        if (enemy.ReachedWanderDestination())
            scores[EnemyAI.AIStates.Idle] = 100f;
    }
}