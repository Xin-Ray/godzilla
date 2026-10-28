using System.Collections.Generic;
using UnityEngine;
using Godzilla.App;
using Godzilla.Core;
using Godzilla.Input;
using Godzilla.Placement;

namespace Godzilla.UI
{
    /// <summary>
    /// Developer panel (Menu held 1 s): placement sliders, occluder/depth toggles, forced calibration mode,
    /// state + FPS readout, and (Task 12) animation test buttons. Navigated with the sticks like every MenuPanel.
    /// </summary>
    public class DevPanel : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] WindowCalibration window;
        [SerializeField] CharacterPlacement placement;
        [SerializeField] AppFlow app;

        MenuPanel _panel;
        float _fps;
        readonly List<MenuRow> _extraRows = new List<MenuRow>();
        public bool Visible => _panel != null && _panel.Visible;
        public string CurrentStateText { get; set; } = "-";

        public void Configure(Transform headAnchor, WindowCalibration w, CharacterPlacement p, AppFlow flow)
        {
            head = headAnchor; window = w; placement = p; app = flow;
        }

        /// <summary>Extra rows appended after the standard ones.</summary>
        public void AddRows(IEnumerable<MenuRow> rows) { _extraRows.AddRange(rows); if (_panel != null) Rebuild(); }

        Godzilla.Character.GiantCharacterController _giant;
        Godzilla.Character.GiantAnimationController _giantAnim;

        /// <summary>Animation test buttons (spec: Idle/Walk/Run/Jump/Punch/Kick/Roar/Reset + Current State).</summary>
        public void ConfigureGiant(Godzilla.Character.GiantCharacterController giant, Godzilla.Character.GiantAnimationController giantAnim)
        {
            _giant = giant; _giantAnim = giantAnim;
            AddRows(new[]
            {
                MenuRow.Text("— Animation test —"),
                MenuRow.Button("Idle", () => _giantAnim.Apply(0f, true, Godzilla.Character.GiantAction.None, placement.Data.animTimeScale)),
                MenuRow.Button("Walk", () => _giantAnim.Apply(0.5f, true, Godzilla.Character.GiantAction.None, placement.Data.animTimeScale)),
                MenuRow.Button("Run", () => _giantAnim.Apply(1f, true, Godzilla.Character.GiantAction.None, placement.Data.animTimeScale)),
                MenuRow.Button("Jump", () => _giant.DebugAction(Godzilla.Character.GiantAction.Jump)),
                MenuRow.Button("Punch", () => _giant.DebugAction(Godzilla.Character.GiantAction.Punch)),
                MenuRow.Button("Kick", () => _giant.DebugAction(Godzilla.Character.GiantAction.Kick)),
                MenuRow.Button("Roar", () => _giant.DebugAction(Godzilla.Character.GiantAction.Roar)),
                MenuRow.Button("Reset (re-apply placement)", () => { placement.Apply(placement.Data); _giant.SyncFromPlacement(); }),
            });
        }

        void Awake()
        {
            _panel = MenuPanel.Create("DevPanel", transform, Vector3.zero, 0.7f, 0.6f, 17);
            Rebuild();
        }

        void Rebuild()
        {
            var rows = new List<MenuRow>
            {
                MenuRow.Text("State", () => app != null ? app.State.ToString() : "-"),
                MenuRow.Text("FPS", () => _fps.ToString("0")),
                MenuRow.Text("Frame", () => window != null ? $"{window.Mode} anchor={Short(window.AnchorUuid)} holes={window.HoleCount}" : "-"),
                MenuRow.Slider("Height m", () => placement.Data.heightMeters, v => Edit(d => d.heightMeters = v), 30f, 200f, 40f, "0"),
                MenuRow.Slider("Distance m", () => placement.Data.distance, v => Edit(d => d.distance = v), 50f, 2000f, 150f, "0"),
                MenuRow.Slider("Lateral m", () => placement.Data.lateral, v => Edit(d => d.lateral = v), -2000f, 2000f, 60f, "0"),
                MenuRow.Slider("Yaw deg", () => placement.Data.yaw, v => Edit(d => d.yaw = Mathf.Repeat(v, 360f)), -1f, 361f, 60f, "0"),
                MenuRow.Slider("Ground drop m", () => placement.Data.groundDrop, v => Edit(d => d.groundDrop = v), 0f, 400f, 30f, "0"),
                MenuRow.Slider("Anim time scale", () => placement.Data.animTimeScale, v => Edit(d => d.animTimeScale = v), 0.1f, 1.5f, 0.3f, "0.00"),
                MenuRow.Toggle("Occluder debug (cyan)", () => window.OccluderDebug, v => window.SetOccluderVisible(v)),
                MenuRow.Toggle("Occluder enabled", () => window.OccluderEnabled, v => window.SetOccluderEnabled(v)),
                MenuRow.Toggle("Depth API occlusion", () => window.DepthApiOn, v => window.SetDepthApi(v)),
                ForcedModeRow(),
                MenuRow.Button("Re-calibrate now", () => app.Recalibrate()),
                MenuRow.Button("Save placement", () => placement.Save()),
                MenuRow.Text("Anim state", () => _giantAnim != null ? _giantAnim.CurrentStateName : CurrentStateText),
            };
            rows.AddRange(_extraRows);
            _panel.SetRows("DEV", rows);
        }

        MenuRow ForcedModeRow()
        {
            string Name() => window.ForcedMode.HasValue ? window.ForcedMode.Value.ToString() : "auto";
            void Cycle(int dir)
            {
                var order = new FrameMode?[] { null, FrameMode.WallAsWindow, FrameMode.HeadFrame };
                int k = System.Array.IndexOf(order, window.ForcedMode);
                window.ForcedMode = order[(k + dir + order.Length) % order.Length];
            }
            return new MenuRow { Label = "Force mode (A cycles)", Value = Name, Activate = () => Cycle(1) };
        }

        void Edit(System.Action<PlacementData> f)
        {
            var d = placement.Data;
            f(d);
            placement.Set(d);
        }

        static string Short(string uuid) => string.IsNullOrEmpty(uuid) ? "-" : (uuid.Length > 8 ? uuid.Substring(0, 8) : uuid);

        public void Toggle()
        {
            _panel.Show(!_panel.Visible);
            if (_panel.Visible && head != null)
            {
                var fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                transform.position = head.position + fwd * 1.0f + Vector3.up * -0.05f;
                transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
        }

        public void Navigate(InputFrame i, float dt) => _panel.Navigate(i, dt);

        void Update()
        {
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(1e-4f, Time.unscaledDeltaTime), 0.1f);
            if (_panel.Visible) _panel.Render();
        }
    }
}
