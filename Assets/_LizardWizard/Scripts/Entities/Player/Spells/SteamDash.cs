using UnityEngine;

public class SteamDash : MonoBehaviour, ISpell
{
    private GameObject Player;
    private int element = 2;
    private float chargetime = 0.0f;
    private float maxcharge = 1.0f;
    
//might change type. currently let ice=0, water=1, steam=2

    public float dashspeed = 200.0f;

    void Start(){
        Player = gameObject;
    }
    public int GetElement(){
        return element;
    }
    public void SetElement(int setTo){
        element = setTo; 
        //Use this to change visual effects and spawned objects if the element changes.
    }

    public void CastSpellDown(){
        Debug.Log("Charging");
    }
    public void CastSpellReleased(){
        Debug.Log("Fwoosh!");
        Player.GetComponent<Player>().entityData.velocity = Player.GetComponent<Player>().GetLookDir() * dashspeed;
    }

    public void UpgradeSpell(){

    }
}
