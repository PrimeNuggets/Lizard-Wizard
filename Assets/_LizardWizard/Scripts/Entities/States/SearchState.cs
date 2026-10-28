using UnityEngine;
using System.Collections.Generic;

public class SearchState : EnemyState
{
    private Quaternion startRotation;
    private Quaternion targetRotation;

    //-- WIP: when something sent it here (lost the player or heard a noise) it just walks to the spot for now
    //-- TODO: look around once it gets there and check a few spots nearby
    private bool investigating;
    private bool done;

    public SearchState(EnemyAI enemy) : base(enemy)
    {
    }

    public override EnemyAI.AIStates Type => EnemyAI.AIStates.Search;

    public override void Enter()
    {
        investigating = enemy.HasSearchDestination;
        done = false;

        if (investigating)
            return;

        //-- Nothing to check, just pick a random direction to look at
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
        if (investigating)
            return GoToSpot();

        //-- Turn toward the search destination
        float progress = enemy.SearchTurnDuration > 0f
            ? Mathf.Clamp01(enemy.stateTime / enemy.SearchTurnDuration)
            : 1f;

        enemy.transform.rotation =
            Quaternion.Slerp(startRotation, targetRotation, progress);

        return Vector2.zero;
    }

    Vector2 GoToSpot()
    {
        if (done)
            return Vector2.zero;

        Vector3 offset = enemy.SearchDestination - enemy.transform.position;
        offset.y = 0f;

        //-- Got there (or took too long), stop searching
        if (offset.magnitude <= 0.75f || enemy.stateTime > 10f)
        {
            done = true;
            enemy.ClearWanderDestination();
            return Vector2.zero;
        }

        enemy.transform.rotation = Quaternion.RotateTowards(
            enemy.transform.rotation,
            Quaternion.LookRotation(offset, Vector3.up),
            enemy.ChaseTurnSpeed * Time.fixedDeltaTime);

        return new Vector2(0f, Mathf.Clamp01(enemy.SearchSpeedMultiplier));
    }

    public override void Score(Dictionary<EnemyAI.AIStates, float> scores)
    {
        if (investigating)
        {
            if (done)
                scores[EnemyAI.AIStates.Idle] = 100f;

            return;
        }

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
