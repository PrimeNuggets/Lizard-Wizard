using UnityEngine;

//-- Goes on the Hat (under the Head bone). Hat wobbles a bit when the head moves, moon swings from the tip.
//-- Runs in LateUpdate after the Animator so it works with every animation.
[DisallowMultipleComponent]
public sealed class HatSway : MonoBehaviour
{
    [Header("Hat")]
    [SerializeField, Tooltip("Higher = snaps back faster.")] private float hatStiffness = 140f;
    [SerializeField, Tooltip("Higher = settles faster, less bounce.")] private float hatDamping = 10f;
    [SerializeField, Range(0f, 30f)] private float hatMaxAngle = 8f;

    [Header("Moon")]
    [SerializeField] private Transform moon;
    [SerializeField] private float moonStiffness = 18f;
    [SerializeField] private float moonDamping = 4f;
    [SerializeField, Range(0f, 2f), Tooltip("How strongly the moon hangs toward world down.")] private float moonGravity = 1f;
    [SerializeField, Range(0f, 150f)] private float moonMaxAngle = 75f;

    private const float MaxStep = 1f / 120f;

    private Transform body;
    private Spring hat, charm;

    private void Awake()
    {
        //-- sim runs relative to the lizard, the player moves in FixedUpdate and that made the hat stutter
        var animator = GetComponentInParent<Animator>();
        body = animator != null ? animator.transform : transform.root;

        //-- hat points from its base to the tip (where the moon hangs)
        Vector3 hatAxis = moon != null ? moon.localPosition : Vector3.up * 0.25f;
        hat = new Spring(transform, body, hatAxis);
        if (moon != null)
        {
            var mf = moon.GetComponent<MeshFilter>();
            Vector3 hang = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.center : Vector3.down * 0.04f;
            charm = new Spring(moon, body, hang);
        }
    }

    private void OnEnable()
    {
        hat?.Reset();
        charm?.Reset();
    }

    private void LateUpdate()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        if (dt <= 0f) return;
        //-- same size steps every frame so it doesn't jitter
        int steps = Mathf.CeilToInt(dt / MaxStep);
        float h = dt / steps;

        for (int i = 0; i < steps; i++) hat.Simulate(h, hatStiffness, hatDamping, 0f, hatMaxAngle);
        hat.Apply();
        if (charm == null) return;
        for (int i = 0; i < steps; i++) charm.Simulate(h, moonStiffness, moonDamping, moonGravity, moonMaxAngle);
        charm.Apply();
    }

    private sealed class Spring
    {
        private readonly Transform t, space;
        private readonly Quaternion restLocal;
        private readonly Vector3 restDirLocal;
        private readonly float length;
        //-- point and velocity are kept relative to the lizard, not the world
        private Vector3 point, velocity;

        public Spring(Transform target, Transform space, Vector3 axisLocal)
        {
            t = target;
            this.space = space;
            restLocal = target.localRotation;
            length = Mathf.Max(target.TransformVector(axisLocal).magnitude, 0.001f);
            restDirLocal = axisLocal.normalized;
            Reset();
        }

        private Quaternion RestWorld => t.parent != null ? t.parent.rotation * restLocal : restLocal;
        private Vector3 RestDir => RestWorld * restDirLocal;
        private Vector3 ToSpace(Vector3 dir) => Quaternion.Inverse(space.rotation) * dir;
        private Vector3 Pivot => ToSpace(t.position - space.position);

        public void Reset()
        {
            point = Pivot + ToSpace(RestDir) * length;
            velocity = Vector3.zero;
        }

        public void Simulate(float dt, float stiffness, float damping, float gravity, float maxAngle)
        {
            Vector3 pivot = Pivot;
            Vector3 rest = ToSpace(RestDir);
            //-- reset after a teleport/respawn
            if ((point - pivot).sqrMagnitude > 4f * length * length + 1f) Reset();

            Vector3 target = pivot + rest * length;
            Vector3 accel = (target - point) * stiffness - velocity * damping + ToSpace(Physics.gravity) * gravity;
            velocity += accel * dt;
            point += velocity * dt;

            //-- keep the same distance from the pivot and limit the swing
            Vector3 dir = point - pivot;
            dir = dir.sqrMagnitude > 1e-8f ? dir.normalized : rest;
            dir = Vector3.RotateTowards(rest, dir, maxAngle * Mathf.Deg2Rad, 0f);
            point = pivot + dir * length;
            velocity -= Vector3.Project(velocity, dir);
        }

        public void Apply()
        {
            Vector3 dir = space.rotation * (point - Pivot).normalized;
            t.rotation = Quaternion.FromToRotation(RestDir, dir) * RestWorld;
        }
    }
}
