using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Entity", menuName = "Scriptable Objects/Entity")]
public class Entity : ScriptableObject
{
    [Serializable]
    public class Stat
    {
        float current;
        [SerializeField] float max;
        public float GetCurrent()
        {
            return current;
        }
        public float GetMax()
        {
            return max;
        }
        public void Set(float value, bool max = false)
        {
            if (!max)
            {
                current = value;
            } else
            {
                this.max = value;
            }
        }
    }
    //===================================================
        //Serializable Variables
    //===================================================
    public Stat health;
    [Tooltip("Effectively Mana")] public Stat water;
    public Stat speed;
    public Stat jumpHeight;
    [SerializeField, Tooltip("Distance from the player's pivot to their feet")] private float feetOffset = 0.25f;

    //===================================================
        //Non-Serializable Variables
    //===================================================
    [NonSerialized] public bool grounded = false;
    [NonSerialized] public bool ctrl = false; //Whether the entity is in control of their own actions
    private readonly Movement.Physics physics = new Movement.Physics();
    [NonSerialized] public Vector3 velocity;
    [NonSerialized] public Terrain terrain;
    [NonSerialized] public GameObject entObj;
    
    public void Initialize(GameObject obj, Terrain ter)
    {
        InitializeStats();
        ctrl = true;
        entObj = obj;
        terrain = ter;
    }
    public void InitializeStats()
    {
        health.Set(health.GetMax());
        water.Set(water.GetMax() / 2);
        speed.Set(speed.GetMax());
        jumpHeight.Set(jumpHeight.GetMax());
    }
    public Vector3 ApplyMove(Vector2 move, bool jumped)
    {
        float dt = Time.deltaTime;
        move = Vector2.ClampMagnitude(move, 1f); //Processes WASD
        if (!ctrl)
        {
            move = Vector2.zero;
            jumped = false;
        }

        Vector3 direction = entObj.transform.right * move.x + entObj.transform.forward * move.y;


        if (grounded && jumped)
        {
            velocity.y = jumpHeight.GetCurrent();
            grounded = false;
            ctrl = false;
        }
        velocity = direction * speed.GetCurrent();
        velocity.y = physics.applyGravity(velocity.y, dt); //Handles gravity

        Vector3 nextPos = entObj.transform.position;
        nextPos += velocity * dt;

        float groundY = terrain.SampleHeight(nextPos) + terrain.transform.position.y + feetOffset;
        if (nextPos.y <= groundY && velocity.y <= 0f)
        {
            nextPos.y = groundY;
            velocity.y = 0f;
            grounded = true;
            ctrl = true;
        }
        return nextPos;
    }
}