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
        public void LoadOrNull_MissingFile_ReturnsNull()
        {
            Assert.IsNull(PlacementStore.LoadOrNull(_dir));
        }
    }
}
