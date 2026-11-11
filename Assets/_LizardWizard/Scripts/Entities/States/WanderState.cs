using UnityEngine;
using System.Collections.Generic;

public class WanderState : EnemyState
{
    private float timer;

    public WanderState(EnemyAI enemy) : base(enemy)
    {
    }

    public override EnemyAI.AIStates Type => EnemyAI.AIStates.Wander;

    public override void Enter()
    {
        if (!enemy.HasWanderDestination)
            enemy.PickWanderDestination();

        timer = 0f;
    }

    public override Vector2 Tick()
    {
        timer += Time.fixedDeltaTime;

        //-- Taking too long, give up on this spot
        if (enemy.ReachedWanderDestination() || timer > 10f)
        {
            enemy.ClearWanderDestination();
            return Vector2.zero;
        }

        Vector3 offset = enemy.WanderDestinationPoint - enemy.transform.position;
        offset.y = 0;
        float arrivalRadius = 1.5f;
        float mult = Mathf.Clamp01(enemy.WanderSpeedMultiplier);
        float distance = offset.magnitude;

        if (distance <= arrivalRadius)
        {
            enemy.PickWanderDestination();
            return Vector2.zero;
        }

        //-- Turn smoothly now since steering can change the direction every frame
        Vector3 direction = enemy.PathTowards(enemy.WanderDestinationPoint);

        if (direction.sqrMagnitude > 0.0001f)
        {
            enemy.transform.rotation = Quaternion.RotateTowards(
                enemy.transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                enemy.ChaseTurnSpeed * Time.fixedDeltaTime);
        }

        float step =
            enemy.entityData.speed.GetCurrent() * mult * Time.fixedDeltaTime;

        float inputAmount = step > 0f
            ? Mathf.Clamp01(distance / step)
            : 0f;

        //-- Turn first then walk, otherwise it circles the spot
        float facing = Mathf.Clamp01(Vector3.Dot(enemy.transform.forward, direction.normalized));

        return new Vector2(0f, inputAmount * mult * facing);
    }

    public override void Score(Dictionary<EnemyAI.AIStates, float> scores)
    {
        if (enemy.ReachedWanderDestination())
            scores[EnemyAI.AIStates.Idle] = 100f;
    }
}