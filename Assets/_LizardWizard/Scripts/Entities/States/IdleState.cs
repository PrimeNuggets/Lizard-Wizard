using UnityEngine;
using System.Collections.Generic;

public class IdleState : EnemyState
{
    public IdleState(EnemyAI enemy) : base(enemy)
    {
    }

    public override EnemyAI.AIStates Type => EnemyAI.AIStates.Idle;

    public override void Score(Dictionary<EnemyAI.AIStates, float> scores)
    {
        scores[EnemyAI.AIStates.Wander] = (
            UnityEngine.Random.Range(0f, 10f) +
            (!enemy.ReachedWanderDestination() ? 7f : 0f)
        );

        scores[EnemyAI.AIStates.Search] = (
            UnityEngine.Random.Range(0f, 10f) +
            (enemy.ReachedWanderDestination() ? 7f : 0f)
        );

        scores[EnemyAI.AIStates.Idle] = (
            UnityEngine.Random.Range(0f, 10f) +
            (enemy.ReachedWanderDestination() ? 5f : 0f)
        );
    }
}