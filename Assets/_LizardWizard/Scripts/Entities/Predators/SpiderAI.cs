using UnityEngine;

[RequireComponent(typeof(HearingSensor))]
[RequireComponent(typeof(VisionSensor))]
public class SpiderAI : EnemyAI
{
    public enum Attacks
    {
        Pounce,
        WebTrap
    }

    //=========================================================
        //General Methods
    //=========================================================
    protected override void OnPlayerDetected()
    {
        if (entityData == null ||
            !entityData.grounded ||
            !entityData.ctrl ||
            entityData.GetMoveType() != 'I')
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
        Debug.Log("Random Roll: " + roll);

        //-- Most detections lead to Chase
        if (roll < 0.75f)
        {
            ChangeState(AIStates.Chase);
        }
        else
        {
            ChangeState(AIStates.Retreat);
        }
    }

    //---------------------------------------------------------
    protected override void AttackActions()
    {
        base.AttackActions();
        //Pounce()
    }
}