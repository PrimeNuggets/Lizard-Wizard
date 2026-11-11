using System;
using UnityEngine.InputSystem;

//-- WIP placeholder. this will be for down the road when i add steamos support for my handheld lenovo legion go
//-- Through steam input it'll probably just show up as an xbox style pad and get caught by XboxPad,
//-- so this only kicks in if the device name actually says Legion. Check what it reports once i test on it
[Serializable]
public class LegionGoPad : PadHaptics
{
    public LegionGoPad() { enabled = false; }

    protected override bool Matches(Gamepad pad)
    {
        string product = pad.description.product;
        return !string.IsNullOrEmpty(product) && product.IndexOf("Legion", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
