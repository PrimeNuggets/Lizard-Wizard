using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;

public class WaterLevelPercentScore : MonoBehaviour
{
    private int waterAmount;
    public TextMeshProUGUI waterLevelPercentText;
    //private PlayerInput playerInput;
    public CharacterController playerController;
    public Image WaterMeter;
    public float WaterLevel, MaxWaterLevel;
    public float WaterMovementMinDrainAmount;
    public float WaterMovementMaxDrainAmount;
    public float WaterMovementMinSpeed = 0.1f;
    public float WaterMovementMaxSpeed = 10.0f;
    public float WaterMagicDrainAmount;
    public float WaterTakeDamageDrainAmount;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        waterAmount = 100;
        waterLevelPercentText.text = "H20: " + waterAmount + "%";
        WaterMeter.fillAmount = MaxWaterLevel;
        //playerInput = GetComponent<PlayerInput>();
        playerController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        WaterMovementLoss();
    }

    private void WaterMovementLoss()
    {
        //gets player velocity to detrimine how fast the player is going
        Vector3 playerVelocity = playerController.velocity;
        if (playerVelocity.magnitude > WaterMovementMinSpeed)
        {
            //if player is moving at a slow enough pace
            WaterLevel -= WaterMovementMinDrainAmount * Time.deltaTime;
            if (WaterLevel < 0) WaterLevel = 0;
            waterLevelPercentText.text = "H20: " + WaterLevel.ToString("F2") + "%";
            WaterMeter.fillAmount = WaterLevel / MaxWaterLevel;
        }
        else if (playerVelocity.magnitude > WaterMovementMaxSpeed) 
        {
            //if the player is moving fast via a sprint or momentum
            WaterLevel -= WaterMovementMaxDrainAmount * Time.deltaTime;
            if (WaterLevel < 0) WaterLevel = 0;
            waterLevelPercentText.text = "H20: " + WaterLevel.ToString("F2") + "%";
            WaterMeter.fillAmount = WaterLevel / MaxWaterLevel;
        }
        else
        {
            //update just to be sure everything is working
            waterLevelPercentText.text = "H20: " + WaterLevel.ToString("F2") + "%";
        }



        /*if(Input.GetMouseButtonDown(0))
        {

        }*/
    }
}
