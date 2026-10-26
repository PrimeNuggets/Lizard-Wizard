using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public sealed class LizardAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Tooltip("On: Surf while idle, Sprawl while sliding.\nOff reverses them.")]
    private bool swapIdleAndSlideAnimations = true; //Since this applies to the lizard specifically, I'll keep this here.
    private CharacterController character;
    private InputAction jumpAction;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        var input = GetComponent<PlayerInput>();
        if (input != null) jumpAction = input.actions.FindAction("Player/Jump");
    }

    private void LateUpdate()
    {
        if (animator == null) return;
        animator.SetFloat("SwapClips", swapIdleAndSlideAnimations ? 1f : 0f);
        //-- Hold jump in the air = backflip (Spin state)
        animator.SetBool("Spinning", jumpAction != null && jumpAction.IsPressed() && !character.isGrounded);

        //-- TODO: get flip angle from the Spin state (clip is 1 flip per loop): angle = (normalizedTime % 1) * 360
        //-- TODO: count flips (each whole normalizedTime = 1 flip) and send to Scoring
        //-- TODO: letting go should freeze the spin where it is, not go back to Airborne (speed multiplier param on Spin, 0 on release)
        //-- TODO: blend Tuck up to 1 for a moment before Spin so it curls up first
        
        //-- Rotation functionality moved to Entity.cs AlignToGround()
    }

    //-- PlayJump() moved to Entity.cs

    //-- OnDisable() functionality move to Entity.cs Disable()
}
