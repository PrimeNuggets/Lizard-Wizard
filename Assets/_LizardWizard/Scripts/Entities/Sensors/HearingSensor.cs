using UnityEngine;
using UnityEngine.Serialization;

public class HearingSensor : MonoBehaviour
{
    [Min(0.001f)]
    public float minimumLoudness = 1f / 65f;

    [FormerlySerializedAs("spiderAI")]
    public EnemyAI enemyAI;

    [SerializeField] private NoiseChannel playerChannel;

    void OnEnable()
    {
        if (playerChannel != null)
            playerChannel.heard.AddListener(OnNoiseHeard);
    }

    void OnDisable()
    {
        if (playerChannel != null)
            playerChannel.heard.RemoveListener(OnNoiseHeard);
    }

    public void OnNoiseHeard(Vector3 noisePosition, Aspect aspect, float strength)
    {
        if (enemyAI == null || aspect == null || strength <= 0f)
            return;

        float distanceSquared =
            (noisePosition - transform.position).sqrMagnitude;

        //-- Sounds get quieter farther away
        float loudness = strength / (1f + distanceSquared);

        if (loudness >= minimumLoudness)
            enemyAI.PlayerDetected(aspect, noisePosition, "Hearing");
    }

    void OnDrawGizmosSelected()
    {
        //-- Blue shows how far a normal sound can be heard
        float threshold = Mathf.Max(0.001f, minimumLoudness);
        float range = Mathf.Sqrt(Mathf.Max(0f, 1f / threshold - 1f));

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}