using System;
using UnityEngine;

[RequireComponent(typeof(HearingSensor))]
[RequireComponent(typeof(VisionSensor))]
public class SpiderAI : EnemyAI
{
    /*
        ======================================
        Map General Attack Plan:
        ======================================
        - Record the intended target
        - Make a script to attach to attack boxes
            - Applies Pause Time
            - Applies Hit Time to User
            - Disables multi-detection if attack type == OneHit
            - Store the hit as a score
            - Add the hit to the number of successful attacks
        - Recievers:
            - Record the attacker
            - Waits for the duration of the pause time

        ===================================
        Projectile Plan
        ===================================
        - Unhandled by attack plan & will exist seperately

        ===================================
        Trap Plan
        ===================================
        - Unhandled by attack plan & will exist seperately

    */

    //=========================================================
        //General Methods
    //=========================================================
    protected override void OnPlayerDetected()
    {
        if (entityData == null ||
            !entityData.grounded ||
            !entityData.ctrl ||
            entityData.GetMoveType() != 'I' ||
            state == AIStates.Retreat)
            return;

        Debug.Log("Player detected by " + LastSense);

        if (LastSense == "Hearing")
        {
            //-- Search where the sound came from
            SearchAt(LastKnownPosition);
            return;
        }

        ChooseReaction();
    }

    //---------------------------------------------------------
    void ChooseReaction()
    {
        float roll = UnityEngine.Random.Range(0f, 1f);
        //Debug.Log("Random Roll: " + roll);

        //-- Most detections lead to Chase
        //if (roll < 0.75f)
        //{
            ChangeState(AIStates.Chase);
        //}
        //else
        //{
        //    ChangeState(AIStates.Retreat);
        //}
    }

    //---------------------------------------------------------
    protected override void AttackActions()
    {
        base.AttackActions();
        //Pounce()
        //Swipe()
    }
}