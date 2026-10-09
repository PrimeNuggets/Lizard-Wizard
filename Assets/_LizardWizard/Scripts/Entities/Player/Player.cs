using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Aspect))]
[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    //=========================================
    [Header("Entity Data")]
    [LabelOverride("Data")] public Entity entityTemplate;
    [NonSerialized] public Entity entityData;
    //=========================================
    [Header("Turning")]
    [SerializeField, LabelOverride("Mouse Sensitivity")] float sensitivity;
    [SerializeField] private Transform cameraPitch;
    [SerializeField] private float maxLookAngle = 75f;
    //=========================================
        // Movement
    //=========================================
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private float pitch;
    //=========================================
    [Header("Misc Data")]
    [SerializeField, Tooltip("Meant to rotate the camera without rotating the whole player"), LabelOverride("Camera Reference")] private GameObject cameraRef;
    [SerializeField] private Terrain terrain;
    //=========================================
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //=========================================================
            //Controls
        //=========================================================
        var input = GetComponent<UnityEngine.InputSystem.PlayerInput>();

        moveAction = input.actions.FindAction("Player/Move", true);
        lookAction = input.actions.FindAction("Player/Look", true);
        jumpAction = input.actions.FindAction("Player/Jump", true);

        pitch = cameraPitch.localEulerAngles.x;
        if (pitch > 180f)
            pitch -= 360f;

        //=========================================================
            //Entity Instantiation
        //=========================================================
        entityData = Instantiate(entityTemplate);
        entityData.Initialize(gameObject, terrain);
    }

    // Update is called once per frame
    void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 mouseDelta = lookAction.ReadValue<Vector2>();

        cameraRef.transform.Rotate(Vector3.up, mouseDelta.x * sensitivity, Space.World);

        //-- Keep the camera from turning too far up or down
        pitch = Mathf.Clamp(
            pitch - mouseDelta.y * sensitivity,
            -maxLookAngle,
            maxLookAngle
        );

        cameraPitch.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void FixedUpdate() //Using this saves a lot of fps
    {
        entityData.BeginTick();
        //Variables
        Vector2 move = moveAction.ReadValue<Vector2>();
        bool jumpPressed = jumpAction.IsPressed();
        //===========================================================
            //Movement
        //===========================================================
        entityData.ApplyMove(move, jumpPressed, cameraRef.transform);
    }

    void OnDisable()
    {
        MenuUIScript.SetCursorLocked(false);
    }

    void OnDestroy()
    {
        Destroy(entityData);
    }
}