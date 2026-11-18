using System.Collections.Generic;
using UnityEngine;

//-- Frame rate + resolution options for the pause menu settings. saved in PlayerPrefs
//-- (took over the fps cap that used to be in HandheldSupport)
public class DisplaySettings
{
    private const string BatterySavingKey = "Display.BatterySaving";
    private const string WidthKey = "Display.Width";
    private const string HeightKey = "Display.Height";

    private List<Vector2Int> resolutions = new List<Vector2Int>();
    private int selectedResolution;

    public bool BatterySaving { get; private set; }
    public Vector2Int Resolution => resolutions[selectedResolution];

    public void Load()
    {
        //-- uncapped by default, battery saving (60 fps) is opt in
        BatterySaving = PlayerPrefs.GetInt(BatterySavingKey) == 1;
        ApplyFrameRate();

        //-- native res first, then common smaller ones with the same aspect ratio
        //-- Legion Go is 2560x1600 so the 16:10 ones are for that
        Resolution desktopMode = Screen.currentResolution;
        Vector2Int native = new Vector2Int(desktopMode.width, desktopMode.height);
        resolutions.Clear();
        resolutions.Add(native);
        AddResolution(native, 2560, 1600);
        AddResolution(native, 2560, 1440);
        AddResolution(native, 1920, 1200);
        AddResolution(native, 1920, 1080);
        AddResolution(native, 1600, 900);
        AddResolution(native, 1280, 800);
        AddResolution(native, 1280, 720);

        Vector2Int saved = new Vector2Int(PlayerPrefs.GetInt(WidthKey), PlayerPrefs.GetInt(HeightKey));
        selectedResolution = Mathf.Max(0, resolutions.IndexOf(saved));
        if (saved.x > 0 && saved.y > 0) ApplyResolution();
    }

    private void AddResolution(Vector2Int native, int width, int height)
    {
        float nativeAspect = (float)native.x / native.y;
        float aspect = (float)width / height;
        if (width < native.x && height < native.y && Mathf.Abs(aspect - nativeAspect) < 0.02f)
        {
            resolutions.Add(new Vector2Int(width, height));
        }
    }

    public void ToggleBatterySaving()
    {
        BatterySaving = !BatterySaving;
        ApplyFrameRate();
        PlayerPrefs.SetInt(BatterySavingKey, BatterySaving ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void CycleResolution()
    {
        selectedResolution = (selectedResolution + 1) % resolutions.Count;
        ApplyResolution();
        PlayerPrefs.SetInt(WidthKey, Resolution.x);
        PlayerPrefs.SetInt(HeightKey, Resolution.y);
        PlayerPrefs.Save();
    }

    private void ApplyFrameRate()
    {
        //-- targetFrameRate gets ignored unless vsync is off
        //-- https://docs.unity.com/en-us/engine/current/script-reference/unityengine/application/targetframerate
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = BatterySaving ? 60 : -1;
    }

    private void ApplyResolution()
    {
        Screen.SetResolution(Resolution.x, Resolution.y, Screen.fullScreenMode);
    }
}
