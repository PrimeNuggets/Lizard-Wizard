using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public sealed class LizardAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualRoot;
    [SerializeField, Tooltip("On: Surf while idle, Sprawl while sliding. Off reverses them.")]
    private bool swapIdleAndSlideAnimations = true;
    [SerializeField, Min(0f)] private float turnSpeed = 540f;
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
        Vector3 movement = character.velocity;
        movement.y = 0f;

        //-- Wait for steady contact before counting a landing
        groundedTime = character.isGrounded ? groundedTime + Time.deltaTime : 0f;
        animator.SetBool("Grounded", character.isGrounded && groundedTime >= groundContactDelay);
        animator.SetFloat("Speed", movement.magnitude);
        animator.SetFloat("SwapClips", swapIdleAndSlideAnimations ? 1f : 0f);

        //-- The Animator picks the pose; this only turns the model
        if (visualRoot != null && movement.sqrMagnitude > 0.0001f)
            visualRoot.rotation = Quaternion.RotateTowards(visualRoot.rotation,
                Quaternion.LookRotation(movement), turnSpeed * Time.deltaTime);
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
