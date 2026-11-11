using UnityEngine;
using UnityEngine.AI;
using static DebugHandler;

//-- Casts a few rays in front of the enemy and bends its direction around walls and steep dunes
//-- Ray + hit normal avoidance is from Aversa, Kyaw & Peters - Unity Artificial Intelligence Programming 4th ed., Ch. 6 (Avoiding obstacles)
public class ObstacleAvoidance : MonoBehaviour
{
    [SerializeField] private bool debug;
    public float whiskerLength = 3f;
    [Tooltip("Angle of the side rays")] public float whiskerAngle = 35f;
    [Tooltip("Height of the rays above the enemy's pivot")] public float whiskerHeight = 0.5f;
    [Tooltip("Anything steeper than this counts as a wall")] public float maxSlope = 60f;
    public LayerMask obstacleLayers = ~0;
    [Tooltip("Seconds of not moving before it tries going around")] public float stuckTime = 1f;

    //-- Big stuff like blocks it goes around with the baked NavMesh, the rays are just for small stuff
    //-- NavMesh from the same book, Ch. 8, but it follows the path corners itself instead of using a NavMeshAgent
    [Header("NavMesh")]
    public bool useNavMesh = true;
    [Tooltip("How often it updates the path")] public float repathTime = 0.25f;
    [Tooltip("How close it gets to a corner before going to the next one")] public float cornerDistance = 1f;

    private Vector3 lastPosition;
    private float stuckTimer;
    private Vector3 unstuckDirection;
    private float unstuckUntil;

    //-- Reused every frame so steering doesn't make garbage
    private readonly float[] whiskerAngles = new float[3];
    private readonly RaycastHit[] whiskerHits = new RaycastHit[8];
    private readonly Vector3[] corners = new Vector3[32];

    private NavMeshPath path;
    private int cornerCount;
    private int corner;
    private bool pathComplete;
    private float nextPathTime;
    private Vector3 pathTarget;

    void Start()
    {
        lastPosition = transform.position;
        path = new NavMeshPath();
    }

