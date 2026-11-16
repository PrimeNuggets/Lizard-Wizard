using UnityEngine;

//May be better as a scriptable object rather than a new script for each spell.

public class SteamDash : MonoBehaviour, ISpell
{
    private GameObject Player;
    private ISpell.Elements element = ISpell.Elements.Steam;
    private float chargetime = 0.0f;
    private float maxcharge = 1.0f;
    
//might change type. currently let ice=0, water=1, steam=2

    public float dashspeed = 200.0f;

    void Start(){
        Player = gameObject;
    }
    public ISpell.Elements GetElement(){
        return element;
    }
    public void SetElement(ISpell.Elements setTo){
        element = setTo; 
        //Use this to change visual effects and spawned objects if the element changes.
    }

    public void CastSpellDown(){
        Debug.Log("Charging");
    }
    public void CastSpellTick(float dt) {}
    public void CastSpellReleased(){
        Debug.Log("Fwoosh!");
        Player.GetComponent<Player>().entityData.velocity = Player.GetComponent<Player>().GetLookDir() * dashspeed;
    }

    public void UpgradeSpell(){

    }

    public bool IsOnCooldown()
    {
        return false;
    }
}
