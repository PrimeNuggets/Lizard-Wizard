using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static Entity;

[RequireComponent(typeof(Aspect))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(WaterLevelPercentScore))]
[DisallowMultipleComponent]
public class Player : MonoBehaviour
{
    #region Variables
        //=========================================
        #region Entity Data
            [Header("Entity Data")]
            [LabelOverride("Data")] public Entity entityTemplate;
            [NonSerialized] public Entity entityData;
        #endregion
        //=========================================
        #region Turning
            [Header("Turning")]
            [SerializeField, Tooltip("Meant to rotate the camera without rotating the whole player"), LabelOverride("Camera Reference")] private GameObject cameraRef;
            [SerializeField, LabelOverride("Mouse Sensitivity")] float sensitivity;
            [SerializeField, LabelOverride("Stick Sensitivity"), Tooltip("Degrees per second at full tilt")] float stickSensitivity = 180f; //-- controller look
            [SerializeField] private Transform cameraPitch;
            [SerializeField] private float maxLookAngle = 75f;
        #endregion
        //=========================================
        #region Movement
            private InputAction moveAction;
            private InputAction lookAction;
            private InputAction jumpAction;
            private float pitch;
        #endregion
        //=========================================
        #region Spells
            private InputAction spell1Action;
            private InputAction spell2Action;
            private InputAction spell3Action;
            private InputAction spell4Action;
            [Header("Spell List")]
            [SerializeField] private ISpell Spell1;
            [SerializeField] private ISpell Spell2;
            [SerializeField] private ISpell Spell3;
            [SerializeField] private ISpell Spell4;
            public SteamDash testspell1;
        #endregion
        //=========================================
        private LizardAnimation lizardAnimation;
        //=========================================
        #region Quests
            [Header("Quest Data")]
            [SerializeField] private Quests tutorialQuest;
            [SerializeField] private QuestRunner questRunner;
            [NonSerialized] public List<Quests> ongoingQuests = new List<Quests>();
            [NonSerialized] public List<Quests> completedQuests = new List<Quests>();
            public List<Quests> achievements;
            [NonSerialized] public List<Quests> completedAchievements = new List<Quests>();
        #endregion
        //=========================================
        #region Landing
            [Header("Landing")]
            [SerializeField] private LandingCheck[] landingChecks;
            [SerializeField] private float checkDist = 5f;
            [SerializeField] private LayerMask groundLayers;
            [SerializeField] private LandingCheck.Threshold[] checkThresholds;
            [SerializeField] private bool showDebug;
        #endregion
        //=========================================
        [Header("Misc Data")]
        [SerializeField] private Transform mesh;
        [NonSerialized] public WaterLevelPercentScore waterLevel;
        //=========================================
    #endregion
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //=========================================================
        #region Controls
            var input = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            lizardAnimation = GetComponent<LizardAnimation>();

