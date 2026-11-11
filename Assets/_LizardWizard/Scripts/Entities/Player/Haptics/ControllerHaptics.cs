using System;
using UnityEngine;
using UnityEngine.InputSystem;

//-- Rumble for controllers. Put it on the Lizard next to Player.
//-- Doesn't need anything wired up, it just watches the player and input actions.
//-- The per controller stuff lives in the Controllers folder
[RequireComponent(typeof(Player))]
[DisallowMultipleComponent]
public class ControllerHaptics : MonoBehaviour
{
    [Serializable]
    public class RumblePulse
    {
        public bool enabled = true;
        [Range(0f, 1f)] public float low = 0.5f;   //-- big heavy motor
        [Range(0f, 1f)] public float high = 0.5f;  //-- small buzzy motor
        [Min(0f)] public float duration = 0.15f;
    }

    [Header("Vibration")]
    [SerializeField] private bool vibrationEnabled = true;
    [SerializeField, Range(0f, 1f)] private float intensity = 1f;

    [Header("Pulses")]
    [SerializeField] private RumblePulse onHurt = new RumblePulse { low = 0.9f, high = 0.6f, duration = 0.3f };
    [SerializeField] private RumblePulse onLand = new RumblePulse { low = 0.35f, high = 0.1f, duration = 0.08f };
    [SerializeField] private RumblePulse onSpellCast = new RumblePulse { low = 0.2f, high = 0.5f, duration = 0.12f };

    [Header("Controllers")]
    [SerializeField] private DualSensePad dualSense = new DualSensePad();
    [SerializeField] private LegionGoPad legionGo = new LegionGoPad();
    [SerializeField] private XboxPad xbox = new XboxPad();
    [SerializeField] private StandardPad standard = new StandardPad();

    private PadHaptics[] pads;
    private PadHaptics activePad;
    private Gamepad lastPad;

    private Player player;
    private InputAction[] spellActions;
    private bool wasHitPaused;
    private bool wasGrounded = true;

    private float pulseLow, pulseHigh, pulseTimeLeft;
    private bool motorsOn;
    private bool paused;

    void Start()
    {
        //-- order matters, first match wins. legion go before xbox since it'll probably look like one
        pads = new PadHaptics[] { dualSense, legionGo, xbox, standard };

        player = GetComponent<Player>();
        var input = GetComponent<PlayerInput>();
        if (input != null)
        {
            spellActions = new[]
            {
                input.actions.FindAction("Player/Spell1"),
                input.actions.FindAction("Player/Spell2"),
                input.actions.FindAction("Player/Spell3"),
                input.actions.FindAction("Player/Spell4"),
            };
        }
    }

    void Update()
    {
        Gamepad pad = Gamepad.current;

        //-- swapped controllers, clear the old one so it doesn't get stuck buzzing
        if (pad != lastPad)
        {
            if (lastPad != null) activePad?.Stop(lastPad);
            lastPad = pad;
            activePad = FindPad(pad);
            motorsOn = false;
            if (activePad != null) activePad.OnConnected(pad);
        }
        if (activePad == null) return;

        //-- game paused (MenuUIScript sets timeScale to 0)
        bool nowPaused = Time.timeScale == 0f;
        if (nowPaused != paused)
        {
            paused = nowPaused;
            if (paused) activePad.Pause(pad);
            else activePad.Resume(pad);
        }
        if (paused) return;

        CheckEvents();
        TickPulse(pad);
    }

    private PadHaptics FindPad(Gamepad pad)
    {
        if (pad == null || pads == null) return null;
        foreach (var p in pads)
            if (p.Handles(pad)) return p;
        return null;
    }

    private void CheckEvents()
    {
        Entity data = player.entityData;
        if (data == null) return;

        //-- hit pause only kicks in when we get hit so use that as the hurt signal
        bool hitPaused = data.IsHitPaused;
        if (hitPaused && !wasHitPaused) Play(onHurt);
        wasHitPaused = hitPaused;

        if (data.entChar != null)
        {
            bool grounded = data.entChar.isGrounded;
            if (grounded && !wasGrounded) Play(onLand);
            wasGrounded = grounded;
        }

        if (spellActions != null)
        {
            foreach (var action in spellActions)
            {
                if (action != null && action.WasPressedThisFrame())
                {
                    Play(onSpellCast);
                    break;
                }
            }
        }
    }

    //-- other scripts can call this too for one off stuff
    public void Play(RumblePulse pulse)
    {
        if (pulse == null || !pulse.enabled) return;
        Play(pulse.low, pulse.high, pulse.duration);
    }

    public void Play(float low, float high, float duration)
    {
        if (!vibrationEnabled) return;
        //-- stronger one wins if two overlap
        pulseLow = Mathf.Max(pulseLow, low);
        pulseHigh = Mathf.Max(pulseHigh, high);
        pulseTimeLeft = Mathf.Max(pulseTimeLeft, duration);
    }

    private void TickPulse(Gamepad pad)
    {
        if (pulseTimeLeft > 0f && vibrationEnabled)
        {
            activePad.SetRumble(pad, pulseLow * intensity, pulseHigh * intensity);
            motorsOn = true;
            pulseTimeLeft -= Time.unscaledDeltaTime;
            return;
        }

        pulseLow = pulseHigh = pulseTimeLeft = 0f;
        if (motorsOn)
        {
            activePad.SetRumble(pad, 0f, 0f);
            motorsOn = false;
        }
    }

    //-- lets you mess with the settings in play mode and see it right away
    void OnValidate()
    {
        if (!Application.isPlaying || lastPad == null) return;

        if (activePad == dualSense)
        {
            dualSense.RefreshLightBar(lastPad);
            dualSense.adaptiveTriggers.Apply(lastPad);
        }
        if (!vibrationEnabled) lastPad.SetMotorSpeeds(0f, 0f);

        //-- re-pick in case a pad got toggled on/off
        PadHaptics picked = FindPad(lastPad);
        if (picked != activePad)
        {
            activePad?.Stop(lastPad);
            activePad = picked;
            motorsOn = false;
            activePad?.OnConnected(lastPad);
        }
    }

    void OnDisable()
    {
        //-- otherwise the pad keeps rumbling after you stop play mode
        if (lastPad != null) activePad?.Stop(lastPad);
        lastPad = null;
        activePad = null;
        motorsOn = false;
        pulseTimeLeft = 0f;
    }
}
