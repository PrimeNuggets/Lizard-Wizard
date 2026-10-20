using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public sealed class LizardAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Tooltip("On: Surf while idle, Sprawl while sliding.\nOff reverses them.")]
    private bool swapIdleAndSlideAnimations = true;
    [SerializeField, Min(0f)] private float groundContactDelay = 0.08f;
    private CharacterController character;
    private float groundedTime;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void LateUpdate()
    {
        if (animator == null) return;
        animator.SetFloat("SwapClips", swapIdleAndSlideAnimations ? 1f : 0f);
        /*
        Vector3 movement = character.velocity;
        movement.y = 0f;

        //-- Wait for steady contact before counting a landing
        groundedTime = character.isGrounded ? groundedTime + Time.deltaTime : 0f;
        animator.SetBool("Grounded", character.isGrounded && groundedTime >= groundContactDelay);
        animator.SetFloat("Speed", movement.magnitude);
        animator.SetFloat("SwapClips", swapIdleAndSlideAnimations ? 1f : 0f);


        //=============================================================
            //Move Rotation
        //=============================================================
        if (visualRoot != null) {
            Vector3 groundNormal = Vector3.up;
            if (character.isGrounded)
            {
                Bounds bounds = character.bounds;

                Debug.DrawRay(
                    bounds.center,
                    Vector3.down * (bounds.extents.y + groundProbeDist),
                    Color.red
                );

                if (UnityEngine.Physics.Raycast(
                        bounds.center,
                        Vector3.down,
                        out RaycastHit hit,
                        bounds.extents.y + groundProbeDist,
                        groundLayer,
                        QueryTriggerInteraction.Ignore
                    ))
                {
                    groundNormal = hit.normal;
                    Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.blue);
                }
            }

            Vector3 forward = movement.sqrMagnitude > 0.0001f ? movement : Vector3.ProjectOnPlane(visualRoot.forward, Vector3.up);

            forward = Vector3.ProjectOnPlane(forward, groundNormal);

            //-- The Animator picks the pose; this only turns the model
            if (movement.sqrMagnitude > 0.0001f) {
                Quaternion.LookRotation(forward, groundNormal);
                visualRoot.rotation = Quaternion.RotateTowards(
                    visualRoot.rotation,
                    Quaternion.LookRotation(movement), turnSpeed * Time.deltaTime
                );
            }
        }*/
    }

    //-- Called by Player after takeoff
    public void PlayJump()
    {
        if (animator == null || !isActiveAndEnabled) return;
        groundedTime = 0f;
        animator.SetBool("Grounded", false);
        animator.ResetTrigger("Jump");
        animator.SetTrigger("Jump");
    }

    //-- Clear old values when the player is disabled
    private void OnDisable()
    {
        groundedTime = 0f;
        if (animator == null || !animator.isInitialized) return;
        animator.SetFloat("Speed", 0f);
        animator.SetBool("Grounded", false);
        animator.ResetTrigger("Jump");
    }
}
