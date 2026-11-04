using System.Linq;
using UnityEngine;

public class AttackState : EnemyState
{
    private readonly Movement.Physics physics = new Movement.Physics();
    private bool endsOnLanding;
    private bool airborne;
    public AttackState(EnemyAI enemy) : base(enemy)
    {
    }

    public override EnemyAI.AIStates Type => EnemyAI.AIStates.Attack;

    public override void Enter()
    {
        airborne = false;
        endsOnLanding = false;
        if (enemy.Target != null)
        {
            Vector3 direction = enemy.Target.transform.position - enemy.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Vector3 offsetDirection = Quaternion.AngleAxis(enemy.currentAttack.facingOffset, Vector3.up) * direction.normalized;
                enemy.entityData.SetAttackFacing(offsetDirection);
            }
        }
        if (enemy.currentAttack.flags.Contains("jump"))
        {
            endsOnLanding = enemy.currentAttack.flags.Contains("no_animtime");
            var gravity = physics.applyGravity(0f, 1f);
            enemy.entityData.PlayJump(gravity);

            if (enemy.Target != null)
            {
                AimJump(enemy.Target.transform.position, gravity);
            }
        }

        enemy.entityData.animator.SetBool(enemy.currentAttackEntry.animatorBool, true);

        //Change state times & cooldowns depending on the attack
        if (!enemy.actionStateTimes.TryGetValue(EnemyAI.AIStates.Attack, out var timer))
        {
            timer = new EnemyAI.StateTimer();
            enemy.actionStateTimes[EnemyAI.AIStates.Attack] = timer;
        }
        timer.maxTime = enemy.currentAttack.anim.length;
        enemy.stateTimer = timer;
        
        if (!enemy.actionCooldowns.TryGetValue(EnemyAI.AIStates.Attack, out var cooldown))
        {
            cooldown = new EnemyAI.Cooldown();
            enemy.actionCooldowns[EnemyAI.AIStates.Attack] = cooldown;
        }
        cooldown.maxTime = enemy.currentAttack.cooldown;

        enemy.prevAttack = enemy.currentAttack.attackId;
    }
    public override Vector2 Tick()
    {
        bool finished;
        if (endsOnLanding)
        {
            bool grounded = enemy.entityData.entChar.isGrounded;
            if (!grounded) airborne = true;

            finished = airborne && grounded;
        } else
        {
            finished = enemy.StateTimeTicked();
        }

        if (finished)
        {
            enemy.entityData.animator.SetBool(enemy.currentAttackEntry.animatorBool, false);

            if (!enemy.IsOnCooldown(EnemyAI.AIStates.Chase))
            {
                enemy.ChangeState(EnemyAI.AIStates.Chase);
            } else if (!enemy.IsOnCooldown(EnemyAI.AIStates.Idle))
            {
                enemy.ChangeState(EnemyAI.AIStates.Idle);
            }
        }
        return base.Tick(); //preserve the previous state's movement
    }
    public override void Exit()
    {
        enemy.entityData.attackFacingLocked = false;
        enemy.entityData.animator.SetBool(enemy.currentAttackEntry.animatorBool, false);
    }
    private void AimJump(Vector3 targetPosition, float gravity)
    {
        CharacterController attackerController = enemy.entityData.entChar;

        CharacterController targetController = enemy.Target.GetComponentInParent<CharacterController>();

        Vector3 landingPoint = targetController != null ? targetController.bounds.center : targetPosition;

        float landingHeight = targetController != null ? targetController.bounds.max.y : targetPosition.y;

        float startingHeight = attackerController.bounds.min.y;
        float heightDifference = landingHeight - startingHeight;

        float gravityStrength = Mathf.Abs(gravity);
        if (gravityStrength < 0.0001f) return;

        // Ensure the arc clears the target's top by at least 0.25 units.
        float minimumRise = Mathf.Max(0f, heightDifference) + 0.25f;

        float verticalSpeed = Mathf.Max(enemy.entityData.velocity.y, Mathf.Sqrt(2f * gravityStrength * minimumRise));

        enemy.entityData.velocity.y = verticalSpeed;

        // Descending time at which the spider's feet reach target height.
        float discriminant = (verticalSpeed * verticalSpeed) - (2f * gravityStrength * heightDifference);

        float flightTime = (verticalSpeed + Mathf.Sqrt(Mathf.Max(0f, discriminant))) / gravityStrength;

        Vector3 horizontalDistance = landingPoint - attackerController.bounds.center;

        horizontalDistance.y = 0f;

        Vector3 horizontalVelocity = horizontalDistance / Mathf.Max(flightTime, 0.001f);

        enemy.entityData.velocity.x = horizontalVelocity.x;
        enemy.entityData.velocity.z = horizontalVelocity.z;
    }
}
