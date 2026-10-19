using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using static Entity;
using Unity.VisualScripting;
using System.Threading.Tasks;

public class HealthUISystem : MonoBehaviour
{

    [Header("Entity Data")]
    [LabelOverride("Data")] public Entity entityTemplate;
    [NonSerialized] public Entity entityData;
    [SerializeField] public Stat health;
    [SerializeField] public Stat water;

    public CharacterController playerController;
    public Collider playerCollider;
    //public Image CurrentHealthIcon;
    public Image HeartShieldIcon;
    public Image HeartFrailIcon;
    public Image HeartBrokenIcon;
    bool HeartShieldBroken = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        entityData = Instantiate(entityTemplate);
        entityData.water.GetCurrent();
        HeartShieldIcon.gameObject.SetActive(false);
        HeartFrailIcon.gameObject.SetActive(false);
        HeartBrokenIcon.gameObject.SetActive(false);
        HeartShieldBroken = false;
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

    private async void DamagePlayerHealth()
    {
        
        if (entityData.water.GetCurrent() > 0)
        {
            HeartShieldIcon.gameObject.SetActive(true);
            await Task.Delay(2000);
            HeartShieldIcon.gameObject.SetActive(false);

        }
        else if (entityData.water.GetCurrent() <= 0)
        {
            HeartFrailIcon.gameObject.SetActive(true);
            HeartShieldBroken = true;
            await Task.Delay(2000);
            HeartFrailIcon.gameObject.SetActive(false);

        }
        else if ((entityData.water.GetCurrent() <= 0) || (HeartShieldBroken = true)) 
        {
            HeartBrokenIcon.gameObject.SetActive(true);
            await Task.Delay(3000);
            HeartBrokenIcon.gameObject.SetActive(false);
        }

    }
}
