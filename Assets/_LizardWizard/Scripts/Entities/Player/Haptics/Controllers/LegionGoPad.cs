using System;
using UnityEngine.InputSystem;

//-- Through steam input the built in controls show up as a regular xbox pad:
//-- https://partner.steamgames.com/doc/features/steam_controller/getting_started_for_devs
//-- so we lean on HandheldSupport to know we're on the legion go.
[Serializable]
public class LegionGoPad : PadHaptics
{
    protected override bool Matches(Gamepad pad)
    {
        if (HandheldSupport.IsLegionGo && pad is UnityEngine.InputSystem.XInput.XInputController) return true;

        string product = pad.description.product;
        return !string.IsNullOrEmpty(product) && product.IndexOf("Legion", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
