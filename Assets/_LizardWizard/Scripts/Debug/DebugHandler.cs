using UnityEngine;

public class DebugHandler : MonoBehaviour
{
    public static bool DevMode()
    {
        bool ret = false;
        #if UNITY_EDITOR
            ret = true;
        #endif
        return ret;
    }
}
