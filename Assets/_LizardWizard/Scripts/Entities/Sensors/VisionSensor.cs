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
    public bool debugVision;

    //-- Once it's chasing it keeps track of the player from further away
    [Header("While Chasing")]
    public float chaseViewDistance = 25f;
    public float chaseViewAngle = 240f;

    private float timer;

    private Vector3 VisionForward =>
        Quaternion.AngleAxis(visionYawOffset, Vector3.up) * transform.forward;

    private bool Chasing =>
        enemyAI != null && enemyAI.state == EnemyAI.AIStates.Chase;

    private float CurrentViewDistance => Chasing ? chaseViewDistance : viewDistance;
    private float CurrentViewAngle => Chasing ? chaseViewAngle : viewAngle;

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
        {
            if (debugVision)
                Debug.Log("Vision: missing player, enemy AI, or vision origin");
            return;
        }

        Collider playerCollider = player.GetComponent<Collider>();
        Vector3 targetPosition = playerCollider != null
            ? playerCollider.bounds.center
            : player.position;

        Vector3 direction = targetPosition - visionOrigin.position;
        float distance = direction.magnitude;

        if (distance <= 0f)
            return;

        // -- Check distance
        if (distance > CurrentViewDistance)
        {
            if (debugVision)
                Debug.Log("Vision: outside range, distance " + distance);
            return;
        }

        // -- Check angle
        float angle = Vector3.Angle(VisionForward, direction);

        if (angle > CurrentViewAngle / 2f)
        {
            if (debugVision)
                Debug.Log("Vision: outside angle, angle " + angle);
            return;
        }

        // -- Check if anything blocks the player
        RaycastHit[] hits = Physics.RaycastAll(
            visionOrigin.position,
            direction.normalized,
            CurrentViewDistance);

        Collider closestCollider = null;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestCollider = hit.collider;
            }
        }

        if (closestCollider == null)
        {
            if (debugVision)
                Debug.Log("Vision: ray hit nothing outside the spider");
            return;
        }

        Aspect aspect = closestCollider.GetComponentInParent<Aspect>();

        if (debugVision)
            Debug.Log("Vision: ray hit " + closestCollider.name +
                ", aspect " + (aspect != null ? aspect.aspectName.ToString() : "none"));

        if (aspect != null)
            enemyAI.PlayerDetected(aspect, player.position, "Vision");
    }

    void OnDrawGizmosSelected()
    {
        // -- Draw the vision range in red
        if (visionOrigin == null || viewDistance <= 0f)
            return;

        //-- Yellow shows how far it can keep track of you while chasing
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(visionOrigin.position, chaseViewDistance);

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