            moveAction = input.actions.FindAction("Player/Move", true);
            lookAction = input.actions.FindAction("Player/Look", true);
            jumpAction = input.actions.FindAction("Player/Jump", true);
            spell1Action = input.actions.FindAction("Player/Spell1", true);
            spell2Action = input.actions.FindAction("Player/Spell2", true);
            spell3Action = input.actions.FindAction("Player/Spell3", true);
            spell4Action = input.actions.FindAction("Player/Spell4", true);
        #endregion
        //=========================================================
        #region Entity Instantiation
            entityData = Instantiate(entityTemplate);
            entityData.Initialize(gameObject, mesh);
            waterLevel = GetComponent<WaterLevelPercentScore>();
            waterLevel.StartScoring(entityData);
            foreach (var check in landingChecks)
            {
                check.showDebug = showDebug;
                check.checkDist = checkDist;
                check.groundLayers = groundLayers;
                check.checkThresholds = checkThresholds;
                check.player = transform;
            }
            SetSpell(testspell1, 1);
        #endregion
        //=========================================================
        #region Quest Initialization
            if (tutorialQuest != null) {
                StartQuest(tutorialQuest);
            }
            foreach (var toast in achievements)
            {
                StartAchievement(toast);
            }
        #endregion
        //=========================================================
        #region Camera
            pitch = cameraPitch.localEulerAngles.x;
            if (pitch > 180f)
                pitch -= 360f;
            GetLookDir();
        #endregion
    }
    // Update is called once per frame
    void Update()
    {
        //=========================================================
        #region Camera
            if (Cursor.lockState != CursorLockMode.Locked) return;
            Vector2 mouseDelta = lookAction.ReadValue<Vector2>();

            //-- mouse gives pixels per frame, sticks give -1..1 so they need deltaTime or it's super slow
            bool fromStick = !(lookAction.activeControl?.device is Pointer);
            float lookSens = fromStick ? stickSensitivity * Time.deltaTime : sensitivity;

            cameraRef.transform.Rotate(Vector3.up, mouseDelta.x * lookSens, Space.World);

            //-- Keep the camera from turning too far up or down
            pitch = Mathf.Clamp(
                pitch - mouseDelta.y * lookSens,
                -maxLookAngle,
                maxLookAngle
            );

            cameraPitch.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        #endregion
    }
    void FixedUpdate() //Using this saves a lot of fps
    {
        //=========================================================
        #region Entity Processing
            entityData?.BeginTick();
            if (entityData.IsHitPaused)
            {
                entityData.ApplyMove(Vector2.zero, false);
                return;
            }
            Scoring.SetValue($"{name.ToLowerInvariant()}.water", (int)Mathf.Round(entityData.water.GetCurrent()));
        #endregion
        //=========================================================
        #region Movement
            Vector2 move = moveAction.ReadValue<Vector2>();
            bool jumpPressed = jumpAction.IsPressed(); //-- TODO: holding jump for a flip re-jumps right when you land, switch to WasPressedThisFrame() (watch FixedUpdate missing it)
            bool wasGrounded = entityData.entChar.isGrounded;
            bool jumped = entityData.jumped; //stores the jumped bool before it updates

            entityData.ApplyMove(move, jumpPressed, cameraRef.transform);

            bool landed = !wasGrounded && entityData.entChar.isGrounded;

            if (landed && jumped)
            {
                foreach (var landing in landingChecks)
                {
                    int threshIndex = landing.Check();
                    if (threshIndex != -1)
                    {
                        Debug.Log($"{landing.GameObject().name}: {checkThresholds[threshIndex].name} landing");
                    }
                }
            }
        #endregion
        //===========================================================
        #region Spells
            bool spell1Pressed = spell1Action.IsPressed();
            bool spell2Pressed = spell2Action.IsPressed();
            bool spell3Pressed = spell3Action.IsPressed();
            bool spell4Pressed = spell4Action.IsPressed();
            //NOTE: This implementation of a 'released' function hypothetically might miss sometimes.
            //Stress testing i havent gotten it to miss yet, but if it starts missing(esp in high framerate) try changing updateMode to FixedUpdate
            //or move this to update ig idk
            if (spell1Action.WasReleasedThisFrame()){
                OnSpellReleased(1);
            }
            if (spell2Action.WasReleasedThisFrame()){
                OnSpellReleased(2);
            }
            if (spell3Action.WasReleasedThisFrame()){
                OnSpellReleased(3);
            }
            if (spell4Action.WasReleasedThisFrame()){
                OnSpellReleased(4);
            }
        #endregion
    }
    void LateUpdate()
    {
        #region Entity Processing
            entityData?.AfterTick();
        #endregion
    }
    void OnDisable()
    {
        entityData?.Disable();
        MenuUIScript.SetCursorLocked(false);
    }
    void OnDestroy()
    {
        #region Entity Processing
            Destroy(entityData);
        #endregion
    }
    
    #region Spell Methods
        void OnSpell(int slot)
        {
            switch (slot)
            {
                case 1:
                    Spell1.CastSpellDown();
                    break;
                case 2:
                    Spell2.CastSpellDown();
                    break;
                case 3:
                    Spell3.CastSpellDown();
                    break;
                case 4:
                    Spell4.CastSpellDown();
                    break;
            }
        }
        void OnSpellReleased(int slot)
        {
            switch (slot)
            {
                case 1:
                    Spell1.CastSpellReleased();
                    break;
                case 2:
                    Spell2.CastSpellReleased();
                    break;
                case 3:
                    Spell3.CastSpellReleased();
                    break;
                case 4:
                    Spell4.CastSpellReleased();
                    break;
            }
        }
        public void SetSpell(ISpell spell, int slot){
            switch(slot){
                case 1:
                    Spell1 = spell;
                    break;
                case 2:
                    Spell2 = spell;
                    break;
                case 3:
                    Spell3 = spell;
                    break;
                case 4:
                    Spell4 = spell;
                    break;
                default:
                    Debug.Log("Invalid slot assignment: " + slot);
                    break;
            }
        }
    #endregion
    public Vector3 GetLookDir() { //Subject to move to Entity.cs
       //OKAY IM DOING DUMB SHIT FOR THIS
       //the children gotta be in the right order
       //camera ref is 2nd, camera 1st inside that, reticle 1st inside that
       //dumbass way to do this smh my head
       //-vv
        return transform.GetChild(1).GetChild(0).GetChild(0).position - transform.GetChild(1).GetChild(0).position;
    }
    #region Quest Methods
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
        public void StartAchievement(Quests quest)
        {
            questRunner.StartQuest(questRunner.quest);
        }
        public void OnAchievementEnd(Quests quest)
        {
            completedAchievements.Add(quest);
            Scoring.AddValue($"achievement.{quest.questId}.completed", 1); //tracks which quests have been completed
            Scoring.AddValue($"achievements_completed", 1); //tracks how many quests have been completed
            achievements.Remove(quest);
            questRunner.quest = null;
        }
    #endregion
}
