using UnityEngine;

[RequireComponent(typeof(HearingSensor))]
[RequireComponent(typeof(VisionSensor))]
public class ScorpionAI : EnemyAI
{
    public enum Attacks
    {
        Pinch,
        Sting
    }

    //-- Chance it panics and runs off instead of chasing
    [SerializeField, Range(0f, 1f)] private float panicChance = 0.35f;

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
        Debug.Log("Random Roll: " + roll);

        if (roll < panicChance)
        {
            ChangeState(AIStates.Retreat);
        }
        else
        {
            ChangeState(AIStates.Chase);
        }
    }

    //---------------------------------------------------------
    protected override void AttackActions()
    {
        base.AttackActions();
        //Pinch()
        //Sting()
    }
}