    //-- Gives back the next corner to walk to, or the target itself if there's no NavMesh
    public Vector3 NextCorner(Vector3 target)
    {
        if (!useNavMesh || path == null)
            return target;

        //-- New path every so often or if the target moved a lot
        if (Time.time >= nextPathTime || (target - pathTarget).sqrMagnitude > 9f)
        {
            nextPathTime = Time.time + repathTime;
            bool sameTarget = (target - pathTarget).sqrMagnitude <= 9f;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit start, 4f, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(target, out NavMeshHit end, 4f, NavMesh.AllAreas) &&
                NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path) &&
                path.status != NavMeshPathStatus.PathInvalid)
            {
                cornerCount = path.GetCornersNonAlloc(corners);
                pathComplete = path.status == NavMeshPathStatus.PathComplete;
                corner = 1;
                pathTarget = target;
            }
            //-- Lost the NavMesh for a sec (edge of a hole), keep the old path
            else if (!sameTarget)
            {
                cornerCount = 0;
                pathTarget = target;
            }
        }

        if (cornerCount < 2)
            return target;

        //-- Skip corners we already got to
        while (corner < cornerCount - 1 && FlatDistance(transform.position, corners[corner]) < cornerDistance)
            corner++;

        //-- If it can already see a later corner on the NavMesh, go straight for it
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit here, 4f, NavMesh.AllAreas))
        {
            while (corner < cornerCount - 1 && !NavMesh.Raycast(here.position, corners[corner + 1], out _, NavMesh.AllAreas))
                corner++;
        }

        //-- Last corner, just go for the actual target
        if (corner >= cornerCount - 1 && pathComplete)
            return target;

        return corners[corner];
    }

    //-- True when it has a NavMesh path, then the rays aren't needed
    public bool FollowingPath => useNavMesh && cornerCount >= 2;

    float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    //-- Ignore is whatever it shouldn't steer away from (the player it's chasing)
    public Vector3 SteerAround(Vector3 desired, Transform ignore = null)
    {
        desired.y = 0f;
        if (!VerifyVector(desired))
            return desired;

        desired.Normalize();

        //-- Was stuck, keep going to the side for a bit
        if (Time.time < unstuckUntil)
            return unstuckDirection;

        //-- NavMesh already goes around stuff, the rays just fight it
        if (FollowingPath)
            return desired;

        Vector3 origin = transform.position + Vector3.up * whiskerHeight;
        Vector3 push = Vector3.zero;

        whiskerAngles[0] = 0f;
        whiskerAngles[1] = -whiskerAngle;
        whiskerAngles[2] = whiskerAngle;

        foreach (float angle in whiskerAngles)
        {
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * desired;

            if (HitsWall(origin, direction, ignore, out Vector3 normal))
                push += normal;
        }

        Vector3 steered = desired + push * 1.5f;
        steered.y = 0f;

        //-- Wall straight ahead, go along it instead
        if (steered.sqrMagnitude < 0.01f)
            steered = Vector3.Cross(Vector3.up, desired);

        return steered.normalized;
    }

    //-- Checks if a ray hits something too steep to walk up
    bool HitsWall(Vector3 origin, Vector3 direction, Transform ignore, out Vector3 normal)
    {
        normal = Vector3.zero;

        int count = Physics.RaycastNonAlloc(origin, direction, whiskerHits, whiskerLength, obstacleLayers, QueryTriggerInteraction.Ignore);

        RaycastHit closest = default;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = whiskerHits[i];

            //-- Skip our own legs and the player we're chasing
            if (hit.transform.IsChildOf(transform))
                continue;

            if (ignore != null && hit.transform.IsChildOf(ignore))
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closest = hit;
            }
        }

        if (closestDistance == float.MaxValue)
            return false;

        //-- Gentle slopes are fine to walk up
        if (Vector3.Angle(closest.normal, Vector3.up) <= maxSlope)
            return false;

        normal = closest.normal;
        normal.y = 0f;
        normal.Normalize();
        return true;
    }

    //-- If it's not moving when it should be, try going to the side
    public void CheckStuck(Vector2 move, float speed)
    {
        Vector3 moved = transform.position - lastPosition;
        moved.y = 0f;
        lastPosition = transform.position;

        float expected = speed * move.magnitude * Time.fixedDeltaTime;

        //-- Trying to move but barely going anywhere
        if (move.sqrMagnitude > 0.01f && moved.magnitude < expected * 0.25f)
            stuckTimer += Time.fixedDeltaTime;
        else
            stuckTimer = 0f;

        if (stuckTimer < stuckTime)
            return;

        stuckTimer = 0f;

        //-- Get a new path and skip the corner it couldn't get to
        nextPathTime = 0f;
        if (corner < cornerCount - 1)
            corner++;

        float side = Random.value < 0.5f ? -1f : 1f;
        unstuckDirection = Vector3.Cross(Vector3.up, transform.forward) * side;
        unstuckUntil = Time.time + 0.75f;
    }

    bool Debug()
    {
        return debug ? DebugHandler.DevMode() : false;
    }

    //-- Green rays show what it checks for walls
    void OnDrawGizmosSelected()
    {
        if (!Debug()) return;
        Gizmos.color = Color.green;
        Vector3 origin = transform.position + Vector3.up * whiskerHeight;

        foreach (float angle in new float[] { 0f, -whiskerAngle, whiskerAngle })
        {
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            Gizmos.DrawLine(origin, origin + direction * whiskerLength);
        }

        //-- Magenta = NavMesh path, a bit higher so the sand doesn't hide it
        Gizmos.color = Color.magenta;
        Vector3 up = Vector3.up * 0.5f;

        for (int i = 1; i < cornerCount; i++)
            Gizmos.DrawLine(corners[i - 1] + up, corners[i] + up);
    }
}
