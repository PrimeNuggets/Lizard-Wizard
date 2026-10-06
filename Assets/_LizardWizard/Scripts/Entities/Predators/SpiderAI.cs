using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(SpiderHearing))]
[RequireComponent(typeof(SpiderVision))]
public class SpiderAI : AI
{
    public enum Attacks
    {
        Pounce,
        WebTrap
    }
    //=========================================================
        //General Methods
    //=========================================================
    public void PlayerDetected(Aspect aspect, string sense)
    {
        // -- Only react while state change is possible
        if (!CanChangeState())
            return;

        // -- Check for Player aspect
        if (aspect != null && aspect.aspectName == Aspect.aspect.Player)
        {
            Debug.Log("Player detected by " + sense);
            ChooseReaction();
        }
    }
    //---------------------------------------------------------
    void ChooseReaction()
    {
        // -- Random roll from 0 to 1
        float roll = UnityEngine.Random.Range(0f, 1f);

        Debug.Log("Random Roll: " + roll);

        // -- 75% Pounce
        if (roll < 0.75f)
        {
            ChangeState(AIStates.Chase);
        }
        // -- 25% Retreat
        else
        {
            ChangeState(AIStates.Retreat);
        }

        //timer = 0f;
    }
    //---------------------------------------------------------
    protected override void AttackActions()
    {
        base.AttackActions();
        //Pounce()
    }
}