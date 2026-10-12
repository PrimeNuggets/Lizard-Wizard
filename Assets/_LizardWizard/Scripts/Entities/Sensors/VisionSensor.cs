using UnityEngine;
using UnityEngine.Serialization;

public class VisionSensor : MonoBehaviour
{
    public Transform player;

    [FormerlySerializedAs("spiderAI")]
    public EnemyAI enemyAI;

    public Transform visionOrigin;

    public float viewDistance = 10f;
    public float viewAngle = 90f;
    public float visionYawOffset = 0f;
    public float detectionRate = 0.2f;

    private float timer;

    private Vector3 VisionForward =>
        Quaternion.AngleAxis(visionYawOffset, Vector3.up) * transform.forward;

    void Update()
    {
        // -- Check vision at the set rate
        timer += Time.deltaTime;

        if (timer >= detectionRate)
        {
            timer = 0f;
            CheckVision();
        }
    }

    void CheckVision()
    {
        if (player == null || enemyAI == null || visionOrigin == null)
            return;

        Vector3 direction = player.position - visionOrigin.position;

        // -- Check distance
        if (direction.magnitude > viewDistance)
            return;

        // -- Check angle
        float angle = Vector3.Angle(VisionForward, direction);

        if (angle > viewAngle / 2f)
            return;

        // -- Check if anything blocks the player
        RaycastHit hit;

        if (Physics.Raycast(
            visionOrigin.position,
            direction.normalized,
            out hit,
            viewDistance))
        {
            Aspect aspect = hit.collider.GetComponent<Aspect>();

            if (aspect != null)
            {
                enemyAI.PlayerDetected(aspect, player.position, "Vision");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // -- Draw the vision range in red
        if (visionOrigin == null || viewDistance <= 0f)
            return;

        Gizmos.color = Color.red;
        Vector3 origin = visionOrigin.position;
        Vector3 forward = VisionForward;
        float halfAngle = Mathf.Clamp(viewAngle, 0f, 360f) * 0.5f;

        // -- Draw a sphere if the angle is 360
        if (halfAngle >= 180f)
        {
            Gizmos.DrawWireSphere(origin, viewDistance);
            return;
        }

        const int segments = 32;
        Vector3 edge = Quaternion.AngleAxis(halfAngle, Vector3.up) * forward;
        Vector3 previous = origin + edge * viewDistance;

        for (int i = 1; i <= segments; i++)
        {
            Vector3 direction =
                Quaternion.AngleAxis(i * 360f / segments, forward) * edge;
            Vector3 point = origin + direction * viewDistance;

            Gizmos.DrawLine(previous, point);

            if (i % 4 == 0)
                Gizmos.DrawLine(origin, point);

            previous = point;
        }

        Gizmos.DrawLine(origin, origin + forward * viewDistance);
    }
}