using UnityEngine;

public class SteamDash : MonoBehaviour, ISpell
{
    private int element = 2;
//might change type. currently let ice=0, water=1, steam=2

    private float dashspeed = 15.0f;

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
    }

    public void UpgradeSpell(){

    }
}
