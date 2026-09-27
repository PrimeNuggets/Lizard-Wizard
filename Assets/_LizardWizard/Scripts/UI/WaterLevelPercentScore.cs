using UnityEngine;
using TMPro;

public class WaterLevelPercentScore : MonoBehaviour
{
    private int waterAmount;
    public TextMeshProUGUI waterLevelPercentText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        waterAmount = 100;
        waterLevelPercentText.text = "H20: " + waterAmount + "%";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
