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

    public static bool VerifyVector(Vector3 vector)
    {
        return vector.sqrMagnitude >= 0.0001f;
    }
}
