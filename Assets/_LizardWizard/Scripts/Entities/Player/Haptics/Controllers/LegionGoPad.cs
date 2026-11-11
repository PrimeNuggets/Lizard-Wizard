using System;
using UnityEngine.InputSystem;

//-- WIP placeholder. this will be for down the road when i add steamos support for my handheld lenovo legion go
//-- Through steam input the built in controls show up as a regular xbox pad:
//-- https://partner.steamgames.com/doc/features/steam_controller/getting_started_for_devs
//-- so we lean on HandheldSupport to know we're on the legion go. Check what it reports once i test on it
[Serializable]
public class LegionGoPad : PadHaptics
{
    public LegionGoPad() { enabled = false; }

    protected override bool Matches(Gamepad pad)
    {
        if (HandheldSupport.IsLegionGo && pad is UnityEngine.InputSystem.XInput.XInputController) return true;

        string product = pad.description.product;
        return !string.IsNullOrEmpty(product) && product.IndexOf("Legion", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
