using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

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
    //===========================================
        [Header("Misc Data")]
    //===========================================
    [SerializeField] private Terrain terrain;
    [TextArea] public List<string> dialogue;
    [LabelOverride("Interaction Range")] public float iRange = 8f;
    private InputAction interactAction;
    //=========================================================
        //Non-Serialized Variables
    //=========================================================
    private Aspect player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //=========================================================
            //Entity Instantiation
        //=========================================================
        entityData = Instantiate(entityTemplate);
        entityData.Initialize(gameObject, terrain);
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
        var playerInputs = player.gameObject.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        interactAction = playerInputs.actions.FindAction("Player/Interact", true);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        entityData.ApplyMove(Vector2.zero, false);
        PlayerNear(player);
    }

    public void PlayerNear(Aspect aspect)
    {
        // Check if the player is close enough
        float distance = Vector3.Distance(transform.position, aspect.transform.position);

        if (distance <= iRange && aspect.aspectName == Aspect.aspect.Player)
        {
            if (canvas.activeSelf == false)
            {
                buttonText.text = interactAction.GetBindingDisplayString(0, InputBinding.DisplayStringOptions.DontIncludeInteractions); //Fix later to account for controllers
                canvas.SetActive(true); //Ideally start an activation animation: fade in & slide to position
            }
        } else
        {
            if (canvas.activeSelf == true)
            {
                buttonText.text = interactAction.GetBindingDisplayString(0, InputBinding.DisplayStringOptions.DontIncludeInteractions);
                canvas.SetActive(false); //Ideally start an deactivation animation: fade out & slide to downward
            }
        }
    }

    //Executes once-per-tick after Update
    void LateUpdate()
    {
        Vector3 direction = player.transform.position - canvas.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            canvas.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
        }
    }


    void OnDrawGizmosSelected()
    {
        // -- Show interaction range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, iRange);
    }
}
