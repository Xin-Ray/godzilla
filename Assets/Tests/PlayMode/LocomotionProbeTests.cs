using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Godzilla.App;
using Godzilla.Character;
using Godzilla.Core;
using Godzilla.Input;
using Godzilla.Placement;

namespace Godzilla.Tests
{
    /// <summary>
    /// T-MVP-010/011/012 (editor half): drive the giant through walk, run, turn, jump, punch, kick, roar with injected
    /// input, capture frame sequences from a side probe camera that follows the giant, and record telemetry.
    /// The PNGs are judged by a human/agent afterwards; the asserts here cover telemetry only.
    /// </summary>
    public class LocomotionProbeTests
    {
        const string TestId = "T-MVP-010";
        const int W = 640, H = 480;

        AppFlow _flow; CharacterPlacement _placement; GiantCharacterController _giant; GiantAnimationController _anim; Camera _cam;
        readonly List<string> _log = new List<string>();

        IEnumerator Boot()
        {
            SceneManager.LoadScene("City");
            yield return null;
            var window = Object.FindFirstObjectByType<WindowCalibration>();
            _flow = Object.FindFirstObjectByType<AppFlow>();
            _placement = Object.FindFirstObjectByType<CharacterPlacement>();
            _giant = Object.FindFirstObjectByType<GiantCharacterController>();
            _anim = Object.FindFirstObjectByType<GiantAnimationController>();
            Assert.IsNotNull(_giant, "rigged giant present in scene");
            float t = 0f;
            while (!window.Ready && t < 10f) { t += Time.deltaTime; yield return null; }
            var d = PlacementData.Default(); d.anchorUuid = window.AnchorUuid; d.frameMode = window.Mode.ToString();
            _placement.Set(d);
            if (_flow.State == AppState.Placement) _flow.Fire(AppEvent.Confirm);
            Assert.AreEqual(AppState.Play, _flow.State);
            _flow.InputOverride = new InputFrame();
            yield return null;

            _cam = new GameObject("LocoProbe").AddComponent<Camera>();
            _cam.fieldOfView = 50f; _cam.nearClipPlane = 1f; _cam.farClipPlane = 5000f;
            _cam.clearFlags = CameraClearFlags.SolidColor; _cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        }

        void Frame(string name)
        {
            var root = _placement.CharacterRoot;
            float h = _placement.Data.heightMeters;
            // side view, slightly in front, following the giant
            _cam.transform.position = root.position + root.right * (2.2f * h) + root.forward * (0.6f * h) + Vector3.up * (0.55f * h);
            _cam.transform.LookAt(root.position + Vector3.up * (0.5f * h));
            var tex = ProbeCapture.Capture(_cam, W, H);
            ProbeCapture.Save(tex, TestId, name);
            Object.Destroy(tex);
            var line = $"{name}: pos={root.position:F1} yaw={root.eulerAngles.y:0} state={_anim.CurrentStateName} grounded={_giant.State.Grounded} locked={_giant.Logic.Locked}";
            _log.Add(line);
            Debug.Log("[Evidence] " + line);
        }

        IEnumerator Hold(InputFrame i, float seconds)
        {
            _flow.InputOverride = i;
            float t = 0f;
            while (t < seconds) { t += Time.deltaTime; yield return null; }
        }

        IEnumerator Press(InputFrame i)
        {
            _flow.InputOverride = i;
            yield return null;
            _flow.InputOverride = new InputFrame();
        }

