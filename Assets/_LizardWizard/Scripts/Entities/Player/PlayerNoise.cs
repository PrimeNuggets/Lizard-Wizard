using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerNoise : MonoBehaviour
{
    public SpiderHearing spiderHearing;

    private InputAction jumpAction;

    void Start()
    {
        PlayerInput input = GetComponent<PlayerInput>();
        jumpAction = input.actions.FindAction("Player/Jump", true);
    }

    void Update()
    {
        // -- Send noise when the player jumps
        if (spiderHearing != null && jumpAction.WasPressedThisFrame())
        {
            spiderHearing.OnNoiseHeard(transform.position);
        }
    }
}