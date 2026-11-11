using System;
using UnityEngine.InputSystem;

//-- Catch all for any other gamepad (DS4, switch pro, etc). Not tested, off by default
[Serializable]
public class StandardPad : PadHaptics
{
    public StandardPad() { enabled = false; }

    protected override bool Matches(Gamepad pad) => true;
}
