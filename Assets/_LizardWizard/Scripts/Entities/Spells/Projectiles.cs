using System;
using UnityEngine;


public class Projectiles : MonoBehaviour
{
    [NonSerialized] public GameObject root;
    [NonSerialized] public AttackData data;
    [NonSerialized] public Vector3 velocity;
    private float traveled;
    [NonSerialized] public float decel = -1f;
    [NonSerialized] public float lifeTime = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var projCollider = GetComponentInChildren<ProjectileCollider>();
        Entity rootData = root.GetComponent<Player>().entityData;
        for (var i = 0; i < 2; i++)
        {
            if (rootData == null)
            {
                if (i == 0)
                {
                    rootData = root.GetComponent<EnemyAI>().entityData;
                } else rootData = root.GetComponent<NPC>().entityData;
            }
        }
        if (projCollider != null)
        {
            projCollider.entity = root.GetComponent<Aspect>();
            projCollider.entityData = rootData;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (decel >= 0f && traveled >= decel)
        {
            velocity = Vector3.MoveTowards(
                velocity,
                Vector3.zero,
                decel * Time.fixedDeltaTime
            );
        }
        Vector3 displacement = velocity * Time.fixedDeltaTime;
        transform.position += displacement;
        traveled += displacement.magnitude;

        if (lifeTime <= 0) Destroy(gameObject);
        lifeTime -= Time.fixedDeltaTime;
    }
    void OnDestroy()
    {
        Destroy(data);
    }
    void OnCollisionEnter(Collision collision)
    {
        bool entityHit = collision.gameObject.GetComponent<EnemyAI>() != null;
        entityHit |= collision.gameObject.GetComponent<Player>() != null;
        entityHit |= collision.gameObject.GetComponent<NPC>() != null;
        if ((data.type == AttackData.AttackType.OneHit && entityHit) || !entityHit) Destroy(gameObject);
    }
}
