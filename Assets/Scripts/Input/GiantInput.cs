using UnityEngine;
using UnityEngine.InputSystem;

namespace Godzilla.Input
{
    /// <summary>
    /// Reads the "Giant" action map (Touch controllers or keyboard/mouse) into one InputFrame per frame.
    /// Aim ray comes from the right controller anchor on device, or the main camera through the mouse in the editor sim.
    /// </summary>
    public class GiantInput : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;
        [SerializeField] Transform head;            // CenterEyeAnchor
        [SerializeField] Transform rightController; // RightHandAnchor (may be null in sim)
        [SerializeField] float menuHoldSeconds = 1f;

        public InputFrame Current { get; private set; }
        public InputActionAsset Actions => actions;
        public bool SimMode { get; set; }

        InputAction _move, _turn, _jump, _punch, _kick, _roar, _run, _menu, _placeDrag, _confirm;
        float _menuDownTime = -1f;
        bool _menuLongFired;

        public void Configure(InputActionAsset asset, Transform headAnchor, Transform rightHand)
        {
            actions = asset; head = headAnchor; rightController = rightHand;
        }

        void OnEnable()
        {
            if (actions == null) { Debug.LogError("[App] GiantInput has no actions asset"); return; }
            var map = actions.FindActionMap("Giant", throwIfNotFound: true);
            _move = map.FindAction("Move"); _turn = map.FindAction("Turn");
            _jump = map.FindAction("Jump"); _punch = map.FindAction("Punch"); _kick = map.FindAction("Kick"); _roar = map.FindAction("Roar");
            _run = map.FindAction("Run"); _menu = map.FindAction("Menu"); _placeDrag = map.FindAction("PlaceDrag"); _confirm = map.FindAction("Confirm");
            map.Enable();
        }

        void OnDisable()
        {
            if (actions != null) actions.FindActionMap("Giant")?.Disable();
        }

        void Update()
        {
            if (_move == null) return;
            var f = new InputFrame
            {
                Move = _move.ReadValue<Vector2>(),
                Turn = _turn.ReadValue<Vector2>(),
                Run = _run.IsPressed(),
                JumpPressed = _jump.WasPressedThisFrame(),
                PunchPressed = _punch.WasPressedThisFrame(),
                KickPressed = _kick.WasPressedThisFrame(),
                RoarPressed = _roar.WasPressedThisFrame(),
                PlaceDragHeld = _placeDrag.IsPressed(),
                ConfirmPressed = _confirm.WasPressedThisFrame(),
            };
            // Placement-only aliases share physical buttons with Play actions; AppFlow decides which to honour.
            f.ResetPressed = f.PunchPressed;              // B
            f.ToggleOccluderDebugPressed = f.KickPressed; // X

            // Menu: short press fires on release before the hold threshold; long press fires once at the threshold.
            if (_menu.WasPressedThisFrame()) { _menuDownTime = Time.unscaledTime; _menuLongFired = false; }
            if (_menu.IsPressed() && _menuDownTime >= 0f && !_menuLongFired && Time.unscaledTime - _menuDownTime >= menuHoldSeconds)
            {
                f.MenuHeld1s = true; _menuLongFired = true;
            }
            if (_menu.WasReleasedThisFrame())
            {
                if (!_menuLongFired) f.MenuPressed = true;
                _menuDownTime = -1f;
            }

            if (head != null) { f.HeadPos = head.position; f.HeadFwd = head.forward; }
            f.RightAimRay = AimRay();
            Current = f;
        }

        Ray AimRay()
        {
            if (!SimMode && rightController != null) return new Ray(rightController.position, rightController.forward);
            var cam = Camera.main;
            if (cam == null) return new Ray(head != null ? head.position : Vector3.zero, head != null ? head.forward : Vector3.forward);
            var mouse = Mouse.current;
            if (mouse == null) return new Ray(cam.transform.position, cam.transform.forward);
            return cam.ScreenPointToRay(mouse.position.ReadValue());
        }
    }
}
