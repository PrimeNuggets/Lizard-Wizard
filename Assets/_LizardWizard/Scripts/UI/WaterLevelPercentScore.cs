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
using static Aspect;

public class WaterLevelPercentScore : MonoBehaviour
{
    //=========================================
    //Entity Data
    [NonSerialized] public Entity entityData;
    [NonSerialized] public Aspect aspect;
    //=============
    //Water Capacity
    //[NonSerialized] public float water;
    //[SerializeField] public Stat water;
    //[SerializeField] public Stat health;
    //[SerializeField] public Terrain terrain;
    //None of the above variables are referenced. therefore unneeded - Xavier
    //=========================================


    //public int waterAmount;
    public TextMeshProUGUI waterLevelPercentText;
    //public Image waterLevelLowTextDisplay;
    public Image healthDangerTextDisplay;
    //private PlayerInput playerInput;
    
    private CharacterController playerController;
    private Collider playerCollider;
    //Colliders don't need to be public since they grab the appropriate versions dynamically - Xavier
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
    private bool IsInvulnerable = false;
    private float WaterNearEmptyValue = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void StartScoring(Entity entityTemplate) //Replacing the start method & using player entity data to remove the need for multiple initializations - Xavier
    {
        entityData = entityTemplate;
        //for some reason the water text and fill is set to half when the terrain is involved
        SetWater(100); //Force sets current water to 100
        //Debug.Log(entityData.water.GetCurrent().ToString());
        //entityData.water.Initialize(water.GetMax());
        //entityData.water.GetCurrent();

        playerCollider = GetComponentInChildren<Collider>(); //Colliders exist in the mesh's skeleton
        HeartShieldIcon.gameObject.SetActive(false);
        HeartFrailIcon.gameObject.SetActive(false);
        HeartBrokenIcon.gameObject.SetActive(false);
        IsHeartShieldBroken = false;
        IsInvulnerable = false;
        healthDangerTextDisplay.gameObject.SetActive(false);
        //waterLevelLowTextDisplay.gameObject.SetActive(false);
        //entityData.water.Set(entityData.water.GetMax());
        Debug.Log(entityData.water.GetCurrent().ToString());
        WaterLevel = entityData.water.GetCurrent();
        MaxWaterLevel = entityData.water.GetMax();
        //waterAmount = 100;
        //waterLevelPercentText.text = "H20: " + waterAmount + "%";
        waterLevelPercentText.text = "H20: " + WaterLevel.ToString("F2") + "%";
        //Debug.Log(entityData.water.GetCurrent().ToString());
        //WaterMeter.fillAmount = MaxWaterLevel;
        WaterMeter.fillAmount = WaterLevel;
        //playerInput = GetComponent<PlayerInput>();
        playerController = entityData.entChar; //entChar already obtains the character controller - Xavier
        
    }

    // Update is called once per frame
    void FixedUpdate() //No need to run every tick. Running every frame works just as gud - Xavier
    {
        WaterMovementLoss();
        WaterLevel = entityData.water.GetCurrent();
    }

    private void WaterMovementLoss()
    {
        //gets player velocity to detrimine how fast the player is going
        if (playerController == null)
        {
            Debug.LogError("Player Controller is Undefined");
            return;
        }
        Vector3 playerVelocity = playerController.velocity;
        if (playerVelocity.magnitude > WaterMovementMinSpeed)
        {
            //if player is moving
            SetWater(WaterLevel - (WaterMovementMinDrainAmount * Time.deltaTime));
            if (WaterLevel < 0f) SetWater(0); //Why subtract the same negative value when it always == 0? - Xavier
            waterLevelPercentText.text = "H20: " + WaterLevel.ToString("F2") + "%";
            //WaterLevel = entityData.water.GetCurrent();
            WaterMeter.fillAmount = WaterLevel / MaxWaterLevel;
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
            if (WaterLevel < 0f) SetWater(0);
            waterLevelPercentText.text = "H20: " + WaterLevel.ToString("F2") + "%";
            //Debug.Log(entityData.water.GetCurrent().ToString());
        }



        /*if(Input.GetMouseButtonDown(0))
        {

        }*/
    }

    void SetWater(float value)
    {
        entityData.water.Set(value);
    }


