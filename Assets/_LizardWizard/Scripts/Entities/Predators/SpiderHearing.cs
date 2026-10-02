using UnityEngine;

public class SpiderHearing : MonoBehaviour
{
    public float hearingRange = 8f;

    public SpiderAI spiderAI;
    [SerializeField] private NoiseChannel playerChannel;

    public void OnNoiseHeard(Vector3 noisePosition, Aspect aspect)
    {
        // -- Check if noise is close enough
        float distance = Vector3.Distance(transform.position, noisePosition);

        if (distance <= hearingRange)
        {
            spiderAI.PlayerDetected(aspect, "Hearing");
        }
    }

    void OnDrawGizmosSelected()
    {
        // -- Show hearing range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
    }

    void OnEnable()
    {
        playerChannel.heard.AddListener(OnNoiseHeard);
    }

    void OnDisable()
    {
        playerChannel.heard.RemoveListener(OnNoiseHeard);
    }
}
