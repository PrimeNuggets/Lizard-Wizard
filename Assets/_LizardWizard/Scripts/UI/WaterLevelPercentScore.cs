/*
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using static Entity;
*/

using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using static Entity;
using Unity.VisualScripting;
using System.Threading.Tasks;
using System.Collections;

public class WaterLevelPercentScore : MonoBehaviour
{
    //=========================================
    [Header("Entity Data")]
    [LabelOverride("Data")] public Entity entityTemplate;
    [NonSerialized] public Entity entityData;
    //=============
    //Water Capacity
    //[NonSerialized] public float water;
    [SerializeField] public Stat water;
    [SerializeField] public Stat health;
    [SerializeField] public Terrain terrain;
    //=========================================


    //public int waterAmount;
    public TextMeshProUGUI waterLevelPercentText;
    //private PlayerInput playerInput;
    public CharacterController playerController;
    public Collider playerCollider;
    public Image WaterMeter;

    public float WaterLevel, MaxWaterLevel;
    public float WaterMovementMinDrainAmount;
    //public float WaterMovementMaxDrainAmount;
    public float WaterMovementMinSpeed = 0.1f;
    //public float WaterMovementMaxSpeed = 10.0f;
    public float WaterMagicDrainAmount;
    public float WaterTakeDamageDrainAmount;
    //public float WaterEmptyValue = 0;
    public Image HeartShieldIcon;
    public Image HeartFrailIcon;
    public Image HeartBrokenIcon;
    private bool IsHeartShieldBroken = false;
    private float WaterNearEmptyValue = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        entityData = Instantiate(entityTemplate);
        entityData.Initialize(gameObject); 
        //for some reason the water text and fill is set to half when the terrain is involved
        //Debug.Log(entityData.water.GetCurrent().ToString());
        //entityData.water.Initialize(water.GetMax());
        entityData.water.GetCurrent();
        playerCollider = GetComponent<Collider>();
        HeartShieldIcon.gameObject.SetActive(false);
        HeartFrailIcon.gameObject.SetActive(false);
        HeartBrokenIcon.gameObject.SetActive(false);
        IsHeartShieldBroken = false;

        entityData.water.Set(100);
        //entityData.water.Set(entityData.water.GetMax());
        Debug.Log(entityData.water.GetCurrent().ToString());
        WaterLevel = entityData.water.GetCurrent();
        //MaxWaterLevel = 100;
        //waterAmount = 100;
        //waterLevelPercentText.text = "H20: " + waterAmount + "%";
        waterLevelPercentText.text = "H20: " + entityData.water.GetCurrent().ToString("F2") + "%";
        //Debug.Log(entityData.water.GetCurrent().ToString());
        //WaterMeter.fillAmount = MaxWaterLevel;
        WaterMeter.fillAmount = entityData.water.GetCurrent();
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
            //if player is moving
            entityData.water.Set(entityData.water.GetCurrent() - (WaterMovementMinDrainAmount * Time.deltaTime));
            if (entityData.water.GetCurrent() < 0f) entityData.water.Set(entityData.water.GetCurrent() - entityData.water.GetCurrent());
            waterLevelPercentText.text = "H20: " + entityData.water.GetCurrent().ToString("F2") + "%";
            //WaterLevel = entityData.water.GetCurrent();
            WaterMeter.fillAmount = entityData.water.GetCurrent() / entityData.water.GetMax();
            //Debug.Log(WaterMeter.fillAmount);
            //WaterMeter.fillAmount = entityData.water.GetCurrent() / MaxWaterLevel;
            //Debug.Log(entityData.water.GetCurrent().ToString());

            // OLD water level change code
            //waterAmount -= WaterMovementMinDrainAmount * Time.deltaTime;
            //if (WaterLevel < 0) WaterLevel = 0;
            //waterLevelPercentText.text = "H20: " + WaterLevel.ToString("F2") + "%";
            //WaterMeter.fillAmount = WaterLevel / MaxWaterLevel;
        }
        /*
        else if (playerVelocity.magnitude > WaterMovementMaxSpeed) 
        {
            //if the player is moving fast via a sprint or momentum
            WaterLevel -= WaterMovementMaxDrainAmount * Time.deltaTime;
            if (WaterLevel < 0) WaterLevel = 0;
            waterLevelPercentText.text = "H20: " + WaterLevel.ToString("F2") + "%";
            WaterMeter.fillAmount = WaterLevel / MaxWaterLevel;
        }
        */
        else
        {
            //update just to be sure everything is working
            //waterLevelPercentText.text = "H20: " + WaterLevel.ToString("F2") + "%";
            if (entityData.water.GetCurrent() < 0f) entityData.water.Set(entityData.water.GetCurrent() - entityData.water.GetCurrent());
            waterLevelPercentText.text = "H20: " + entityData.water.GetCurrent().ToString("F2") + "%";
            //Debug.Log(entityData.water.GetCurrent().ToString());
        }



        /*if(Input.GetMouseButtonDown(0))
        {

        }*/
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            DamagePlayerHealth();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            //DamagePlayerHealth();
            //code to then tell the script player has taken damage, and to now make sure the player knows they're on death's door.
        }
    }

    private void DamagePlayerHealth()
    {
        Debug.Log("Take DMG");
        if (entityData.water.GetCurrent() > WaterNearEmptyValue)
        {
            Debug.Log("Damage Blocked.");
            HeartShieldIcon.gameObject.SetActive(true);
            StartCoroutine(HeartIconDisplayTimer(3f));
            //HeartShieldIcon.gameObject.SetActive(false);
        }
        else if (entityData.water.GetCurrent() == 0)
        {
            Debug.Log("Fatal DMG taken.");
            if (IsHeartShieldBroken == true)
            {
                Debug.Log("Fainted");
                HeartBrokenIcon.gameObject.SetActive(true);
                StartCoroutine(HeartIconDisplayTimer(3f));
                //HeartBrokenIcon.gameObject.SetActive(false);
            }
            else
            {
                Debug.Log("Critical Health");
                HeartFrailIcon.gameObject.SetActive(true);
                StartCoroutine(HeartIconDisplayTimer(12f));
                IsHeartShieldBroken = true;
                //HeartFrailIcon.gameObject.SetActive(false);
            }

        }

        /*
        else if (entityData.water.GetCurrent() <= WaterEmptyValue && (IsHeartShieldBroken)) 
        {
            HeartBrokenIcon.gameObject.SetActive(true);
            StartCoroutine(HeartIconDisplayTimer());
            //HeartBrokenIcon.gameObject.SetActive(false);
        }
        */
    }

    IEnumerator HeartIconDisplayTimer(float time)
    {
        yield return new WaitForSeconds(time);
        HeartShieldIcon.gameObject.SetActive(false);
        HeartFrailIcon.gameObject.SetActive(false);
        HeartBrokenIcon.gameObject.SetActive(false);
    }

}
