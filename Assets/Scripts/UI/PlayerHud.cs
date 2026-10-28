using UnityEngine;
using Godzilla.App;
using Godzilla.Core;
using Godzilla.Input;
using Godzilla.Placement;

namespace Godzilla.UI
{
    /// <summary>
    /// Player-facing UI: a small hint panel in front of the head (placement instructions, loading text) and the pause menu.
    /// No technical readouts here; those live in DevPanel.
    /// </summary>
    public class PlayerHud : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] AppFlow app;
        [SerializeField] float distance = 1.2f;
        [SerializeField] float followLerp = 4f;

        MenuPanel _hint, _pause;
        bool _follow = true;

        public void Configure(Transform headAnchor, AppFlow flow) { head = headAnchor; app = flow; }

        void Awake()
        {
            _hint = MenuPanel.Create("Hint", transform, Vector3.zero, 0.5f, 0.18f, 20);
            _pause = MenuPanel.Create("PauseMenu", transform, new Vector3(0f, -0.12f, 0f), 0.4f, 0.22f, 20);
            _pause.SetRows("Paused", new[]
            {
                MenuRow.Button("Resume", () => app.Resume()),
                MenuRow.Button("Re-place character", () => app.RePlace()),
                MenuRow.Button("Re-calibrate window", () => app.Recalibrate()),
                MenuRow.Button("Quit", () => app.Quit()),
            });
        }

        void LateUpdate()
        {
            if (head == null || !_follow) return;
            var fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            var target = head.position + fwd * distance + Vector3.up * -0.1f;
            transform.position = Vector3.Lerp(transform.position, target, followLerp * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fwd, Vector3.up), followLerp * Time.deltaTime);
        }

        public void OnState(AppState s)
        {
            _pause.Show(s == AppState.Paused);
            switch (s)
            {
                case AppState.LoadingRoom:
                    _hint.SetRows("", new[] { MenuRow.Text("Looking for your room and window…") });
                    _hint.Show(true);
                    break;
                case AppState.Placement:
                    _hint.Show(true);
                    break;
                case AppState.Play:
                    _hint.SetRows("", new[] { MenuRow.Text("Left stick move · Right stick turn · A jump · B punch · X kick · Y roar · Menu pause") });
                    _hint.Show(true);
                    Invoke(nameof(HideHint), 6f);
                    break;
                case AppState.Paused:
                    _hint.Show(false);
                    break;
            }
        }

        void HideHint() { if (app != null && app.State == AppState.Play) _hint.Show(false); }

        public void ShowPlacement(PlacementData d, FrameMode mode)
        {
            string modeText = mode == FrameMode.Window ? "window found" : mode == FrameMode.WallAsWindow ? "facing wall used as window" : "no room scan — facing direction";
            _hint.SetRows("Place the giant (" + modeText + ")", new[]
            {
                MenuRow.Text("Hold right trigger and point at the city to drop it · sticks fine-tune"),
                MenuRow.Text($"Height {d.heightMeters:0} m · Distance {d.distance:0} m · Facing {d.yaw:0}°"),
                MenuRow.Text("A confirm · B reset · X show walls · Menu pause"),
            });
        }

        public void NavigatePause(InputFrame i, float dt) => _pause.Navigate(i, dt);
    }
}
