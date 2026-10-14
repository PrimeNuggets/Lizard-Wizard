using UnityEngine;
using TMPro;

public class BasicTimer : MonoBehaviour
{
    public float time;
    public TextMeshProUGUI timerText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        Clock();
    }

    public void Clock()
    {
        time += Time.deltaTime;
        //set up minutes and seconds
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        //format the display so numbers come out right as MM:SS
        timerText.text = string.Format("Time: " + "{0:00}:{1:00}", minutes, seconds);
    }
}