    void OnTriggerEnter(Collider other)
    {
        /*
        if (other.CompareTag("Enemy")) //Look into checking aspect instead - Xavier
        {
            DamagePlayerHealth();
        } */
        var aspects = FindObjectsByType<Aspect>();
        foreach (var aspect in aspects)
        if (aspect.aspectName == Aspect.aspect.Enemy && !IsInvulnerable)
        {
            Debug.Log("Take DMG");
            DamagePlayerHealth();
        }
        else
        {
            Debug.Log("Invulnerable right now, try again later.");
            //StartCoroutine(HeartIconDisplayTimer(3f));
            //IsInvulnerable = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var aspects = FindObjectsByType<Aspect>();
        foreach (var aspect in aspects)
        if (aspect.aspectName == Aspect.aspect.Enemy)
        {
            Debug.Log("Damage has been taken.");
            
            IsInvulnerable = false;
            if (entityData.water.GetCurrent() == 0)
            {
                Debug.Log("Teetering on the brink, facing the abyss...");
                MortalDanger();
            }
            //code to then tell the script player has taken damage, and to now make sure the player knows they're on death's door.
            //StartCoroutine(HeartIconDisplayTimer(3f));
        }
    }

    private void DamagePlayerHealth()
    {
        
        if (IsInvulnerable)
        {
            Debug.Log("Invulnerable right now, mate.");

        }  
        else if (entityData.water.GetCurrent() > WaterNearEmptyValue)
        {
            Debug.Log("Damage Blocked.");
            HeartFrailIcon.gameObject.SetActive(false);
            HeartBrokenIcon.gameObject.SetActive(false); //make sure to hide the other ui when context changes to being healthy
            
            HeartShieldIcon.gameObject.SetActive(true);
            IsInvulnerable = true;
            StartCoroutine(HeartIconDisplayTimer(3f));
            //IsInvulnerable = false;
            //HeartShieldIcon.gameObject.SetActive(false);
        }
        else if ((entityData.water.GetCurrent() == 0) && IsHeartShieldBroken == true)
        {
            Debug.Log("Fatal DMG taken.");
            Debug.Log("Fainted");
            HeartFrailIcon.gameObject.SetActive(false);
            HeartShieldIcon.gameObject.SetActive(false); //make sure to hide the other ui when context changes to being on death's door, different than before where this was being called.
            
            HeartBrokenIcon.gameObject.SetActive(true);
            IsInvulnerable = true;
            StartCoroutine(HeartIconDisplayTimer(3f));
            //IsInvulnerable = false;
            //HeartBrokenIcon.gameObject.SetActive(false);
        }
        else if ((entityData.water.GetCurrent() == 0) && IsHeartShieldBroken == false)
        {
            Debug.Log("Fatal DMG taken.");
            Debug.Log("Critical Health");
            HeartShieldIcon.gameObject.SetActive(false); //make sure to hide the other ui when context changes to being on death's door
            
            HeartFrailIcon.gameObject.SetActive(true);
            IsInvulnerable = true;
            StartCoroutine(HeartIconDisplayTimer(12f));
            IsHeartShieldBroken = true;
            //IsInvulnerable = false;
            //HeartFrailIcon.gameObject.SetActive(false);
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

    private void DoNotDamagePlayerHealth()
    {
        Debug.Log("Invulnerable right now, try again later.");
        return;

    }

    private void MortalDanger()
    {
        if ((entityData.water.GetCurrent() == 0) && IsHeartShieldBroken == true) 
        {
            healthDangerTextDisplay.gameObject.SetActive(true);
        }
        if (!IsHeartShieldBroken)
        {
            healthDangerTextDisplay.gameObject.SetActive(false);
        }

        //while (entityData.water.GetCurrent() == 0)
        //{
        //waterLevelLowText.gameObject.SetActive(true);
        //}
    }
    IEnumerator HeartIconDisplayTimer(float TimerDuration)
    {
        //float TimerElapsedTime = 0f;
        //yield return new WaitForSeconds(TimerDuration);
        /*
        Debug.Log("Max Invul Timer Counting Down.");
        while (TimerElapsedTime < TimerDuration)
        {
            TimerElapsedTime += Time.deltaTime;
            if (TimerElapsedTime == TimerDuration) 
            {
                Debug.Log("Max Invul Timer Finished. Can be damaged now.");
                IsInvulnerable = false;
            }
        }
        */
        IsInvulnerable = true;

        yield return new WaitForSeconds(TimerDuration);

        if (entityData.water.GetCurrent() > WaterNearEmptyValue)
        {
            HeartShieldIcon.gameObject.SetActive(false);
            IsInvulnerable = false;
        }
        if ((entityData.water.GetCurrent() == 0) && IsHeartShieldBroken == false)
        {
            HeartFrailIcon.gameObject.SetActive(false);
            IsInvulnerable = false;
        }
        if ((entityData.water.GetCurrent() == 0) && IsHeartShieldBroken == true)
        {
            HeartBrokenIcon.gameObject.SetActive(false);
            IsInvulnerable = false;
        }
    }

}
