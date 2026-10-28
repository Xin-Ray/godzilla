using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Godzilla.App;
using Godzilla.Core;
using Godzilla.Sim;

namespace Godzilla.Tests
{
    public class SimRigTests
    {
        [UnityTest]
        public IEnumerator SimRig_ActivatesInEditor_AndWindowIsFound()
        {
            SceneManager.LoadScene("City");
            yield return null;
            var sim = Object.FindFirstObjectByType<EditorSimRig>();
            var window = Object.FindFirstObjectByType<WindowCalibration>();
            var flow = Object.FindFirstObjectByType<AppFlow>();
            Assert.IsNotNull(sim); Assert.IsNotNull(window); Assert.IsNotNull(flow);

            float t = 0f;
            while ((!sim.Active || !window.Ready) && t < 10f) { t += Time.deltaTime; yield return null; }

            Assert.IsTrue(sim.Active, "sim rig activates when no XR device is present");
            Assert.IsTrue(window.Ready, "calibration finished");
            Assert.AreEqual(FrameMode.Window, window.Mode, "Bedroom00 prefab has a WINDOW_FRAME");
            Assert.Greater(window.HoleCount, 0);
            Assert.AreNotEqual(AppState.Boot, flow.State);
            Assert.AreNotEqual(AppState.LoadingRoom, flow.State);
        }

        [UnityTest]
        public IEnumerator DevPanel_HasAnimationRows_AtRuntime()
        {
            SceneManager.LoadScene("City");
            yield return null; yield return null;
            var dev = Object.FindFirstObjectByType<Godzilla.UI.DevPanel>();
            Assert.IsNotNull(dev);
            var labels = new System.Collections.Generic.List<string>();
            foreach (var r in dev.Rows) labels.Add(r.Label);
            foreach (var expected in new[] { "Idle", "Walk", "Run", "Jump", "Punch", "Kick", "Roar" })
                Assert.Contains(expected, labels, "animation test row present in the built scene");
            Assert.IsTrue(dev.HasGiant, "giant references survive scene serialization");
        }

        [UnityTest]
        public IEnumerator ForcedWallAsWindow_UsesFacingWall_AndExcludesItFromOccluder()
        {
            SceneManager.LoadScene("City");
            yield return null;
            var window = Object.FindFirstObjectByType<WindowCalibration>();
            var flow = Object.FindFirstObjectByType<AppFlow>();
            float t = 0f;
            while (!window.Ready && t < 10f) { t += Time.deltaTime; yield return null; }
            int holesWithWindow = window.HoleCount;

            window.ForcedMode = FrameMode.WallAsWindow;
            flow.Recalibrate(); // Play/Placement â†’ Paused is required by the state machine; drive it directly
            if (flow.State != AppState.LoadingRoom) { flow.Fire(AppEvent.MenuShort); flow.Fire(AppEvent.Recalibrate); }
            t = 0f;
            while (flow.State == AppState.LoadingRoom && t < 10f) { t += Time.deltaTime; yield return null; }

            Assert.AreEqual(FrameMode.WallAsWindow, window.Mode);
            Assert.IsTrue(window.AnchorUuid.StartsWith("WALL_FACE"), window.AnchorUuid);
            Assert.AreEqual(holesWithWindow + 1, window.HoleCount, "the chosen wall counts as one extra opening");
            Assert.AreEqual(AppState.Placement, flow.State, "a different anchor key means re-placement");
        }

        [UnityTest]
        public IEnumerator ForcedHeadFrame_NoOccluder_FrameAheadOfHead()
        {
            SceneManager.LoadScene("City");
            yield return null;
            var window = Object.FindFirstObjectByType<WindowCalibration>();
            var city = Object.FindFirstObjectByType<CitySpaceManager>();
            var flow = Object.FindFirstObjectByType<AppFlow>();
            var head = Camera.main.transform;
            float t = 0f;
            while (!window.Ready && t < 10f) { t += Time.deltaTime; yield return null; }

            window.ForcedMode = FrameMode.HeadFrame;
            if (flow.State != AppState.LoadingRoom) { flow.Fire(AppEvent.MenuShort); flow.Fire(AppEvent.Recalibrate); }
            t = 0f;
            while (flow.State == AppState.LoadingRoom && t < 10f) { t += Time.deltaTime; yield return null; }

            Assert.AreEqual(FrameMode.HeadFrame, window.Mode);
            Assert.AreEqual(0, window.HoleCount, "no occluder in HeadFrame mode");
            var fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            Assert.Greater(Vector3.Dot(city.Frame.Forward, fwd), 0.99f, "frame forward = head yaw");
            Assert.Less(Vector3.Distance(new Vector3(head.position.x, 0, head.position.z), new Vector3(city.Frame.Origin.x, 0, city.Frame.Origin.z)), 0.01f, "origin under the head");
        }
    }
}
