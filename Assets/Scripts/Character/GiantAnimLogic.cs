using UnityEngine;
using Godzilla.Input;

namespace Godzilla.Character
{
    public enum GiantAction { None, Jump, Punch, Kick, Roar }

    /// <summary>
    /// Pure animation-intent logic: normalised locomotion speed, one-shot action requests, and the action lock
    /// (punch/kick/roar block movement and further actions until <see cref="OnActionFinished"/>).
    /// </summary>
    public class GiantAnimLogic
    {
        public const float Deadzone = 0.15f;
        public const float SpeedLerpPerSecond = 8f;

        public float Speed01 { get; private set; }   // 0 idle, 0.5 walk, 1 run
        public bool Grounded { get; private set; } = true;
        public bool Locked { get; private set; }
        public GiantAction Pending { get; private set; } = GiantAction.None;

        public void Tick(InputFrame i, bool grounded, float dt)
        {
            Grounded = grounded;
            var move = MoveAllowed(i);
            float target = move.sqrMagnitude < 1e-6f ? 0f : (i.Run ? 1f : 0.5f);
            Speed01 = Mathf.MoveTowards(Speed01, target, SpeedLerpPerSecond * dt * Mathf.Max(0.25f, Mathf.Abs(target - Speed01)));

            if (Locked) return;
            if (i.PunchPressed) Request(GiantAction.Punch, lockMove: true);
            else if (i.KickPressed) Request(GiantAction.Kick, lockMove: true);
            else if (i.RoarPressed) Request(GiantAction.Roar, lockMove: true);
            else if (i.JumpPressed && grounded) Request(GiantAction.Jump, lockMove: false);
        }

        /// <summary>Movement vector the mover may use this frame (zero while locked or inside the deadzone).</summary>
        public Vector2 MoveAllowed(InputFrame i)
        {
            if (Locked) return Vector2.zero;
            return i.Move.magnitude < Deadzone ? Vector2.zero : i.Move;
        }

        /// <summary>Returns the queued action once; later calls return None until a new action is requested.</summary>
        public GiantAction ConsumePending()
        {
            var a = Pending;
            Pending = GiantAction.None;
            return a;
        }

        public void OnActionFinished() => Locked = false;

        void Request(GiantAction a, bool lockMove)
        {
            Pending = a;
            if (lockMove) Locked = true;
        }
    }
}
