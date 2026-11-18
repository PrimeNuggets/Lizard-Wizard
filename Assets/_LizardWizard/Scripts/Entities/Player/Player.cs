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
            private bool jumpRequested; //-- set in Update, used in FixedUpdate so presses don't get missed
            private InputAction crouchAction;
            private float pitch;
            //-- trick boost: land a full flip = short speed burst that fades out
            private const float TrickBoostDuration = 0.45f;
            private const float TrickBoostSpeed = 17f;
            private float trickBoostRemaining;
            private Vector3 trickBoostDirection;
            private bool trickBoostAppliedThisTick;
        #endregion
        //=========================================
        #region Spells
            public Transform firePosition;
            private InputAction spell1Action;
            private InputAction spell2Action;
            private InputAction spell3Action;
            private InputAction spell4Action;
            [SerializeField] private SpellData[] spells;
        #endregion
        //=========================================
        private LizardAnimation lizardAnimation;
        private MenuUIScript menu;
        //=========================================
        #region Quests
            [Header("Quest Data")]
            [SerializeField] private Quests tutorialQuest;
            [SerializeField] private QuestRunner questRunner;
            [NonSerialized] public List<Quests> ongoingQuests = new List<Quests>();
            [NonSerialized] public List<Quests> completedQuests = new List<Quests>();
            public List<Quests> achievements;
            [NonSerialized] public List<Quests> completedAchievements = new List<Quests>();
            [NonSerialized] private float movementToAchieve;
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
        [NonSerialized] public AttackData currentAttack;
        //=========================================
    #endregion

    public bool IsTrickBoosting => trickBoostRemaining > 0f && entityData.ctrl && !entityData.IsHitPaused;
    //-- also true on the last boost tick, so the water meter doesn't drain on that frame
    public bool IsTrickBoostMovement => IsTrickBoosting || (trickBoostAppliedThisTick && entityData.ctrl && !entityData.IsHitPaused);
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //=========================================================
        #region Controls
            var input = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            lizardAnimation = GetComponent<LizardAnimation>();
            menu = FindAnyObjectByType<MenuUIScript>();

            moveAction = input.actions.FindAction("Player/Move", true);
            lookAction = input.actions.FindAction("Player/Look", true);
            jumpAction = input.actions.FindAction("Player/Jump", true);
            crouchAction = input.actions.FindAction("Player/Crouch", true);
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
            for (var i = 0; i < spells.Length; i++)
            {
                SetSpell(spells[i], i); //Initializes each spell in the array
            }
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
        //-- one jump per press, holding doesn't re-jump on landing (holding in the air still flips)
        if (Time.timeScale > 0f && jumpAction.WasPressedThisFrame())
        {
            jumpRequested = true;
        }
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
        //===========================================================
        #region Spells
            //NOTE: This implementation of a 'released' function hypothetically might miss sometimes.
            //Stress testing i havent gotten it to miss yet, but if it starts missing(esp in high framerate) try changing updateMode to FixedUpdate
            //or move this to update ig idk
            for (var i = 0; i < spells.Length; i++)
            {
                var spell = spells[i];
                if (spell != null)
                {
                    switch (i)
                    {
                        case 0:
                            if (spell1Action.WasPressedThisFrame()) OnSpell(i);
                            if (spell1Action.WasReleasedThisFrame()) OnSpellReleased(i);
                            break;
                        case 1:
                            if (spell2Action.WasPressedThisFrame()) OnSpell(i);
                            if (spell2Action.WasReleasedThisFrame()) OnSpellReleased(i);
                            break;
                        case 2:
                            if (spell3Action.WasPressedThisFrame()) OnSpell(i);
                            if (spell3Action.WasReleasedThisFrame()) OnSpellReleased(i);
                            break;
                        case 3:
                            if (spell4Action.WasPressedThisFrame()) OnSpell(i);
                            if (spell4Action.WasReleasedThisFrame()) OnSpellReleased(i);
                            break;
                    }
                    spell.CastSpellTick(Time.deltaTime);
                }
            }
        #endregion
    }
    void FixedUpdate() //Using this saves a lot of fps
    {
        //=========================================================
        #region Entity Processing
            entityData?.BeginTick();
            trickBoostAppliedThisTick = false;
            bool jumpPressed = jumpRequested;
            jumpRequested = false;
            //-- getting hit or losing control cancels the boost and the flip
            if (!entityData.ctrl || entityData.IsHitPaused)
            {
                trickBoostRemaining = 0f;
                lizardAnimation?.ResetTrick();
            }
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
            bool wasGrounded = entityData.entChar.isGrounded;
            bool jumped = entityData.jumped; //stores the jumped bool before it updates
            bool crouchPressed = crouchAction.IsPressed();
            entityData.crouched = crouchPressed;

            Vector3 previousPosition = transform.position;

            //-- jumping again ends the boost early
            if (jumpPressed && wasGrounded) trickBoostRemaining = 0f;
            //-- boost fades from full speed to 0 over the duration
            Vector3 boostVelocity = Vector3.zero;
            if (IsTrickBoosting)
            {
                trickBoostAppliedThisTick = true;
                boostVelocity = trickBoostDirection * (TrickBoostSpeed * trickBoostRemaining / TrickBoostDuration);
                trickBoostRemaining = Mathf.Max(0f, trickBoostRemaining - Time.fixedDeltaTime);
            }
            entityData.ApplyMove(move, jumpPressed && entityData.ctrl, cameraRef.transform, boostVelocity);

            Vector3 movement = transform.position - previousPosition;
            movement.y = 0f;

            movementToAchieve += movement.magnitude;

            int wholeMovingUnits = Mathf.FloorToInt(movementToAchieve);

            if (wholeMovingUnits > 0)
            {
                Scoring.AddValue("lizard.move", wholeMovingUnits);
                movementToAchieve -= wholeMovingUnits;
            }

            bool landed = !wasGrounded && entityData.entChar.isGrounded;

            //-- clean flip landing = boost + sand puff + rumble + text
            if (landed && entityData.ctrl && lizardAnimation != null && lizardAnimation.ConsumeCleanTrickLanding())
            {
                //-- boost the way we're moving, or where the camera faces if we landed standing still
                Vector3 boostDirection = movement.sqrMagnitude > 0.0001f
                    ? movement.normalized
                    : Vector3.ProjectOnPlane(cameraRef.transform.forward, Vector3.up).normalized;
                trickBoostDirection = boostDirection;
                trickBoostRemaining = TrickBoostDuration;
                lizardAnimation.PlayTrickLanding(boostDirection);
                GetComponent<ControllerHaptics>()?.Play(0.7f, 0.45f, 0.19f);
                menu?.ShowTrickBoost();
            }

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
            //NOTE: This implementation of a 'released' function hypothetically might miss sometimes.
            //Stress testing i havent gotten it to miss yet, but if it starts missing(esp in high framerate) try changing updateMode to FixedUpdate
            //or move this to update ig idk
            if (crouchAction.WasReleasedThisFrame())
            {
                OnCrouchReleased();
            }
        #endregion
    }
    void LateUpdate()
    {
        #region Entity Processing
            entityData?.AfterTick();
            //-- keep the slide pose during the boost even if we bounce off a dune for a frame
            if (IsTrickBoosting && entityData.animator != null) entityData.animator.SetBool("Grounded", true);
        #endregion
    }
    void OnDisable()
    {
        jumpRequested = false;
        trickBoostRemaining = 0f;
        trickBoostAppliedThisTick = false;
        lizardAnimation?.ResetTrick();
        #region Entity Processing
            entityData?.Disable();
        #endregion
        MenuUIScript.SetCursorLocked(false);
    }
    void OnDestroy()
    {
        #region Entity Processing
            foreach (var spell in spells) Destroy(spell);
            Destroy(entityData);
        #endregion
    }
    #region Movement Methods
        void OnCrouch()
        {
            entityData.CrouchDown();
        }
        void OnCrouchReleased()
        {
            entityData.CrouchUp();
        }
    #endregion
    #region Spell Methods
        void OnSpell(int slot)
        {
            if (slot >= spells.Length) return;
            currentAttack = spells[slot];
            spells[slot].CastSpellDown();
        }
        void OnSpellReleased(int slot)
        {
            if (slot >= spells.Length) return;
            spells[slot].CastSpellReleased();
        }
        public SpellData GetSpell(int slot) => spells != null && slot >= 0 && slot < spells.Length ? spells[slot] : null; //-- for the hud
        public void SetSpell(SpellData spell, int slot){
            spells[slot] = Instantiate(spell);
            spells[slot].root = gameObject;
            spells[slot].rootData = entityData;
            spells[slot].firePosition = firePosition;
        }
    #endregion
    public Vector3 GetLookDir() { //Subject to move to Entity.cs
       //OKAY IM DOING DUMB SHIT FOR THIS
       //the children gotta be in the right order
       //camera ref is 2nd, camera 1st inside that, reticle 1st inside that
       //dumbass way to do this smh my head
       //-vv
        return entityData.GetLookDir(GetComponentInChildren<Camera>(), transform.GetChild(1).GetChild(0).GetChild(0));
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
            questRunner.TrackAchievement(quest, OnAchievementEnd);
        }
        public void OnAchievementEnd(Quests quest)
        {
            completedAchievements.Add(quest);

            Scoring.AddValue($"achievement.{quest.questId}.completed", 1); //tracks which achievements have been completed
            Scoring.AddValue($"achievements_completed", 1); //tracks how many achievements have been completed

            achievements.Remove(quest);
        }
    #endregion
}
