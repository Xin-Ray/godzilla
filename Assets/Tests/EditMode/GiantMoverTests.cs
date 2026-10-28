using NUnit.Framework;
using UnityEngine;
using Godzilla.Character;
using Godzilla.Input;

namespace Godzilla.Tests
{
    public class GiantMoverTests
    {
        const float Scale = 100f / 1.8f;   // 100 m giant from a 1.8 m model
        const float TimeScale = 0.35f;
        const float GroundY = -120f;
        const float Height = 100f;

        static GiantMover.State Start() => new GiantMover.State { Position = new Vector3(0, GroundY, 0), Yaw = 0f, VerticalVelocity = 0f, Grounded = true };

        [Test]
        public void Deadzone_NoMotion()
        {
            var m = new GiantMover();
            var s = Start();
            var i = new InputFrame { Move = new Vector2(0.1f, -0.1f), Turn = new Vector2(0.1f, 0) };
            for (int k = 0; k < 60; k++) s = m.Step(s, i, Scale, TimeScale, GroundY, Height, 1f / 60f);
            Assert.AreEqual(0f, Vector3.Distance(s.Position, Start().Position), 1e-4f);
            Assert.AreEqual(0f, s.Yaw, 1e-4f);
        }

        [Test]
        public void Walk_SpeedScales()
        {
            var m = new GiantMover();
            var s = Start();
            var i = new InputFrame { Move = new Vector2(0, 1f) };
            s = m.Step(s, i, Scale, TimeScale, GroundY, Height, 1f);
            float expected = 1.4f * Scale * TimeScale; // walk 1.4 m/s at 1.8 m, scaled and slowed
            Assert.AreEqual(expected, s.Position.z, 0.01f, "moves along +Z (yaw 0 = facing +Z)");
            Assert.AreEqual(GroundY, s.Position.y, 1e-4f);
        }

        [Test]
        public void Run_IsFaster_AndStrafeUsesRight()
        {
            var m = new GiantMover();
            var s = Start();
            s = m.Step(s, new InputFrame { Move = new Vector2(1f, 0), Run = true }, Scale, TimeScale, GroundY, Height, 1f);
            Assert.AreEqual(4f * Scale * TimeScale, s.Position.x, 0.01f, "strafe right at run speed");
            Assert.AreEqual(0f, s.Position.z, 0.01f);
        }

        [Test]
        public void Turn_60DegPerSec_AndMovesAlongFacing()
        {
            var m = new GiantMover();
            var s = Start();
            s = m.Step(s, new InputFrame { Turn = new Vector2(1f, 0) }, Scale, TimeScale, GroundY, Height, 1.5f);
            Assert.AreEqual(90f, s.Yaw, 0.01f);
            s = m.Step(s, new InputFrame { Move = new Vector2(0, 1f) }, Scale, TimeScale, GroundY, Height, 1f);
            Assert.AreEqual(1.4f * Scale * TimeScale, s.Position.x, 0.01f, "facing +X after a 90° turn");
            Assert.AreEqual(0f, s.Position.z, 0.01f);
        }

        [Test]
        public void Jump_ReachesApex_AndReturnsToGround()
        {
            var m = new GiantMover();
            var s = Start();
            s = m.Step(s, new InputFrame(), Scale, TimeScale, GroundY, Height, 1f / 60f, jump: true);
            Assert.IsFalse(s.Grounded);
            float apex = s.Position.y; float t = 0f;
            while (!s.Grounded && t < 60f)
            {
                s = m.Step(s, new InputFrame(), Scale, TimeScale, GroundY, Height, 1f / 60f);
                apex = Mathf.Max(apex, s.Position.y); t += 1f / 60f;
            }
            Assert.IsTrue(s.Grounded, "lands");
            Assert.AreEqual(GroundY, s.Position.y, 1e-3f, "clamped to ground");
            Assert.AreEqual(GroundY + 0.3f * Height, apex, 0.05f * 0.3f * Height, "apex ≈ 0.3 × height");
            // ballistic duration with g = 9.81·scale·timeScale²: t = 2·sqrt(2h/g)
            float g = 9.81f * Scale * TimeScale * TimeScale;
            float expectedT = 2f * Mathf.Sqrt(2f * 0.3f * Height / g);
            Assert.AreEqual(expectedT, t, 0.1f, "flight time matches scaled gravity");
        }

        [Test]
        public void Jump_IgnoredWhileAirborne()
        {
            var m = new GiantMover();
            var s = Start();
            s = m.Step(s, new InputFrame(), Scale, TimeScale, GroundY, Height, 1f / 60f, jump: true);
            float v1 = s.VerticalVelocity;
            s = m.Step(s, new InputFrame(), Scale, TimeScale, GroundY, Height, 1f / 60f, jump: true);
            Assert.Less(s.VerticalVelocity, v1, "second jump press does not re-launch");
        }
    }
}
