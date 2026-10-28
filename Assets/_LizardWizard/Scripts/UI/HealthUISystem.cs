using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using static Entity;
using Unity.VisualScripting;
using System.Threading.Tasks;
using System.Collections;

//DEPRECATED
public class HealthUISystem : MonoBehaviour
{

    [Header("Entity Data")]
    [LabelOverride("Data")] public Entity entityTemplate;
    [NonSerialized] public Entity entityData;
    [NonSerialized] public Terrain terrain;
    [SerializeField] public Stat health;
    [SerializeField] public Stat water;

    public CharacterController playerController;
    public Collider playerCollider;
    //public Image CurrentHealthIcon;
    public Image HeartShieldIcon;
    public Image HeartFrailIcon;
    public Image HeartBrokenIcon;
    private bool IsHeartShieldBroken = false;
    private bool IsInvulnerable = false;
    private float WaterNearEmptyValue = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        entityData = Instantiate(entityTemplate);
        entityData.Initialize(gameObject);
        entityData.water.GetCurrent();
        playerCollider = GetComponent<Collider>();
        HeartShieldIcon.gameObject.SetActive(false);
        HeartFrailIcon.gameObject.SetActive(false);
        HeartBrokenIcon.gameObject.SetActive(false);
        IsHeartShieldBroken = false;
        IsInvulnerable = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            DamagePlayerHealth();
        }
    }

    // script RETIRED lowk forgot.
    private void DamagePlayerHealth()
    {
        Debug.Log("Take DMG");
        if (IsInvulnerable)
        {
            Debug.Log("Invulnerable right now, try again later.");

        }
        if (entityData.water.GetCurrent() > WaterNearEmptyValue)
        {
            Debug.Log("Damage Blocked.");
            HeartShieldIcon.gameObject.SetActive(true);
            IsInvulnerable = true;
            StartCoroutine(HeartIconDisplayTimer());
            IsInvulnerable = false;
            //HeartShieldIcon.gameObject.SetActive(false);
        }     
        if (entityData.water.GetCurrent() == 0)
        {
            Debug.Log("Fatal DMG taken.");
            if ((entityData.water.GetCurrent() == 0) && !IsHeartShieldBroken)
            {
                Debug.Log("Critical Health");
                HeartFrailIcon.gameObject.SetActive(true);
                IsInvulnerable = true;
                StartCoroutine(HeartIconDisplayTimer());
                IsInvulnerable = false;
                IsHeartShieldBroken = true;
                //HeartFrailIcon.gameObject.SetActive(false);
            }
            else if (IsHeartShieldBroken)
            {
                Debug.Log("Fainted");
                HeartBrokenIcon.gameObject.SetActive(true);
                StartCoroutine(HeartIconDisplayTimer());
                IsInvulnerable = false;
                //HeartBrokenIcon.gameObject.SetActive(false);
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

    IEnumerator HeartIconDisplayTimer()
    {
        yield return new WaitForSeconds(3);
        HeartShieldIcon.gameObject.SetActive(false);
        HeartFrailIcon.gameObject.SetActive(false);
        HeartBrokenIcon.gameObject.SetActive(false);
    }
}
