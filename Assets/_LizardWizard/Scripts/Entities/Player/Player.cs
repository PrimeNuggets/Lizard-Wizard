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
    //=========================================
        // Movement
    //=========================================
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    //=========================================
    [Header("Misc Data")]
    [SerializeField, Tooltip("Meant to rotate the camera without rotating the whole player"), LabelOverride("Camera Reference")] private GameObject cameraRef;
    [SerializeField] private Terrain terrain;
    //=========================================
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //=========================================================
            //Cursor Locking
        //=========================================================
        SetCursorLocked(false); //Start unlocked so that it's easier to test in the editor

        //=========================================================
            //Controls
        //=========================================================
        var input = GetComponent<UnityEngine.InputSystem.PlayerInput>();

        moveAction = input.actions.FindAction("Player/Move", true);
        lookAction = input.actions.FindAction("Player/Look", true);
        jumpAction = input.actions.FindAction("Player/Jump", true);

        //=========================================================
            //Entity Instantiation
        //=========================================================
        entityData = Instantiate(entityTemplate);
        entityData.Initialize(gameObject, terrain);
    }

    // Update is called once per frame
    void Update()
    {
        //===========================================================
            //Cursor Locking
        //===========================================================
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(false);
        }
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                SetCursorLocked(true);
            }
        }
    }

    void FixedUpdate() //Using this saves a lot of fps
    {
        entityData.BeginTick();
        //Variables
        Vector2 move = moveAction.ReadValue<Vector2>();
        bool jumpPressed = jumpAction.IsPressed();
        //===========================================================
            //Turning
        //===========================================================
        if (Cursor.lockState == CursorLockMode.Locked) {
            Vector2 mouseDelta = lookAction.ReadValue<Vector2>();
            cameraRef.transform.Rotate(Vector3.up, mouseDelta.x * sensitivity, Space.World);
        }
        //===========================================================
            //Movement
        //===========================================================
        entityData.ApplyMove(move, jumpPressed, cameraRef.transform);
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void OnDisable()
    {
        SetCursorLocked(false);
    }
    void OnDestroy()
    {
        Destroy(entityData);
    }

}
