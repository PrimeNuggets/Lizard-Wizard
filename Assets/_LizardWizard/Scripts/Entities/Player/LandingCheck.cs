using System;
using System.Collections.Generic;
using UnityEngine;
using static DebugHandler;

public class LandingCheck : MonoBehaviour
{
    [Serializable]
    public class Threshold
    {
        public string name;
        [Range(-180f, 180f)] public float min; //Percentage
        [Range(-180f, 180f)] public float max;
    }
    [NonSerialized] public bool showDebug = false;
    [NonSerialized] public float checkDist = 5f;
    [NonSerialized] public LayerMask groundLayers;
    [NonSerialized] public Threshold[] checkThresholds;
    [NonSerialized] public Transform player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Raycast to check slope angle
    }

    public int Check()
    {
        Transform modelAngle = transform;
        if (checkThresholds == null || player == null || modelAngle == null) return -1;
        bool terrainFound = Physics.Raycast(
            transform.position,
            Vector3.down,
            out RaycastHit hit,
            checkDist,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );
        Debug.DrawRay(
            transform.position,
            Vector3.down * checkDist,
            terrainFound ? Color.green : Color.red
        );
        if (!terrainFound) return -1;

        Vector3 approach = Vector3.ProjectOnPlane(player.forward, Vector3.up);
        if (!VerifyVector(approach)) return -1;
        approach.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, approach);

        Vector3 surfaceForward = Vector3.Cross(right, hit.normal);
        if (!VerifyVector(surfaceForward)) return -1;
        surfaceForward.Normalize();

        Vector3 forward = Vector3.ProjectOnPlane(modelAngle.forward, right);
        if (!VerifyVector(forward)) return -1;
        forward.Normalize();

        float slopeAngle = Vector3.SignedAngle(approach, surfaceForward, -right);
        float angle = Vector3.SignedAngle(approach, forward, -right);
        float diff = Mathf.DeltaAngle(slopeAngle, angle);
        //float slopeMg = Math.Abs(slopeAngle);
        
        if (showDebug && DevMode()) Debug.Log($"{name}: slope={slopeAngle:F2}°, " + $"feet={angle:F2}°, difference={diff:F2}°");

        for (int i = 0; i < checkThresholds.Length; i++)
        {
            Threshold thresh = checkThresholds[i];
            if (thresh == null) continue;
            if (thresh.min > thresh.max)
            {
                Debug.LogWarning($"Landing threshold '{thresh.name}' has min ({thresh.min}) > max ({thresh.max}).");
                continue;
            }
            if (diff >= thresh.min && diff <= thresh.max) return i;
        }
        return -1;
    }
}
