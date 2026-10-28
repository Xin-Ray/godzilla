using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Godzilla.Core;

namespace Godzilla.Tests
{
    public class WindowCalibrationLogicTests
    {
        static PlaneCandidate Plane(string uuid, Vector3 center, Vector3 normal, Vector2 halfSize, bool isWindow)
        {
            var up = Vector3.up;
            var right = Vector3.Cross(up, normal).normalized;
            return new PlaneCandidate(uuid, center, normal, right, up, halfSize, isWindow);
        }

        // Room: 4 x 5 m, walls at z = +2.5 (north) and x = +2 (east). Head at origin, 1.6 m high.
        static readonly PlaneCandidate NorthWall = Plane("wall-n", new Vector3(0, 1.3f, 2.5f), Vector3.forward, new Vector2(2f, 1.3f), false);
        static readonly PlaneCandidate EastWall = Plane("wall-e", new Vector3(2f, 1.3f, 0), Vector3.right, new Vector2(2.5f, 1.3f), false);
        static readonly PlaneCandidate NorthWindowSmall = Plane("win-n-small", new Vector3(-1.2f, 1.5f, 2.5f), Vector3.forward, new Vector2(0.4f, 0.4f), true);
        static readonly PlaneCandidate NorthWindowBig = Plane("win-n-big", new Vector3(0.8f, 1.5f, 2.5f), Vector3.forward, new Vector2(0.9f, 0.7f), true);
        static readonly PlaneCandidate EastWindow = Plane("win-e", new Vector3(2f, 1.5f, 0.5f), Vector3.right, new Vector2(0.6f, 0.6f), true);

        [Test]
        public void RayHitsWindow_PicksIt()
        {
            var planes = new List<PlaneCandidate> { NorthWall, EastWall, NorthWindowSmall, NorthWindowBig, EastWindow };
            var head = new Ray(new Vector3(0, 1.6f, 0), Vector3.right); // looking east, through the east window
            var (mode, pick) = WindowCalibrationLogic.Choose(planes, head);
            Assert.AreEqual(FrameMode.Window, mode);
            Assert.AreEqual("win-e", pick.Value.Uuid);
        }

        [Test]
        public void RayHitsSmallWindow_PicksItNotLargest()
        {
            var planes = new List<PlaneCandidate> { NorthWall, NorthWindowSmall, NorthWindowBig };
            var head = new Ray(new Vector3(-1.2f, 1.6f, 0), Vector3.forward); // straight at the small window
            var (mode, pick) = WindowCalibrationLogic.Choose(planes, head);
            Assert.AreEqual(FrameMode.Window, mode);
            Assert.AreEqual("win-n-small", pick.Value.Uuid);
        }

        [Test]
        public void NoRayHit_PicksLargestWindow()
        {
            var planes = new List<PlaneCandidate> { NorthWall, EastWall, NorthWindowSmall, NorthWindowBig };
            var head = new Ray(new Vector3(0, 1.6f, 0), Vector3.back); // looking at a blank wall direction (no south wall listed)
            var (mode, pick) = WindowCalibrationLogic.Choose(planes, head);
            Assert.AreEqual(FrameMode.Window, mode);
            Assert.AreEqual("win-n-big", pick.Value.Uuid);
        }

        [Test]
        public void NoWindow_RayHitsWall_WallAsWindow()
        {
            var planes = new List<PlaneCandidate> { NorthWall, EastWall };
            var head = new Ray(new Vector3(0, 1.6f, 0), Vector3.forward);
            var (mode, pick) = WindowCalibrationLogic.Choose(planes, head);
            Assert.AreEqual(FrameMode.WallAsWindow, mode);
            Assert.AreEqual("wall-n", pick.Value.Uuid);
        }

        [Test]
        public void NoWindow_NoWallHit_UsesHeadFrame()
        {
            var planes = new List<PlaneCandidate> { NorthWall, EastWall };
            var head = new Ray(new Vector3(0, 1.6f, 0), Vector3.back); // doorway direction, nothing listed there
            var (mode, pick) = WindowCalibrationLogic.Choose(planes, head);
            Assert.AreEqual(FrameMode.HeadFrame, mode);
            Assert.IsFalse(pick.HasValue);
        }

        [Test]
        public void EmptyRoom_UsesHeadFrame()
        {
            var (mode, pick) = WindowCalibrationLogic.Choose(new List<PlaneCandidate>(), new Ray(Vector3.zero, Vector3.forward));
            Assert.AreEqual(FrameMode.HeadFrame, mode);
            Assert.IsFalse(pick.HasValue);
        }

        [Test]
        public void RayHit_IgnoresPlanesBehindTheHead()
        {
            var planes = new List<PlaneCandidate> { NorthWall, NorthWindowBig };
            var head = new Ray(new Vector3(0.8f, 1.6f, 3.5f), Vector3.forward); // already past the north wall, looking away
            var (mode, pick) = WindowCalibrationLogic.Choose(planes, head);
            Assert.AreEqual(FrameMode.Window, mode, "falls back to the largest window rather than a behind-the-head hit");
            Assert.AreEqual("win-n-big", pick.Value.Uuid);
        }
    }
}
