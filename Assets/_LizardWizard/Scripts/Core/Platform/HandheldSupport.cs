using System;
using UnityEngine;

//-- WIP prep for running on my lenovo legion go with steamos (windows build through proton)
//-- Detection is just hints, valve doesn't have an official way yet without steamworks:
//-- https://partner.steamgames.com/doc/steamdeck/recommendations
//-- Once we have steamworks, ISteamUtils.IsRunningOnSteamHardware / IsRunningUnderProton are the real checks:
//-- https://partner.steamgames.com/doc/api/ISteamUtils
[DisallowMultipleComponent]
public class HandheldSupport : MonoBehaviour
{
    [Header("Testing")]
    [SerializeField, Tooltip("Pretend we're on a handheld in the editor")] private bool simulateHandheld = false;

    //-- Steam sets these for games it launches. Not documented, found by the community so it could change
    //-- (proton passes the linux env through to the game so it should still see them)
    public static bool IsSteamOS =>
        Environment.GetEnvironmentVariable("SteamOS") == "1" ||
        Environment.GetEnvironmentVariable("SteamDeck") == "1";

    //-- Legion Go is 2560x1600, Go S is 1920x1200, both 16:10
    //-- https://psref.lenovo.com/syspool/Sys/PDF/Legion/Legion_Go_8APU1/Legion_Go_8APU1_Spec.pdf
    //-- https://psref.lenovo.com/syspool/Sys/PDF/Legion/Legion_Go_S_8APU1/Legion_Go_S_8APU1_Spec.PDF
    public static bool IsLegionGo =>
        SystemInfo.deviceModel.StartsWith("83E1", StringComparison.OrdinalIgnoreCase) ||
        SystemInfo.deviceModel.IndexOf("Legion", StringComparison.OrdinalIgnoreCase) >= 0;

    public static bool IsHandheld { get; private set; }

    void Awake()
    {
        IsHandheld = IsSteamOS || IsLegionGo || (Application.isEditor && simulateHandheld);
        Debug.Log($"[HandheldSupport] model: {SystemInfo.deviceModel}, steamos: {IsSteamOS}, legion go: {IsLegionGo}, handheld: {IsHandheld}");
    }
}
