using UnityEngine;

namespace Movement
{
    public class Physics
    {
        const float GRAVITY = -100.0f;
        const float FRICTION = 1.0f;

        public float applyGravity(float yVel, float deltaTime)
        {
            return yVel + GRAVITY * deltaTime;
        }
    }
}
