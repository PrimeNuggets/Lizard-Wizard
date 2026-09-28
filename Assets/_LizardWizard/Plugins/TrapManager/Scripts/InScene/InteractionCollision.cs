using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class InteractionCollision : MonoBehaviour
{
    public CollisionEffects collisionFXData;
    [NonSerialized] public float nextDamageTime;
    private float despawnTimer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        despawnTimer = collisionFXData != null ? collisionFXData.collisionLife : 1f;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (collisionFXData == null || collisionFXData.collisionLife <= 0f)
        {
            return;
        }

        despawnTimer -= Time.deltaTime;
        if (despawnTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Debug.Log("Despawning " + gameObject.name + " in: " + despawnTimer.ToString("F2"));
    }
}
