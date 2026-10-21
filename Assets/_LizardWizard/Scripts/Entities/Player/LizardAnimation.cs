using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public sealed class LizardAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Tooltip("On: Surf while idle, Sprawl while sliding.\nOff reverses them.")]
    private bool swapIdleAndSlideAnimations = true; //Since this applies to the lizard specifically, I'll keep this here.
    private CharacterController character;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void LateUpdate()
    {
        if (animator == null) return;
        animator.SetFloat("SwapClips", swapIdleAndSlideAnimations ? 1f : 0f);
        
        //-- Rotation functionality moved to Entity.cs AlignToGround()
    }

    //-- PlayJump() moved to Entity.cs

    //-- OnDisable() functionality move to Entity.cs Disable()
}
