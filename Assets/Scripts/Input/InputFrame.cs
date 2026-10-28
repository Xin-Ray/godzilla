using UnityEngine;

namespace Godzilla.Input
{
    /// <summary>One frame of player intent, independent of device (Touch controllers or keyboard/mouse sim).</summary>
    public struct InputFrame
    {
        public Vector2 Move;          // left stick / WASD
        public Vector2 Turn;          // right stick / QE + arrows
        public bool Run;              // left trigger held / shift
        public bool JumpPressed;      // A / space
        public bool PunchPressed;     // B / J
        public bool KickPressed;      // X / K
        public bool RoarPressed;      // Y / L
        public bool MenuPressed;      // left menu short press / Esc
        public bool MenuHeld1s;       // left menu held >= 1 s (fires once)
        public bool PlaceDragHeld;    // right trigger held (Placement only)
        public bool ConfirmPressed;   // A (Placement only)
        public bool ResetPressed;     // B (Placement only)
        public bool ToggleOccluderDebugPressed; // X (Placement only)
        public Ray RightAimRay;       // right controller ray / camera ray through mouse
        public Vector3 HeadPos;
        public Vector3 HeadFwd;
    }
}
