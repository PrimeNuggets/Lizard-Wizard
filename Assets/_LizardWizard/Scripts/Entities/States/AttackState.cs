using UnityEngine;

public class AttackState : EnemyState
{
    public AttackState(EnemyAI enemy) : base(enemy)
    {
    }

    public override EnemyAI.AIStates Type => EnemyAI.AIStates.Attack;

    public override void Enter()
    {
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
        if (enemy.StateTimeTicked())
        {
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
}
