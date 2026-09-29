using UnityEngine;

public class SpiderAI : MonoBehaviour
{
    public enum State
    {
        Idle,
        Pounce,
        Retreat
    }

    public State currentState = State.Idle;
    public float actionTime = 2f;

    private float timer;

    void Start()
    {
        // -- Start in Idle
        Debug.Log("Spider is Idle");
    }

    void Update()
    {
        // -- Return to Idle after an action
        if (currentState != State.Idle)
        {
            timer += Time.deltaTime;

            if (timer >= actionTime)
            {
                currentState = State.Idle;
                timer = 0f;

                Debug.Log("Spider returned to Idle");
            }
        }
    }

    public void PlayerDetected(Aspect aspect, string sense)
    {
        // -- Only react while Idle
        if (currentState != State.Idle)
            return;

        // -- Check for Player aspect
        if (aspect != null && aspect.aspectName == Aspect.aspect.Player)
        {
            Debug.Log("Player detected by " + sense);
            ChooseReaction();
        }
    }

    void ChooseReaction()
    {
        // -- Random roll from 0 to 1
        float roll = Random.Range(0f, 1f);

        Debug.Log("Random Roll: " + roll);

        // -- 75% Pounce
        if (roll < 0.75f)
        {
            currentState = State.Pounce;
            Debug.Log("Pounce");
        }
        // -- 25% Retreat
        else
        {
            currentState = State.Retreat;
            Debug.Log("Retreat");
        }

        timer = 0f;
    }
}