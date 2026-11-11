using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;

//-- WIP, no xbox pad on me to test with yet. Uses the default rumble from PadHaptics
//-- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/api/UnityEngine.InputSystem.XInput.XInputController.html
[Serializable]
public class XboxPad : PadHaptics
{
    public XboxPad() { enabled = false; }

    protected override bool Matches(Gamepad pad) => pad is XInputController;
}
