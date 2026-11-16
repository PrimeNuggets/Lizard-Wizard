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
    public Collider OasisCollider;
    //Colliders don't need to be public since they grab the appropriate versions dynamically - Xavier
    public Image WaterMeter;

    public float MaxWaterLevel;
    public float WaterMovementMinDrainAmount;
    //public float WaterMovementMaxDrainAmount;
    public float WaterMovementMinSpeed = 0.1f;
    //public float WaterMovementMaxSpeed = 10.0f;
    public float WaterMagicDrainAmount;
    public float WaterTakeDamageDrainAmount;
    public float WaterRestRegenAmount;
    //public float WaterEmptyValue = 0;
    public Image HeartShieldIcon;
    public Image HeartFrailIcon;
    public Image HeartBrokenIcon;
    private bool IsHeartShieldBroken = false;
    private bool IsInvulnerable = false;
    private float WaterNearEmptyValue = 1f;
    private bool IsRegenWater = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void StartScoring(Entity entityTemplate) //Replacing the start method & using player entity data to remove the need for multiple initializations - Xavier
    {
        entityData = entityTemplate;
        SetWater(100); //Force sets current water to 100
        playerCollider = GetComponentInChildren<Collider>(); //Colliders exist in the mesh's skeleton
        playerController = entityData.entChar; //entChar already obtains the character controller - Xavier
        OasisCollider = GetComponent<Collider>(); //May want to handle this in the oasis rather than the player if oasis will be spawned
        HeartShieldIcon.gameObject.SetActive(false);
        HeartFrailIcon.gameObject.SetActive(false);
        HeartBrokenIcon.gameObject.SetActive(false);
        IsHeartShieldBroken = false;
        IsInvulnerable = false;
        healthDangerTextDisplay.gameObject.SetActive(false);
        //waterLevelLowTextDisplay.gameObject.SetActive(false);
        //entityData.water.Set(entityData.water.GetMax());
        Debug.Log(Water().ToString());
        MaxWaterLevel = Water(true);
        //waterAmount = 100;
        //waterLevelPercentText.text = "H20: " + waterAmount + "%";
        waterLevelPercentText.text = "H20: " + Water().ToString("F2") + "%";
        //Debug.Log(entityData.water.GetCurrent().ToString());
        WaterMeter.fillAmount = MaxWaterLevel;
        //playerInput = GetComponent<PlayerInput>();
    }

    public float Water(bool max = false)
    {
        return !max ? entityData.water.GetCurrent() : entityData.water.GetMax();
    }

    // Update is called once per frame
    void FixedUpdate() //No need to run every tick. Running every frame works just as gud - Xavier
    {
        if (IsRegenWater == false) 
        {
            WaterMovementLoss();
        }
        string text = "H20: " + WaterPercentage().ToString("F2") + "%";
        if (Water() >= MaxWaterLevel) text = "H20: 100.00%";
        if (Water() <= 0f) text = "H20: 0.00%";
        UpdateText(text);
        MortalDanger();
    }

    private float WaterPercentage()
    {
        return Mathf.Clamp01(Water() / MaxWaterLevel) * 100f;
    }

    private void WaterMovementLoss()
    {
        if (entityData.GetMoveType() == 'H') return; //Don't lose water on knockback
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
            SetWater(Water() - (WaterMovementMinDrainAmount * Time.deltaTime));
            if (Water() < 0f)
            {
                SetWater(0); //Why subtract the same negative value when it always == 0? - Xavier 
            }
            // this was to ensure the display would always be 0 and not go further.
            waterLevelPercentText.text = "H20: " + WaterPercentage().ToString("F2") + "%";
            WaterMeter.fillAmount = WaterPercentage() / 100f;
        }
        else
        {
            //update just to be sure everything is working
            //waterLevelPercentText.text = "H20: " + WaterLevel.ToString("F2") + "%";
            if (Water() < 0f)
            {
                SetWater(0);
            }
            //Debug.Log(entityData.water.GetCurrent().ToString());
        }
    }

    void UpdateText(string text)
    {
        waterLevelPercentText.text = text;
        WaterMeter.fillAmount = WaterPercentage() / 100f;
    }

    void SetWater(float value)
    {
        entityData.water.Set(value);
    }

    private void WaterGain()
    {
        if (entityData.GetMoveType() == 'H') return; //Don't gain water when hurt
        //FORNOW this will be regen when not moving
        if (playerController == null)
        {
            Debug.LogError("Player Controller is Undefined");
            return;
        }
        Vector3 playerVelocity = playerController.velocity;
        if (IsRegenWater == true)
        {
            //if player is moving
            SetWater(Water() + (WaterRestRegenAmount * Time.deltaTime));
            WaterMeter.fillAmount = Water() / MaxWaterLevel;
        }
        else
        {
            //update just to be sure everything is working
            //waterLevelPercentText.text = "H20: " + Water().ToString("F2") + "%";
            if (Water() < 0f)
            {
                SetWater(0);
            }
            waterLevelPercentText.text = "H20: " + WaterPercentage().ToString("F2") + "%";
            //Debug.Log(entityData.water.GetCurrent().ToString());
        }


    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Oasis")) 
        {
            Debug.Log("Getting Water");
            IsRegenWater = true;
            WaterGain();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Oasis"))
        {
            IsRegenWater = false;
        }
    }

    public void DisplayShield()
    {
        HeartFrailIcon.gameObject.SetActive(false);
        HeartBrokenIcon.gameObject.SetActive(false); //make sure to hide the other ui when context changes to being healthy

        HeartShieldIcon.gameObject.SetActive(true);
        //StartCoroutine(HeartIconDisplayTimer(3f));
        //IsInvulnerable = false;
    }
    public void DisplayEmptyHealth()
    {
        HeartFrailIcon.gameObject.SetActive(false);
        HeartShieldIcon.gameObject.SetActive(false); //make sure to hide the other ui when context changes to being on death's door, different than before where this was being called.

        HeartBrokenIcon.gameObject.SetActive(true);
        //IsInvulnerable = true;
        //StartCoroutine(HeartIconDisplayTimer(3f));
    }
    public void DisplayHealth()
    {
        if (Water() > 0 || IsHeartShieldBroken) return;
        HeartShieldIcon.gameObject.SetActive(false); //make sure to hide the other ui when context changes to being on death's door

        HeartFrailIcon.gameObject.SetActive(true);
        //IsInvulnerable = true;
        //StartCoroutine(HeartIconDisplayTimer(12f));
        IsHeartShieldBroken = entityData.health.GetCurrent() <= 0f;
    }

    public void DisplayGraphic(float time)
    {
        if (Water() > WaterNearEmptyValue) DisplayShield();
        if (Water() <= 0 && !IsHeartShieldBroken) DisplayHealth();
        if (Water() <= 0 && IsHeartShieldBroken) DisplayEmptyHealth();
        StartCoroutine(HeartIconDisplayTimer(time / 60f));
    }

    public void MortalDanger()
    {
        if (Water() <= 0 && IsHeartShieldBroken == true)
        {
            healthDangerTextDisplay.gameObject.SetActive(true);
        }
        if (!IsHeartShieldBroken)
        {
            healthDangerTextDisplay.gameObject.SetActive(false);
        }
    }
    IEnumerator HeartIconDisplayTimer(float TimerDuration)
    {
        //IsInvulnerable = true;

        yield return new WaitForSeconds(TimerDuration);

        HideHealthGraphics();
    }

    public void HideHealthGraphics()
    {
        if (Water() > WaterNearEmptyValue)
        {
            HeartShieldIcon.gameObject.SetActive(false);
        }
        if ((Water() == 0) && IsHeartShieldBroken == false)
        {
            HeartFrailIcon.gameObject.SetActive(false);
        }
        if ((Water() == 0) && IsHeartShieldBroken == true)
        {
            HeartBrokenIcon.gameObject.SetActive(false);
        }
    }

}
