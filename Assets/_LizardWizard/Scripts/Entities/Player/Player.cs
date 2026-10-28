using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static Entity;

[RequireComponent(typeof(Aspect))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(WaterLevelPercentScore))]
[DisallowMultipleComponent]
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
    private LizardAnimation lizardAnimation;
    //=========================================
    [Header("Quest Data")]
    [SerializeField] private Quests tutorialQuest;
    [SerializeField] private QuestRunner questRunner;
    [NonSerialized] public List<Quests> ongoingQuests = new List<Quests>();
    [NonSerialized] public List<Quests> completedQuests = new List<Quests>();
    //=========================================
    [Header("Misc Data")]
    [SerializeField, Tooltip("Meant to rotate the camera without rotating the whole player"), LabelOverride("Camera Reference")] private GameObject cameraRef;
    [SerializeField] private Transform mesh;
    //=========================================

    //================================
    //stats ?
    //[NonSerialized] public float water;
    //[NonSerialized] public Stat health;
    //[NonSerialized] public Stat speed;
    //[NonSerialized] public Stat jumpHeight;
    //[NonSerialized] public NoiseChannel noiseChannel;
    //=========================================
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //=========================================================
            //Controls
        //=========================================================
        var input = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        lizardAnimation = GetComponent<LizardAnimation>();

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
        entityData.Initialize(gameObject, mesh);
        var waterLevel = GetComponent<WaterLevelPercentScore>();
        waterLevel.StartScoring(entityData);

        //=========================================================
            //Quest Initialization
        //=========================================================
        if (tutorialQuest != null) {
            StartQuest(tutorialQuest);
        }

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
        entityData?.BeginTick();
        //Variables
        Vector2 move = moveAction.ReadValue<Vector2>();
        bool jumpPressed = jumpAction.IsPressed(); //-- TODO: holding jump for a flip re-jumps right when you land, switch to WasPressedThisFrame() (watch FixedUpdate missing it)
        bool spell1Pressed = spell1Action.IsPressed();
        bool spell2Pressed = spell2Action.IsPressed();
        bool spell3Pressed = spell3Action.IsPressed();
        bool spell4Pressed = spell4Action.IsPressed();
        //NOTE: This implementation of a 'released' function hypothetically might miss sometimes.
        //Stress testing i havent gotten it to miss yet, but if it starts missing try changing updateMode to FixedUpdate
        if (spell1Action.WasReleasedThisFrame()){
            //-- TEMP: commented out until OnSpell1Released exists, it was breaking Play mode
            // OnSpell1Released();
        }
        //===========================================================
            //Movement
        //===========================================================
        bool wasGrounded = entityData.entChar.isGrounded;
        entityData.ApplyMove(move, jumpPressed, cameraRef.transform);
    }

    void LateUpdate()
    {
        entityData?.AfterTick();
    }

    void OnDisable()
    {
        entityData?.Disable();
        MenuUIScript.SetCursorLocked(false);
    }

    void OnDestroy()
    {
        Destroy(entityData);
    }

    public void StartQuest(Quests quest)
    {
        questRunner.SetQuest(quest);
        ongoingQuests.Add(questRunner.quest);
        questRunner.StartQuest(questRunner.quest);
    }

    public void OnQuestEnd(Quests quest)
    {
        completedQuests.Add(quest);
        Scoring.AddValue($"quest.{quest.questId}.completed", 1); //tracks which quests have been completed
        Scoring.AddValue($"quests_completed", 1); //tracks how many quests have been completed
        ongoingQuests.Remove(quest);
        questRunner.quest = null;
    }
}
