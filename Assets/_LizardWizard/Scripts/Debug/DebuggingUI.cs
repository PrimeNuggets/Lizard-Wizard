using System.Collections.Generic;
using TMPro;
using UnityEngine;

//Xavier: This script will handle developer tools that we want to add
public class DebuggingUI : MonoBehaviour
{
    //==========================================================================
    [SerializeField, LabelOverride("Developer Mode")] private bool devMode;
    //==========================================================================
    [Header("FPS")]
    [SerializeField, LabelOverride("FPS Object")] GameObject fpsObj;
    public class FpsDisplay
    {
        public float value = 60;
        public TMP_Text text;
        public float updateInterval = 0.25f;
        public float updateTimer = 0f;
        public float frames = 0;
    }
    private readonly FpsDisplay fps = new FpsDisplay();
    //==========================================================================
    void Start()
    {
        fps.text = fpsObj.GetComponent<TMP_Text>();
    }

    void Update()
    {
        fps.updateTimer += Time.unscaledDeltaTime;
        fps.frames++;
        if (fps.updateTimer >= fps.updateInterval)
        {
            fps.value = fps.frames / fps.updateTimer;
            fps.text.SetText("FPS: {0:0}", fps.value);

            fps.updateTimer = 0f;
            fps.frames = 0;
        }
        if (devMode)
        {
            fpsObj.SetActive(true);
        } else
        {
            fpsObj.SetActive(false);
        }
    }
}
