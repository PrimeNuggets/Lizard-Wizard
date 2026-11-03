using UnityEngine;
using System.Collections.Generic;

public class SearchState : EnemyState
{
    private Quaternion startRotation;
    private Quaternion targetRotation;

    //-- Investigating = something sent it here (lost the player or heard a noise)
    private bool investigating;
    private bool done;
    private Vector3 center;
    private Vector3 spot;
    private int spotsLeft;
    private bool lookingAround;
    private float timer;
    private float lookYaw;

    public SearchState(EnemyAI enemy) : base(enemy)
    {
    }

    public override EnemyAI.AIStates Type => EnemyAI.AIStates.Search;

    public override void Enter()
    {
        investigating = enemy.HasSearchDestination;
        done = false;

        if (investigating)
        {
            center = enemy.SearchDestination;
            spot = center;
            spotsLeft = enemy.SearchPoints;
            lookingAround = false;
            timer = 0f;
            return;
        }

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
        {
            if (done)
                return Vector2.zero;

            timer += Time.fixedDeltaTime;
            return lookingAround ? LookAround() : GoToSpot();
        }

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
        Vector3 offset = spot - enemy.transform.position;
        offset.y = 0f;

        //-- Got there (or took too long), look around
        if (offset.magnitude <= 1.5f || timer > 8f)
        {
            lookingAround = true;
            timer = 0f;
            lookYaw = enemy.transform.eulerAngles.y;
            return Vector2.zero;
        }

        Vector3 direction = enemy.PathTowards(spot);

        if (direction.sqrMagnitude > 0.0001f)
        {
            enemy.transform.rotation = Quaternion.RotateTowards(
                enemy.transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                enemy.ChaseTurnSpeed * Time.fixedDeltaTime);
        }

        //-- Turn first then walk so it doesn't circle the spot
        float facing = Mathf.Clamp01(Vector3.Dot(enemy.transform.forward, direction.normalized));

        return new Vector2(0f, Mathf.Clamp01(enemy.SearchSpeedMultiplier) * facing);
    }

    Vector2 LookAround()
    {
        float duration = enemy.LookAroundTime;

        if (timer >= duration)
        {
            if (spotsLeft <= 0)
            {
                //-- Checked everything, give up
                done = true;
                enemy.ClearWanderDestination();
                return Vector2.zero;
            }

            //-- Go check another spot nearby
            spotsLeft--;
            Vector2 random = Random.insideUnitCircle * enemy.SearchRadius;
            spot = center + new Vector3(random.x, 0f, random.y);
            lookingAround = false;
            timer = 0f;
            return Vector2.zero;
        }

        //-- Look right, then left, then back to the middle
        float sweep = Mathf.Sin(timer / duration * Mathf.PI * 2f) * enemy.LookAroundAngle;
        enemy.transform.rotation = Quaternion.Euler(0f, lookYaw + sweep, 0f);

        return Vector2.zero;
    }

    public override void Score(Dictionary<EnemyAI.AIStates, float> scores)
    {
        //-- Stay until it's done checking the area
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
