using NUnit.Framework;
using UnityEngine;
using Godzilla.Core;

namespace Godzilla.Tests
{
    public class CityFrameTests
    {
        const float Eps = 1e-3f;

        [Test]
        public void CityToWorld_UsesRightAndForward()
        {
            // origin (1,0,2), forward +X  => Right = Cross(Up, Forward) = (0,0,-1)
            var f = new CityFrame(new Vector3(1, 0, 2), Vector3.right);
            var w = f.CityToWorld(lateral: 10f, distance: 320f, y: 0f);
            Assert.AreEqual(321f, w.x, Eps);
            Assert.AreEqual(0f, w.y, Eps);
            Assert.AreEqual(-8f, w.z, Eps);
        }

        [Test]
        public void WorldToCity_RoundTrips()
        {
            var f = new CityFrame(new Vector3(-3, 1.2f, 7), new Vector3(0.6f, 0, 0.8f));
            var w = f.CityToWorld(42f, 250f, -120f);
            var (lat, dist) = f.WorldToCity(w);
            Assert.AreEqual(42f, lat, Eps);
            Assert.AreEqual(250f, dist, Eps);
        }

        [Test]
        public void FromPlane_FlipsNormalTowardOutside()
        {
            // window centre at z=+5, room centre at origin: normal must point +Z regardless of input sign
            var f = CityFrame.FromPlane(center: new Vector3(0, 1.5f, 5), normal: Vector3.back, roomCenter: Vector3.zero, floorY: 0f);
            Assert.AreEqual(1f, Vector3.Dot(f.Forward, Vector3.forward), Eps);
            Assert.AreEqual(0f, f.Origin.y, Eps, "origin is projected onto the floor");
            Assert.AreEqual(5f, f.Origin.z, Eps);
        }

        [Test]
        public void FromPlane_KeepsNormalAlreadyPointingOutside()
        {
            var f = CityFrame.FromPlane(new Vector3(0, 1.5f, 5), Vector3.forward, Vector3.zero, 0f);
            Assert.AreEqual(1f, Vector3.Dot(f.Forward, Vector3.forward), Eps);
        }

        [Test]
        public void Forward_IsHorizontalAndUnit()
        {
            var f = new CityFrame(Vector3.zero, new Vector3(1, 1, 0));
            Assert.AreEqual(0f, f.Forward.y, Eps);
            Assert.AreEqual(1f, f.Forward.magnitude, Eps);
        }

        [Test]
        public void GroundY_SubtractsDrop()
        {
            var f = new CityFrame(new Vector3(0, 0, 0), Vector3.forward);
            Assert.AreEqual(-120f, f.GroundY(120f), Eps);
        }
    }
}
