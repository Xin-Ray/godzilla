using UnityEngine;
using Godzilla.App;
using Godzilla.Input;
using Godzilla.Placement;

namespace Godzilla.Character
{
    /// <summary>
    /// Drives the character root in Play state: subscribes to AppFlow.PlayInput, runs GiantAnimLogic (intent)
    /// and GiantMover (kinematics), then hands pose intent to GiantAnimationController.
    /// Position/yaw live in the CityFrame via CharacterPlacement so re-placement and locomotion share one source.
    /// </summary>
    public class GiantCharacterController : MonoBehaviour
    {
        [SerializeField] AppFlow app;
        [SerializeField] CharacterPlacement placement;
        [SerializeField] GiantAnimationController anim;

        public GiantAnimLogic Logic { get; } = new GiantAnimLogic();
        public GiantMover Mover { get; } = new GiantMover();
        public GiantMover.State State => _state;
        GiantMover.State _state;
        bool _stateInit;

        public void Configure(AppFlow flow, CharacterPlacement characterPlacement, GiantAnimationController animation)
        {
            app = flow; placement = characterPlacement; anim = animation;
        }

        void OnEnable()
        {
            if (app != null) { app.PlayInput += OnPlayInput; app.StateChanged += OnStateChanged; }
        }

        void OnDisable()
        {
            if (app != null) { app.PlayInput -= OnPlayInput; app.StateChanged -= OnStateChanged; }
        }

        void OnStateChanged(AppState s)
        {
            if (s == AppState.Play) SyncFromPlacement();
        }

        /// <summary>Adopt the placed pose (after Placement/Restore) as the locomotion state.</summary>
        public void SyncFromPlacement()
        {
            var root = placement.CharacterRoot;
            _state = new GiantMover.State
            {
                Position = root.position,
                Yaw = root.eulerAngles.y,
                VerticalVelocity = 0f,
                Grounded = true,
            };
            _stateInit = true;
        }

        void OnPlayInput(InputFrame i, float dt)
        {
            if (!_stateInit) SyncFromPlacement();
            var d = placement.Data;
            float scale = placement.ScaleFactor;
            float groundY = placement.GroundY;

            Logic.Tick(i, _state.Grounded, dt);
            var action = Logic.ConsumePending();
            var moveInput = i;
            moveInput.Move = Logic.MoveAllowed(i);
            _state = Mover.Step(_state, moveInput, scale, d.animTimeScale, groundY, d.heightMeters, dt, jump: action == GiantAction.Jump);

            var root = placement.CharacterRoot;
            root.position = _state.Position;
            root.rotation = Quaternion.Euler(0f, _state.Yaw, 0f);

            if (anim != null) anim.Apply(Logic.Speed01, _state.Grounded, action, d.animTimeScale);
            if (action != GiantAction.None) Debug.Log($"[Giant] action={action} pos={_state.Position} yaw={_state.Yaw:0}");
        }

        /// <summary>DevPanel: play an action directly (Play state only).</summary>
        public void DebugAction(GiantAction a)
        {
            if (anim == null) return;
            if (a == GiantAction.Jump) { _state = Mover.Step(_state, new InputFrame(), placement.ScaleFactor, placement.Data.animTimeScale, placement.GroundY, placement.Data.heightMeters, 0f, jump: true); }
            else if (a != GiantAction.None) { Logic.Tick(new InputFrame { PunchPressed = a == GiantAction.Punch, KickPressed = a == GiantAction.Kick, RoarPressed = a == GiantAction.Roar }, true, 0f); Logic.ConsumePending(); }
            anim.Apply(Logic.Speed01, _state.Grounded, a, placement.Data.animTimeScale);
        }
    }
}
