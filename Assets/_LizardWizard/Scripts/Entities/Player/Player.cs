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
    private InputAction spell1Action;
    private InputAction spell2Action;
    private InputAction spell3Action;
    private InputAction spell4Action;    
    private ISpell Spell1;
    private ISpell Spell2;
    private ISpell Spell3;
    private ISpell Spell4;
    
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
        spell1Action = input.actions.FindAction("Player/Spell1", true);
        spell2Action = input.actions.FindAction("Player/Spell2", true);
        spell3Action = input.actions.FindAction("Player/Spell3", true);
        spell4Action = input.actions.FindAction("Player/Spell4", true);

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
        bool spell1Pressed = spell1Action.IsPressed();
        bool spell2Pressed = spell2Action.IsPressed();
        bool spell3Pressed = spell3Action.IsPressed();
        bool spell4Pressed = spell4Action.IsPressed();
        //NOTE: This implementation of a 'released' function hypothetically might miss sometimes.
        //Stress testing i havent gotten it to miss yet, but if it starts missing try changing updateMode to FixedUpdate
        if (spell1Action.WasReleasedThisFrame()){
            OnSpell1Released();
        }
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

    void SetSpell(int SpellNumber, ISpell spellScript){
        switch (SpellNumber){
            case 1:
                Spell1 = spellScript;
                break;
            case 2:
                Spell2 = spellScript;
                break;
            case 3:
                Spell3 = spellScript;
                break;
            case 4:
                Spell4 = spellScript;
                break;
            default:
                Debug.Log(SpellNumber + ": index invalid!");
                break;
        }

    }


    void OnSpell1()
    {
        Spell1.CastSpellDown();
    }
    void OnSpell1Released()
    {
       Spell1.CastSpellReleased();
    }
        void OnSpell2()
    {
        Spell2.CastSpellDown();
    }
    void OnSpell2Released()
    {
       Spell2.CastSpellReleased();
    }
        void OnSpell3()
    {
        Spell3.CastSpellDown();
    }
    void OnSpell3Released()
    {
       Spell3.CastSpellReleased();
    }
        void OnSpell4()
    {
        Spell1.CastSpellDown();
    }
    void OnSpell4Released()
    {
       Spell4.CastSpellReleased();
    }
}