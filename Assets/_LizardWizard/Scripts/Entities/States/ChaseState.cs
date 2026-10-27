using UnityEngine;

public class ChaseState : EnemyState
{
    public ChaseState(EnemyAI enemy) : base(enemy)
    {
    }

    private float currentSpeed;

    public override EnemyAI.AIStates Type => EnemyAI.AIStates.Chase;

    public override void Enter()
    {
        currentSpeed = 0f;
    }

    public override Vector2 Tick()
    {
        //-- Lost the player for too long, go look where they were
        if (enemy.LostTarget())
        {
            enemy.GiveUpChase();
            return Vector2.zero;
        }

        Vector3 toTarget = enemy.TargetPosition - enemy.transform.position;
        toTarget.y = 0f;

        //-- Close enough, just keep facing them
        bool closeEnough = toTarget.magnitude <= enemy.ChaseStopDistance;

        //-- Turn to face them first before running
        bool reacting = enemy.stateTime < enemy.ChaseReactionTime;

        Vector3 aim = closeEnough || reacting
            ? toTarget
            : enemy.ChaseAimPoint - enemy.transform.position;
        aim.y = 0f;

        if (aim.sqrMagnitude > 0.0001f)
        {
            enemy.transform.rotation = Quaternion.RotateTowards(
                enemy.transform.rotation,
                Quaternion.LookRotation(aim, Vector3.up),
                enemy.ChaseTurnSpeed * Time.fixedDeltaTime);
        }

        //-- Speed up and slow down over time instead of snapping
        float targetSpeed = closeEnough || reacting
            ? 0f
            : Mathf.Clamp01(enemy.ChaseSpeedMultiplier);

        float accel = enemy.ChaseAccelTime > 0f
            ? Time.fixedDeltaTime / enemy.ChaseAccelTime
            : 1f;

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel);

        return new Vector2(0f, currentSpeed);
    }
}
