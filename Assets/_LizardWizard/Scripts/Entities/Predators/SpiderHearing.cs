using UnityEngine;

public class SpiderHearing : MonoBehaviour
{
    public float hearingRange = 8f;

    public SpiderAI spiderAI;
    public Aspect playerAspect;

    public void OnNoiseHeard(Vector3 noisePosition)
    {
        // -- Check if noise is close enough
        float distance = Vector3.Distance(transform.position, noisePosition);

        if (distance <= hearingRange)
        {
            spiderAI.PlayerDetected(playerAspect, "Hearing");
        }
    }

    void OnDrawGizmosSelected()
    {
        // -- Show hearing range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
    }
}
