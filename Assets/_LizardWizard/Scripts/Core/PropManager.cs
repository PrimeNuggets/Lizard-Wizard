using UnityEngine;

public class PropManager : MonoBehaviour
{
        //Prop overlap prevention system stuff
    private Collider[] propColliders;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    { 
        /*propColliders = new Collider[10];
            int numObjectsNearby = Physics.OverlapSphereNonAlloc(transform.position, 0.5f, propColliders);
            Debug.Log(numObjectsNearby);
       if (numObjectsNearby > 2){
        Debug.Log("Deleting prop" + transform.position);
            Destroy(gameObject);
       }
*/
    }

    // Update is called once per frame
    void Update()
    {

    }

}
