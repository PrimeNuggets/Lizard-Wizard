using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public sealed class LizardAnimation : MonoBehaviour
{
    private const float BoostFieldOfView = 9f; //-- how much the camera zooms out while boosting

    [SerializeField] private Animator animator;
    [SerializeField, Tooltip("On: Surf while idle, Sprawl while sliding.\nOff reverses them.")]
    private bool swapIdleAndSlideAnimations = true; //Since this applies to the lizard specifically, I'll keep this here.
    [SerializeField] private Material landingDustMaterial;

    private CharacterController character;
    private Player player;
    private InputAction jumpAction;
    private bool completedFlip;
    private bool ignoreCurrentSpin;
    private ParticleSystem landingDust;
    private Camera playerCamera;
    private float boostFieldOfViewOffset;
    [NonSerialized] public float spinProgress;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        player = GetComponent<Player>();
        playerCamera = GetComponentInChildren<Camera>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        var input = GetComponent<PlayerInput>();
        if (input != null) jumpAction = input.actions.FindAction("Player/Jump");
    }

    private void LateUpdate()
    {
        if (animator == null || !animator.isInitialized) return;
        Entity data = player != null ? player.entityData : null;
        bool boosting = player != null && player.IsTrickBoosting;
        animator.SetFloat("SwapClips", swapIdleAndSlideAnimations || boosting ? 1f : 0f);
        UpdateBoostFieldOfView(boosting);

        //-- Hold jump in the air = backflip (Spin state)
        bool canSpin = !boosting && !character.isGrounded && (data == null || data.ctrl);
        if (canSpin) SampleSpinProgress();
        //-- stop after 1 full flip so you land upright even if you keep holding jump
        animator.SetBool("Spinning", canSpin && !completedFlip && jumpAction != null && jumpAction.IsPressed());

        //-- TODO: count flips (each whole normalizedTime = 1 flip) and send to Scoring
        //-- TODO: letting go should freeze the spin where it is, not go back to Airborne (speed multiplier param on Spin, 0 on release)
        //-- TODO: blend Tuck up to 1 for a moment before Spin so it curls up first

        //-- Rotation functionality moved to Entity.cs AlignToGround()
    }

    //-- PlayJump() moved to Entity.cs

    private AnimatorStateInfo GetAnimationState()
    {
        if (animator == null || !animator.isInitialized) return default;
        return animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0)
            : animator.GetCurrentAnimatorStateInfo(0);
    }

    //-- Spin clip is 1 flip per loop, so normalizedTime >= 1 means a full flip
    public void SampleSpinProgress()
    {
        spinProgress = 0f;
        AnimatorStateInfo state = GetAnimationState();
        if (!state.IsName("Base Layer.Spin"))
        {
            ignoreCurrentSpin = false;
        }
        else if (ignoreCurrentSpin) return;
        spinProgress = state.normalizedTime;
    }

    //-- Player calls this on landing. true = landed a full flip
    public bool ConsumeCleanTrickLanding()
    {
        SampleSpinProgress();
        completedFlip = spinProgress >= 1f;
        bool clean = completedFlip;
        ResetTrick();
        return clean;
    }

    public void ResetTrick()
    {
        completedFlip = false;
        ignoreCurrentSpin = GetAnimationState().IsName("Base Layer.Spin");
    }

    public void PlayTrickLanding(Vector3 direction)
    {
        animator.SetBool("Spinning", false);
        animator.SetBool("Grounded", true);
        animator.SetFloat("SwapClips", 1f);
        animator.CrossFadeInFixedTime("Base Layer.Slide", 0.07f);

        if (landingDustMaterial == null) return;
        if (landingDust == null) CreateLandingDust();

        Bounds bounds = character.bounds;
        Vector3 feet = new Vector3(bounds.center.x, bounds.min.y + 0.13f, bounds.center.z);
        landingDust.transform.SetPositionAndRotation(feet, Quaternion.LookRotation(-direction + Vector3.up * 0.25f));
        landingDust.Play();
        landingDust.Emit(22);
    }

    //-- zoom the camera out a bit while boosting
    private void UpdateBoostFieldOfView(bool boosting)
    {
        if (playerCamera == null || playerCamera.orthographic) return;
        float nextOffset = Mathf.MoveTowards(boostFieldOfViewOffset, boosting ? BoostFieldOfView : 0f,
            (boosting ? 75f : 40f) * Time.deltaTime);
        playerCamera.fieldOfView += nextOffset - boostFieldOfViewOffset;
        boostFieldOfViewOffset = nextOffset;
    }

    //-- OnDisable() functionality move to Entity.cs Disable(), this one just undoes the boost zoom + trick state
    private void OnDisable()
    {
        if (playerCamera != null) playerCamera.fieldOfView -= boostFieldOfViewOffset;
        boostFieldOfViewOffset = 0f;
        ResetTrick();
    }

    //-- sand puff for trick landings, made once the first time we need it
    //-- TODO: bigger puff on crash
    private void CreateLandingDust()
    {
        var dust = new GameObject("Trick Landing Dust");
        dust.transform.SetParent(transform, false);
        landingDust = dust.AddComponent<ParticleSystem>();
        landingDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        dust.GetComponent<ParticleSystemRenderer>().sharedMaterial = landingDustMaterial;

        var main = landingDust.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.45f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.43f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.46f);
        main.startColor = new Color(0.92f, 0.74f, 0.48f, 0.8f);
        main.maxParticles = 32;

        var emission = landingDust.emission;
        emission.enabled = false;
        var shape = landingDust.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.14f;
        shape.angle = 37f;

        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        var color = landingDust.colorOverLifetime;
        color.enabled = true;
        color.color = fade;
    }
}
