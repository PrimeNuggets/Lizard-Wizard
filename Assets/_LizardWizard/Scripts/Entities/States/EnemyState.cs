using UnityEngine;
using System.Collections.Generic;

//-- Base class for enemy states
public abstract class EnemyState
{
    protected EnemyAI enemy;

    protected EnemyState(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public abstract EnemyAI.AIStates Type { get; }

    //-- Called when the enemy enters this state
    public virtual void Enter()
    {
    }

    //-- Called each physics tick, returns movement input
    public virtual Vector2 Tick()
    {
        return Vector2.zero;
    }

    //-- Adds the choices this state can make
    public virtual void Score(Dictionary<EnemyAI.AIStates, float> scores)
    {
    }

    //-- Called when the enemy leaves this state
    public virtual void Exit()
    {
    }
}