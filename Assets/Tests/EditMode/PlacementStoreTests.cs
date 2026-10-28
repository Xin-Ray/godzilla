using System.IO;
using NUnit.Framework;
using Godzilla.Placement;

namespace Godzilla.Tests
{
    public class PlacementStoreTests
    {
        string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "godzilla_tests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void Default_HasSpecValues()
        {
            var d = PlacementData.Default();
            Assert.AreEqual(1, d.version);
            Assert.AreEqual(320f, d.distance);
            Assert.AreEqual(100f, d.heightMeters);
            Assert.AreEqual(120f, d.groundDrop);
            Assert.AreEqual(0.35f, d.animTimeScale, 1e-6f);
            Assert.AreEqual(0f, d.lateral);
            Assert.AreEqual(0f, d.yaw);
        }

        [Test]
        public void SaveThenLoad_RoundTrips()
        {
            var d = PlacementData.Default();
            d.anchorUuid = "abc-123";
            d.frameMode = "Window";
            d.lateral = -12.5f; d.distance = 410f; d.yaw = 33f; d.heightMeters = 150f; d.groundDrop = 118f; d.animTimeScale = 0.5f;
            PlacementStore.Save(d, _dir);

            Assert.IsTrue(File.Exists(PlacementStore.PathFor(_dir)));
            var result = PlacementStore.Load(_dir, "abc-123", out var loaded);
            Assert.AreEqual(PlacementStore.LoadResult.Restored, result);
            Assert.AreEqual(-12.5f, loaded.lateral);
            Assert.AreEqual(410f, loaded.distance);
            Assert.AreEqual(33f, loaded.yaw);
            Assert.AreEqual(150f, loaded.heightMeters);
            Assert.AreEqual(118f, loaded.groundDrop);
            Assert.AreEqual(0.5f, loaded.animTimeScale, 1e-6f);
            Assert.AreEqual("Window", loaded.frameMode);
            Assert.IsFalse(string.IsNullOrEmpty(loaded.savedAtUtc), "Save stamps savedAtUtc");
        }

        [Test]
        public void LoadMissing_ReturnsNone()
        {
            var result = PlacementStore.Load(_dir, "anything", out var data);
            Assert.AreEqual(PlacementStore.LoadResult.None, result);
            Assert.IsNotNull(data, "caller always gets usable defaults");
            Assert.AreEqual(320f, data.distance);
        }

        [Test]
        public void LoadMismatchedUuid_ReturnsNeedsPlacement()
        {
            var d = PlacementData.Default();
            d.anchorUuid = "old-window";
            d.heightMeters = 77f;
            PlacementStore.Save(d, _dir);

            var result = PlacementStore.Load(_dir, "new-window", out var data);
            Assert.AreEqual(PlacementStore.LoadResult.NeedsPlacement, result);
            Assert.AreEqual(77f, data.heightMeters, "previous numbers are the starting point for re-placement");
            Assert.IsTrue(File.Exists(PlacementStore.PathFor(_dir)), "old file stays until a new save");
        }

        [Test]
        public void Load_BadValues_AreSanitized()
        {
            File.WriteAllText(PlacementStore.PathFor(_dir),
                "{\"version\":1,\"anchorUuid\":\"w\",\"frameMode\":\"Window\",\"lateral\":5,\"distance\":0,\"yaw\":725,\"heightMeters\":-3,\"groundDrop\":-50,\"animTimeScale\":99,\"savedAtUtc\":\"x\"}");
            var r = PlacementStore.Load(_dir, "w", out var d);
            Assert.AreEqual(PlacementStore.LoadResult.Restored, r);
            Assert.AreEqual(30f, d.heightMeters, "height clamped to 30");
            Assert.AreEqual(50f, d.distance, "distance clamped to 50");
            Assert.AreEqual(5f, d.yaw, 1e-3f, "yaw wrapped");
            Assert.AreEqual(0f, d.groundDrop, "groundDrop >= 0");
            Assert.AreEqual(1.5f, d.animTimeScale, 1e-6f, "animTimeScale clamped");
        }

        [Test]
        public void Load_UnparsableNumbers_AreSafe()
        {
            File.WriteAllText(PlacementStore.PathFor(_dir),
                "{\"version\":1,\"anchorUuid\":\"w\",\"frameMode\":\"Window\",\"lateral\":\"NaN\",\"distance\":\"NaN\",\"yaw\":0,\"heightMeters\":\"NaN\",\"groundDrop\":120,\"animTimeScale\":0.35,\"savedAtUtc\":\"x\"}");
            PlacementStore.Load(_dir, "w", out var d);
            // JsonUtility reads the unparsable numbers as 0; Sanitize must then clamp them into a safe range.
            Assert.IsFalse(float.IsNaN(d.lateral));
            Assert.GreaterOrEqual(d.distance, 50f, "never inside the room");
            Assert.GreaterOrEqual(d.heightMeters, 30f, "never a zero/negative scale");
        }

        [Test]
        public void Load_WrongVersion_ReturnsNone()
        {
            File.WriteAllText(PlacementStore.PathFor(_dir), "{\"version\":7,\"anchorUuid\":\"w\",\"heightMeters\":150}");
            var r = PlacementStore.Load(_dir, "w", out var d);
            Assert.AreEqual(PlacementStore.LoadResult.None, r);
            Assert.AreEqual(100f, d.heightMeters, "defaults, not the unknown-version numbers");
        }

        [Test]
        public void LoadOrNull_MissingFile_ReturnsNull()
        {
            Assert.IsNull(PlacementStore.LoadOrNull(_dir));
        }
    }
}
