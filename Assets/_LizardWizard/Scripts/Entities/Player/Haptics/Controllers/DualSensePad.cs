using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

//-- PS5 controller. Rumble + light bar through Unity's Input System:
//-- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/api/UnityEngine.InputSystem.DualShock.DualSenseGamepadHID.html
[Serializable]
public class DualSensePad : PadHaptics
{
    public bool lightBarEnabled = true;
    public Color lightBarColor = new Color(0.2f, 0.9f, 0.4f);

    [Tooltip("WIP, USB only for now")]
    public DualSenseTriggers adaptiveTriggers = new DualSenseTriggers();

    protected override bool Matches(Gamepad pad) => pad is DualSenseGamepadHID;

    public override void OnConnected(Gamepad pad)
    {
        RefreshLightBar(pad);
        adaptiveTriggers.Apply(pad);
    }

    public void RefreshLightBar(Gamepad pad)
    {
        if (pad is DualSenseGamepadHID ds)
            ds.SetLightBarColor(lightBarEnabled ? lightBarColor : Color.black);
    }

    //-- let go of the triggers while paused so menus don't feel weird
    public override void Pause(Gamepad pad)
    {
        base.Pause(pad);
        adaptiveTriggers.Release(pad);
    }

    public override void Resume(Gamepad pad)
    {
        base.Resume(pad);
        adaptiveTriggers.Apply(pad);
    }

    public override void Stop(Gamepad pad)
    {
        base.Stop(pad);
        adaptiveTriggers.Release(pad);
    }
}
