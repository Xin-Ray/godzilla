using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Godzilla.Input;

namespace Godzilla.Sim
{
    /// <summary>
    /// Editor / no-HMD fallback: when no XR device is active shortly after start, drives the CenterEye camera with
    /// right-mouse-drag look and T/F/G/H translation (HeadMove action) so parallax can be checked without a headset.
    /// </summary>
    public class EditorSimRig : MonoBehaviour
    {
        [SerializeField] Transform head;            // CenterEyeAnchor
        [SerializeField] GiantInput playerInput;
        [SerializeField] float moveSpeed = 1f;      // m/s
        [SerializeField] float lookDegPerPixel = 0.15f;
        [SerializeField] float activateAfter = 0.5f;
        [SerializeField] float standingHeight = 1.6f;

        public bool Active { get; private set; }
        public bool ForceActive { get; set; } // tests
        float _yaw, _pitch, _t;
        InputAction _look, _lookHold, _move;

        public void Configure(Transform headAnchor, GiantInput input) { head = headAnchor; playerInput = input; }

        void Update()
        {
            if (!Active)
            {
                _t += Time.unscaledDeltaTime;
                if (_t < activateAfter) return;
                if (XRSettings.isDeviceActive && !ForceActive) { enabled = false; return; }
                Activate();
            }
            if (head == null) return;

            if (_lookHold != null && _lookHold.IsPressed() && _look != null)
            {
                var d = _look.ReadValue<Vector2>();
                _yaw += d.x * lookDegPerPixel;
                _pitch = Mathf.Clamp(_pitch - d.y * lookDegPerPixel, -80f, 80f);
                head.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
            if (_move != null)
            {
                var m = _move.ReadValue<Vector2>();
                var fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                var right = Vector3.Cross(Vector3.up, fwd);
                head.position += (fwd * m.y + right * m.x) * moveSpeed * Time.deltaTime;
            }
        }

        void Activate()
        {
            Active = true;
            if (playerInput != null) playerInput.SimMode = true;
            if (head != null)
            {
                head.position = new Vector3(head.position.x, standingHeight, head.position.z);
                _yaw = head.localEulerAngles.y;
            }
            var asset = playerInput != null ? playerInput.Actions : null;
            var map = asset != null ? asset.FindActionMap("Giant") : null;
            if (map != null)
            {
                _look = map.FindAction("HeadLook"); _lookHold = map.FindAction("HeadLookHold"); _move = map.FindAction("HeadMove");
            }
            Debug.Log("[App] sim rig active (no XR device): RMB-drag look, T/F/G/H move");
        }
    }
}
