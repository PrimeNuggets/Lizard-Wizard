using UnityEngine;

namespace Movement
{
    public class Physics
    {
        const float GRAVITY = 40.0f; //May want to lower so diving works as intended
        const float FRICTION = 1.0f;

        public float applyGravity(float yVel, float deltaTime)
        {
            return yVel - GRAVITY * deltaTime;
        }
    }
}
