using UnityEngine;
using System.Collections.Generic;

public class SearchState : EnemyState
{
    private Quaternion startRotation;
    private Quaternion targetRotation;

    public SearchState(EnemyAI enemy) : base(enemy)
    {
    }

    public override EnemyAI.AIStates Type => EnemyAI.AIStates.Search;

    public override void Enter()
    {
        //-- Picks a destination if this search was not caused by a sound
        if (!enemy.HasSearchDestination)
            enemy.PickSearchDestination();

        startRotation = enemy.transform.rotation;

        Vector3 direction =
            enemy.SearchDestination - enemy.transform.position;
        direction.y = 0f;

        targetRotation = direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction, Vector3.up)
            : startRotation;
    }

    public override Vector2 Tick()
    {
        //-- Turn toward the search destination
        float progress = enemy.SearchTurnDuration > 0f
            ? Mathf.Clamp01(enemy.stateTime / enemy.SearchTurnDuration)
            : 1f;

        enemy.transform.rotation =
            Quaternion.Slerp(startRotation, targetRotation, progress);

        return Vector2.zero;
    }

    public override void Score(Dictionary<EnemyAI.AIStates, float> scores)
    {
        scores[EnemyAI.AIStates.Wander] = (
            UnityEngine.Random.Range(0f, 10f) +
            (enemy.HasSearchDestination ? 5f : 0f)
        );

        scores[EnemyAI.AIStates.Search] = (
            UnityEngine.Random.Range(0f, 10f) +
            (!enemy.HasSearchDestination ? 5f : 0f)
        );
    }
}