using UnityEngine;
using Godzilla.Input;

namespace Godzilla.Character
{
    /// <summary>
    /// Pure kinematics for the giant. Speeds are defined for a 1.8 m human and multiplied by
    /// scale × timeScale so the slowed-down animation and the displacement stay in step (no foot sliding).
    /// Gravity scales with scale × timeScale² so a jump's flight time matches the slowed animation.
    /// </summary>
    public class GiantMover
    {
        public struct State
        {
            public Vector3 Position;
            public float Yaw;              // degrees, 0 = +Z
            public float VerticalVelocity; // m/s
            public bool Grounded;
        }

        public float WalkSpeed = 1.4f;     // m/s @ 1.8 m
        public float RunSpeed = 4f;        // m/s @ 1.8 m
        public float TurnSpeedDeg = 60f;   // deg/s at full stick
        public float JumpFactor = 0.3f;    // apex height as a fraction of character height
        public float Deadzone = 0.15f;
        public const float Gravity = 9.81f;

        public State Step(State s, InputFrame i, float scale, float timeScale, float groundY, float heightMeters, float dt, bool jump = false)
        {
            float k = scale * timeScale;

            // turn
            if (Mathf.Abs(i.Turn.x) >= Deadzone) s.Yaw = Mathf.Repeat(s.Yaw + i.Turn.x * TurnSpeedDeg * dt, 360f);

            // planar move in facing space
            if (i.Move.magnitude >= Deadzone)
            {
                var move = Vector2.ClampMagnitude(i.Move, 1f);
                var rot = Quaternion.Euler(0f, s.Yaw, 0f);
                var dir = rot * new Vector3(move.x, 0f, move.y);
                float speed = (i.Run ? RunSpeed : WalkSpeed) * k;
                s.Position += dir * speed * dt;
            }

            // vertical
            float g = Gravity * scale * timeScale * timeScale;
            if (jump && s.Grounded)
            {
                s.VerticalVelocity = Mathf.Sqrt(2f * g * JumpFactor * heightMeters);
                s.Grounded = false;
            }
            if (!s.Grounded)
            {
                s.VerticalVelocity -= g * dt;
                s.Position.y += s.VerticalVelocity * dt;
                if (s.Position.y <= groundY)
                {
                    s.Position.y = groundY;
                    s.VerticalVelocity = 0f;
                    s.Grounded = true;
                }
            }
            else
            {
                s.Position.y = groundY;
            }
            return s;
        }
    }
}
