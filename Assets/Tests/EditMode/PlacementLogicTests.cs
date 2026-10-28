using NUnit.Framework;
using UnityEngine;
using Godzilla.Core;
using Godzilla.Input;
using Godzilla.Placement;

namespace Godzilla.Tests
{
    public class PlacementLogicTests
    {
        static readonly CityFrame Frame = new CityFrame(Vector3.zero, Vector3.forward);

        [Test]
        public void Height_Clamped_30_200()
        {
            var d = PlacementData.Default();
            d.heightMeters = 199f;
            var up = new InputFrame { Move = new Vector2(0, 1f) };
            d = PlacementLogic.Step(d, up, Frame, dt: 10f); // +200 m requested
            Assert.AreEqual(200f, d.heightMeters, 1e-3f);

            d.heightMeters = 31f;
            var down = new InputFrame { Move = new Vector2(0, -1f) };
            d = PlacementLogic.Step(d, down, Frame, dt: 10f);
            Assert.AreEqual(30f, d.heightMeters, 1e-3f);
        }

        [Test]
        public void StickX_NudgesLateral_ScaledByDistance()
        {
            var d = PlacementData.Default(); // distance 320
            var i = new InputFrame { Move = new Vector2(1f, 0) };
            d = PlacementLogic.Step(d, i, Frame, dt: 1f);
            Assert.AreEqual(0.05f * 320f, d.lateral, 1e-3f);
        }

        [Test]
        public void TurnY_ChangesDistance_AndClamps()
        {
            var d = PlacementData.Default();
            d.distance = 100f;
            d = PlacementLogic.Step(d, new InputFrame { Turn = new Vector2(0, -1f) }, Frame, dt: 1f);
            Assert.AreEqual(75f, d.distance, 1e-3f);
            d.distance = 60f;
            d = PlacementLogic.Step(d, new InputFrame { Turn = new Vector2(0, -1f) }, Frame, dt: 10f);
            Assert.AreEqual(50f, d.distance, 1e-3f, "min distance 50");
            d.distance = 1990f;
            d = PlacementLogic.Step(d, new InputFrame { Turn = new Vector2(0, 1f) }, Frame, dt: 10f);
            Assert.AreEqual(2000f, d.distance, 1e-3f, "max distance 2000");
        }

        [Test]
        public void Yaw_Wraps360()
        {
            var d = PlacementData.Default();
            d.yaw = 350f;
            d = PlacementLogic.Step(d, new InputFrame { Turn = new Vector2(1f, 0) }, Frame, dt: 1f); // +45
            Assert.AreEqual(35f, d.yaw, 1e-3f);
            d.yaw = 10f;
            d = PlacementLogic.Step(d, new InputFrame { Turn = new Vector2(-1f, 0) }, Frame, dt: 1f); // -45
            Assert.AreEqual(325f, d.yaw, 1e-3f);
        }

        [Test]
        public void Drag_SetsLateralDistanceFromHit()
        {
            var d = PlacementData.Default(); // groundDrop 120 => groundY -120
            // ray from 1.6 m high, pointing out and down so it hits y=-120 at distance 400, lateral 20
            var target = new Vector3(20f, -120f, 400f);
            var origin = new Vector3(0, 1.6f, 0);
            var i = new InputFrame { PlaceDragHeld = true, RightAimRay = new Ray(origin, (target - origin).normalized) };
            d = PlacementLogic.Step(d, i, Frame, dt: 0.016f);
            Assert.AreEqual(20f, d.lateral, 0.05f);
            Assert.AreEqual(400f, d.distance, 0.05f);
        }

        [Test]
        public void Drag_RayMissingGround_LeavesDataUnchanged()
        {
            var d = PlacementData.Default();
            var i = new InputFrame { PlaceDragHeld = true, RightAimRay = new Ray(new Vector3(0, 1.6f, 0), Vector3.up) };
            var r = PlacementLogic.Step(d, i, Frame, dt: 0.016f);
            Assert.AreEqual(320f, r.distance, 1e-3f);
            Assert.AreEqual(0f, r.lateral, 1e-3f);
        }

        [Test]
        public void TryRayToGround_ParallelRay_False()
        {
            Assert.IsFalse(PlacementLogic.TryRayToGround(new Ray(new Vector3(0, 1, 0), Vector3.forward), groundY: -120f, out _));
            Assert.IsTrue(PlacementLogic.TryRayToGround(new Ray(new Vector3(0, 1, 0), new Vector3(0, -1, 1).normalized), groundY: -120f, out var hit));
            Assert.AreEqual(-120f, hit.y, 1e-3f);
            Assert.AreEqual(121f, hit.z, 1e-3f);
        }

        [Test]
        public void Reset_RestoresDefaultsButKeepsAnchor()
        {
            var d = PlacementData.Default();
            d.anchorUuid = "w1"; d.frameMode = "Window"; d.heightMeters = 55f; d.distance = 900f;
            d = PlacementLogic.Step(d, new InputFrame { ResetPressed = true }, Frame, dt: 0.016f);
            Assert.AreEqual(100f, d.heightMeters, 1e-3f);
            Assert.AreEqual(320f, d.distance, 1e-3f);
            Assert.AreEqual("w1", d.anchorUuid);
            Assert.AreEqual("Window", d.frameMode);
        }
    }
}
