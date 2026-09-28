using UnityEngine;

public class SpiderVision : MonoBehaviour
{
    public Transform player;
    public SpiderAI spiderAI;

    public float viewDistance = 10f;
    public float viewAngle = 90f;
    public float detectionRate = 0.2f;

    private float timer;

    void Update()
    {
        // -- Check vision every 0.2 seconds
        timer += Time.deltaTime;

        if (timer >= detectionRate)
        {
            timer = 0f;
            CheckVision();
        }
    }

    void CheckVision()
    {
        Vector3 direction = player.position - transform.position;

        // -- Check distance
        if (direction.magnitude > viewDistance)
            return;

        // -- Check view angle
        float angle = Vector3.Angle(transform.forward, direction);

        if (angle > viewAngle / 2f)
            return;

        // -- Check line of sight
        RaycastHit hit;

        if (Physics.Raycast(
            transform.position,
            direction.normalized,
            out hit,
            viewDistance))
        {
            Aspect aspect = hit.collider.GetComponent<Aspect>();

            // -- Send detection to SpiderAI
            if (aspect != null)
            {
                spiderAI.PlayerDetected(aspect, "Vision");
            }
        }
    }
}