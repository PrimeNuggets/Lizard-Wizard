using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

//-- WIP adaptive triggers for the DualSense. Unity doesn't have an API for these so we send the raw USB report ourselves.
//-- USB only for now, unity's own DualSense code doesn't do bluetooth output either
//-- Effect bytes are based on Nielk1's trigger effect generator (Feedback effect):
//-- https://gist.github.com/Nielk1/6d54cc2c00d2201ccb8c2720ad7538db
//-- Report layout from the linux hid-playstation driver:
//-- https://github.com/torvalds/linux/blob/master/drivers/hid/hid-playstation.c
[Serializable]
public class DualSenseTriggers
{
    [Serializable]
    public class TriggerSettings
    {
        public bool enabled = true;
        [Range(0, 9), Tooltip("How far into the pull the resistance starts, 0 = right away")]
        public int startZone = 3;
        [Range(1, 8)] public int strength = 4;
    }

    public bool enabled = false;
    public TriggerSettings left = new TriggerSettings();   //-- L2 / Spell1
    public TriggerSettings right = new TriggerSettings();  //-- R2 / Spell2

    private const byte EffectOff = 0x05;
    private const byte EffectFeedback = 0x21;

    public void Apply(Gamepad pad)
    {
        if (enabled) Send(pad, left.enabled ? left : null, right.enabled ? right : null);
        else Release(pad);
    }

    //-- triggers stay stiff after the game closes if we don't turn them off
    public void Release(Gamepad pad) => Send(pad, null, null);

    private static void Send(Gamepad pad, TriggerSettings l, TriggerSettings r)
    {
        if (!(pad is DualSenseGamepadHID)) return;

        int size = 48;
        try { size = HID.HIDDeviceDescriptor.FromJson(pad.description.capabilities).outputReportSize; }
        catch { }
        if (size <= 0 || size > TriggerReport.kMaxReport) return;

        var cmd = new TriggerReport
        {
            baseCommand = new InputDeviceCommand(TriggerReport.Type, InputDeviceCommand.BaseCommandSize + size),
            reportId = 0x02,
            flags0 = 0x04 | 0x08, //-- only touch the triggers, leaves rumble + light bar alone
        };

        var rb = Effect(r);
        cmd.r0 = rb[0]; cmd.r1 = rb[1]; cmd.r2 = rb[2]; cmd.r3 = rb[3]; cmd.r4 = rb[4]; cmd.r5 = rb[5]; cmd.r6 = rb[6];
        var lb = Effect(l);
        cmd.l0 = lb[0]; cmd.l1 = lb[1]; cmd.l2 = lb[2]; cmd.l3 = lb[3]; cmd.l4 = lb[4]; cmd.l5 = lb[5]; cmd.l6 = lb[6];

        pad.ExecuteCommand(ref cmd);
    }

    //-- resistance from startZone to the end of the pull
    private static byte[] Effect(TriggerSettings t)
    {
        var b = new byte[7];
        if (t == null)
        {
            b[0] = EffectOff;
            return b;
        }

        int zone = Mathf.Clamp(t.startZone, 0, 9);
        uint force = (uint)((Mathf.Clamp(t.strength, 1, 8) - 1) & 0x07);
        uint forceZones = 0;
        ushort activeZones = 0;
        for (int i = zone; i < 10; i++)
        {
            forceZones |= force << (3 * i);
            activeZones |= (ushort)(1 << i);
        }

        b[0] = EffectFeedback;
        b[1] = (byte)(activeZones & 0xff);
        b[2] = (byte)(activeZones >> 8);
        b[3] = (byte)(forceZones & 0xff);
        b[4] = (byte)(forceZones >> 8);
        b[5] = (byte)(forceZones >> 16);
        b[6] = (byte)(forceZones >> 24);
        return b;
    }

    //-- USB output report 0x02. Right trigger effect starts at byte 11, left at 22
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.BaseCommandSize + kMaxReport)]
    private struct TriggerReport : IInputDeviceCommandInfo
    {
        public const int kMaxReport = 64;
        private const int B = InputDeviceCommand.BaseCommandSize;
        public static FourCC Type => new FourCC('H', 'I', 'D', 'O');
        public FourCC typeStatic => Type;

        [FieldOffset(0)] public InputDeviceCommand baseCommand;
        [FieldOffset(B + 0)] public byte reportId;
        [FieldOffset(B + 1)] public byte flags0;

        [FieldOffset(B + 11)] public byte r0;
        [FieldOffset(B + 12)] public byte r1;
        [FieldOffset(B + 13)] public byte r2;
        [FieldOffset(B + 14)] public byte r3;
        [FieldOffset(B + 15)] public byte r4;
        [FieldOffset(B + 16)] public byte r5;
        [FieldOffset(B + 17)] public byte r6;

        [FieldOffset(B + 22)] public byte l0;
        [FieldOffset(B + 23)] public byte l1;
        [FieldOffset(B + 24)] public byte l2;
        [FieldOffset(B + 25)] public byte l3;
        [FieldOffset(B + 26)] public byte l4;
        [FieldOffset(B + 27)] public byte l5;
        [FieldOffset(B + 28)] public byte l6;
    }
}
