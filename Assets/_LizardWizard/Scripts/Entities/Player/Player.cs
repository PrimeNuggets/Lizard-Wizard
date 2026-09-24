using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
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
    [Header("Entity Data")]
    [LabelOverride("Data")] public Entity entityTemplate;
    [NonSerialized] public Entity entityData;
    //=========================================
    [Header("Misc Data")]
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
            //Turning
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
        } else {
            Vector2 mouseDelta = lookAction.ReadValue<Vector2>();
            transform.Rotate(Vector3.up, mouseDelta.x * sensitivity, Space.World);
        }
    }

    void FixedUpdate()
    {
        //Variables
        Vector2 move = moveAction.ReadValue<Vector2>();
        bool jumpPressed = jumpAction.WasPressedThisFrame();
        //===========================================================
            //Movement
        //===========================================================
        transform.position = entityData.ApplyMove(move, jumpPressed);
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
