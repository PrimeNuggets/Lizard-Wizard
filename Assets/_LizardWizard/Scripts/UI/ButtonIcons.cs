using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

//-- what button to show on screen. keyboard = key name, ps = shapes, xbox = colored letters
//-- △ comes from the Inter font fallback, liberation sans doesnt have it. no ✕ in either so its ×
public static class ButtonIcons
{
    public static string For(InputAction action, PlayerInput input)
    {
        if (action == null) return "";
        bool onPad = input != null && input.currentControlScheme == "Gamepad";
        InputControl control = onPad ? FindControl(action, Gamepad.current) : FindControl(action, Keyboard.current, Mouse.current);
        if (control == null) return action.GetBindingDisplayString(0, InputBinding.DisplayStringOptions.DontIncludeInteractions);
        return Icon(control);
    }

    //-- first binding that lands on one of these devices
    public static InputControl FindControl(InputAction action, params InputDevice[] devices)
    {
        foreach (var binding in action.bindings)
        {
            foreach (var device in devices)
            {
                if (device == null) continue;
                InputControl control = InputControlPath.TryFindControl(device, binding.effectivePath);
                if (control != null) return control;
            }
        }
        return null;
    }

    public static string Icon(InputControl control)
    {
        if (control.device is Gamepad)
        {
            bool playstation = control.device is DualShockGamepad; //-- DualSense counts as this too
            switch (control.name)
            {
                case "buttonNorth": return playstation ? "△" : "<color=#F2C200>Y</color>";
                case "buttonEast": return playstation ? "○" : "<color=#E5484D>B</color>";
                case "buttonSouth": return playstation ? "×" : "<color=#2FB872>A</color>";
                case "buttonWest": return playstation ? "□" : "<color=#4A7BE0>X</color>";
            }
        }
        return string.IsNullOrEmpty(control.shortDisplayName) ? control.displayName : control.shortDisplayName;
    }
}
