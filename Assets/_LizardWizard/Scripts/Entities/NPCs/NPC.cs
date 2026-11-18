using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Aspect))]
[RequireComponent(typeof(CharacterController))]
public class NPC : MonoBehaviour
{
    //===========================================
        [Header("Entity Data")]
    //===========================================
    [LabelOverride("Data")] public Entity entityTemplate;
    [NonSerialized] public Entity entityData;
    //===========================================
        [Header("NPC Data")]
    //===========================================
    [SerializeField, LabelOverride("Interaction Canvas")] private GameObject canvas;
    [SerializeField, LabelOverride("Interaction Button Text")] private TMP_Text buttonText;
    [SerializeField, Tooltip("Renderer used to position the prompt beside the NPC.")] protected Renderer promptBoundsRenderer;
    [SerializeField, LabelOverride("Controller Button Sprite"), Tooltip("round button used on controller")] private Sprite padButtonSprite;
    [SerializeField, LabelOverride("Controller Button Color")] private Color padButtonColor = new Color(0.17f, 0.17f, 0.17f);
    [SerializeField, LabelOverride("PlayStation Button Color"), Tooltip("white like the dualsense buttons")] private Color psButtonColor = new Color(0.95f, 0.95f, 0.95f);
    [SerializeField, LabelOverride("Controller Button Size"), Range(0.3f, 1f)] private float padButtonScale = 0.75f;
    //===========================================
        [Header("Misc Data")]
    //===========================================
    [SerializeField] private Transform mesh;
    [SerializeField] public List<Dialogue.ConvoConds> dialogue;
    [LabelOverride("Interaction Range")] public float iRange = 8f;
    private InputAction interactAction;
    private PlayerInput playerInputs;
    private Image buttonBox; //-- the box behind the button text
    private Sprite keySprite;
    private Color keyColor;
    //=========================================================
        //Non-Serialized Variables
    //=========================================================
    private Aspect player;
    private Dialogue dialogueUI;
    private bool waitForRelease = false; //Waits for the interaction button release
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        //=========================================================
            //Entity Instantiation
        //=========================================================
        entityData = Instantiate(entityTemplate);
        entityData.Initialize(gameObject, mesh);
        entityData.water.Set(0); //All NPCs start with no water
        //=========================================================
            //Player Lookup
        //=========================================================
        var aspects = FindObjectsByType<Aspect>();
        foreach (var aspect in aspects)
        {
            if (aspect.aspectName == Aspect.aspect.Player)
            {
                player = aspect;
                break; //This logic will need to be altered if multiplayer is desired
            }
        }
        playerInputs = player.gameObject.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        interactAction = playerInputs.actions.FindAction("Player/Interact", true);
        buttonBox = buttonText.transform.parent.GetComponent<Image>();
        if (buttonBox != null)
        {
            keySprite = buttonBox.sprite;
            keyColor = buttonBox.color;
        }
        //=========================================================
            //Dialogue Lookup
        //=========================================================
        dialogueUI = FindAnyObjectByType<Dialogue>(FindObjectsInactive.Include);
    }

    //-- keycap box on keyboard, round button on controller
    //-- ps = white button dark shape, xbox = dark button colored letter
    private void UpdatePrompt()
    {
        bool onPad = playerInputs != null && playerInputs.currentControlScheme == "Gamepad";
        bool playstation = Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad;

        buttonText.text = ButtonIcons.For(interactAction, playerInputs);

        if (buttonBox != null && padButtonSprite != null)
        {
            buttonBox.sprite = onPad ? padButtonSprite : keySprite;
            buttonBox.color = !onPad ? keyColor : playstation ? psButtonColor : padButtonColor;
            buttonBox.rectTransform.localScale = Vector3.one * (onPad ? padButtonScale : 1f);
        }
    }

    // Update is called once per frame
    protected virtual void FixedUpdate()
    {
        entityData?.BeginTick();
        if (entityData.IsHitPaused)
        {
            entityData.ApplyMove(Vector2.zero, false);
            return;
        }
        entityData.ApplyMove(Vector2.zero, false);
        PlayerNear(player);
    }

    void OnDisable()
    {
        entityData?.Disable();
    }
    //Executes once-per-tick after Update
    void LateUpdate()
    {
        //entityData?.AfterTick(); //Aligns the entity to the ground. Since some npcs will be stuck in sand or similar situations, I'm not sure we should do this. - Xavier

        if (canvas.activeSelf && promptBoundsRenderer != null && Camera.main is Camera viewCamera)
        {
            Bounds bounds = promptBoundsRenderer.bounds;
            Vector3 viewRight = viewCamera.transform.right;
            Vector3 absoluteViewRight = new Vector3(Mathf.Abs(viewRight.x), Mathf.Abs(viewRight.y), Mathf.Abs(viewRight.z));
            float halfWidth = Vector3.Dot(bounds.extents, absoluteViewRight);
            Vector3 promptPosition = bounds.center - viewRight * (halfWidth + 0.4f);
            float bobOffset = Mathf.Sin(Time.time * Mathf.PI) * 0.1f; //-- one bounce every two seconds
            promptPosition += viewCamera.transform.up * bobOffset;
            canvas.transform.SetPositionAndRotation(promptPosition, viewCamera.transform.rotation);
            return;
        }

        Vector3 direction = player.transform.position - canvas.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            canvas.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
        }
    }

    public void PlayerNear(Aspect aspect)
    {
        // Check if the player is close enough
        float distance = Vector3.Distance(transform.position, aspect.transform.position);

        bool pressed = interactAction.IsPressed();
        bool newPressed = pressed && !waitForRelease;
        waitForRelease = pressed;

        bool showAlternatePrompt = ShowAlternatePrompt(distance);
        if (buttonBox != null) buttonBox.gameObject.SetActive(!showAlternatePrompt);
        if (showAlternatePrompt)
        {
            canvas.SetActive(true);
            return;
        }

        if (distance <= iRange && aspect.aspectName == Aspect.aspect.Player)
        {
            UpdatePrompt(); //-- every frame so it swaps if you pick up the controller mid prompt
            if (canvas.activeSelf == false)
            {
                canvas.SetActive(true); //Ideally start an activation animation: fade in & slide to position
            } else
            {
                if (newPressed && !Dialogue.inDialogue) {
                    dialogueUI.StartDialogue(this);
                }
            }
        } else
        {
            if (canvas.activeSelf == true)
            {
                canvas.SetActive(false); //Ideally start an deactivation animation: fade out & slide to downward
            }
            if (Dialogue.inDialogue && dialogueUI.npc == this) { //Prevents other npcs from ending other's dialogues
                dialogueUI.EndDialogue();
            }
        }
    }

    protected virtual bool ShowAlternatePrompt(float distance) => false;

    public virtual Conversations GetConversation()
    {
        //Sets the conversation to the first available convo
        return dialogue.FirstOrDefault(option => option.ParseConditions())?.convo;
    }

    public virtual void OnDialogueFinished(Player speaker) { }

    /*public void ChangeDialogue(string line)
    {
        //Change here
    }*/

    void OnDrawGizmosSelected()
    {
        // -- Show interaction range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, iRange);
    }

    public void Save(float water)
    {
        entityData.water.Set(water);
        var player = this.player.gameObject.GetComponent<Player>();
        player.entityData.water.Set(
            player.entityData.water.GetCurrent() -
            water
        );
        RecordRescue();
    }

    protected void RecordRescue(string scoreKey = null)
    {
        //Update Saved Count
        Scoring.AddValue("npc.saved", 1);
        Scoring.AddValue(scoreKey ?? $"npc.{gameObject.name.ToLowerInvariant()}.saved", 1);
    }
}