        [UnityTest]
        public IEnumerator Walk_Run_Turn_Sequences()
        {
            yield return Boot();
            var start = _placement.CharacterRoot.position;
            float h = _placement.Data.heightMeters;
            float scale = _placement.ScaleFactor, ts = _placement.Data.animTimeScale;

            // walk: 6 frames at 0.25 s
            var positions = new List<Vector3>();
            for (int k = 0; k < 6; k++) { yield return Hold(new InputFrame { Move = new Vector2(0, 1f) }, 0.25f); Frame($"walk_{k}"); positions.Add(_placement.CharacterRoot.position); }
            float walked = Vector3.Distance(start, positions[positions.Count - 1]);
            Assert.Greater(walked, 0.5f * 1.4f * scale * ts * 1.5f, "walked a sizeable distance in 1.5 s");
            for (int k = 1; k < positions.Count; k++) Assert.Greater(Vector3.Distance(positions[k - 1], positions[k]), 0f, "continuous displacement");
            Assert.AreEqual("Walk", _anim.CurrentStateName);

            // run: 5 frames at 0.2 s
            var runStart = _placement.CharacterRoot.position;
            for (int k = 0; k < 5; k++) { yield return Hold(new InputFrame { Move = new Vector2(0, 1f), Run = true }, 0.2f); Frame($"run_{k}"); }
            float ran = Vector3.Distance(runStart, _placement.CharacterRoot.position);
            Assert.Greater(ran / 1.0f, walked / 1.5f * 1.8f, "run is clearly faster than walk");
            Assert.AreEqual("Run", _anim.CurrentStateName);

            // turn in place: 90° in 1.5 s
            float yaw0 = _placement.CharacterRoot.eulerAngles.y;
            yield return Hold(new InputFrame { Turn = new Vector2(1f, 0) }, 1.5f);
            Frame("turn_90");
            Assert.AreEqual(90f, Mathf.DeltaAngle(yaw0, _placement.CharacterRoot.eulerAngles.y), 8f);

            // idle settles
            yield return Hold(new InputFrame(), 0.6f);
            Frame("idle");
            Assert.AreEqual("Idle", _anim.CurrentStateName);
            Assert.AreEqual(_placement.GroundY, _placement.CharacterRoot.position.y, 1e-3f, "feet on the virtual ground");
        }

        [UnityTest]
        public IEnumerator Jump_And_Actions_Sequences()
        {
            yield return Boot();
            float h = _placement.Data.heightMeters;
            float groundY = _placement.GroundY;

            // jump: frames at 0.2 / mid / landing
            yield return Press(new InputFrame { JumpPressed = true });
            Assert.IsFalse(_giant.State.Grounded, "airborne after jump press");
            yield return Hold(new InputFrame(), 0.2f); Frame("jump_0");
            float apex = _placement.CharacterRoot.position.y; float t = 0f;
            while (!_giant.State.Grounded && t < 5f) { t += Time.deltaTime; apex = Mathf.Max(apex, _placement.CharacterRoot.position.y); if (Mathf.Abs(t - 0.9f) < Time.deltaTime) Frame("jump_1"); yield return null; }
            Frame("jump_2");
            Assert.IsTrue(_giant.State.Grounded, "lands");
            Assert.Greater(apex - groundY, 0.2f * h, "apex is a real jump (≥ 0.2 × height)");
            Assert.AreEqual(groundY, _placement.CharacterRoot.position.y, 1e-3f);

            foreach (var (name, frame) in new[] {
                ("punch", new InputFrame { PunchPressed = true }),
                ("kick", new InputFrame { KickPressed = true }),
                ("roar", new InputFrame { RoarPressed = true }) })
            {
                yield return Press(frame);
                Assert.IsTrue(_giant.Logic.Locked, name + " locks locomotion");
                yield return Hold(new InputFrame { Move = new Vector2(0, 1f) }, 0.15f); Frame($"{name}_0");
                var p0 = _placement.CharacterRoot.position;
                yield return Hold(new InputFrame { Move = new Vector2(0, 1f) }, 0.6f); Frame($"{name}_1");
                Assert.AreEqual(0f, Vector3.Distance(p0, _placement.CharacterRoot.position), 1e-3f, name + ": no movement while locked even with stick held");
                // mash during the action: nothing queues
                yield return Press(new InputFrame { PunchPressed = true, KickPressed = true, RoarPressed = true, JumpPressed = true });
                Assert.AreEqual(GiantAction.None, _giant.Logic.Pending);
                t = 0f;
                while (_giant.Logic.Locked && t < 12f) { t += Time.deltaTime; yield return null; }
                Frame($"{name}_2");
                Assert.IsFalse(_giant.Logic.Locked, name + " unlocks when the clip exits (ActionExitNotifier)");
            }
            Assert.IsTrue(_giant.State.Grounded);

            System.IO.File.WriteAllLines(System.IO.Path.Combine(ProbeCapture.EvidenceDir(TestId), "telemetry.txt"), _log);
        }
    }
}
