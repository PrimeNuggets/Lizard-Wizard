using System;
using UnityEngine;
using UnityEngine.InputSystem;

//-- Base for each controller type. ControllerHaptics picks the first one that matches the pad being used.
//-- Rumble is the same SetMotorSpeeds call for all of them, so only override what's actually different.
[Serializable]
public abstract class PadHaptics
{
    public bool enabled = true;
    [Range(0f, 1f), Tooltip("Some pads feel stronger than others, use this to even them out")]
    public float strength = 1f;

    public bool Handles(Gamepad pad) => enabled && pad != null && Matches(pad);
    protected abstract bool Matches(Gamepad pad);

    public virtual void OnConnected(Gamepad pad) { }
    public virtual void SetRumble(Gamepad pad, float low, float high) => pad.SetMotorSpeeds(low * strength, high * strength);
    public virtual void Pause(Gamepad pad) => pad.PauseHaptics();
    public virtual void Resume(Gamepad pad) => pad.ResumeHaptics();
    public virtual void Stop(Gamepad pad) => pad.ResetHaptics();
}
