using UnityEngine;
using Godzilla.Core;
using Godzilla.Input;

namespace Godzilla.Placement
{
    /// <summary>Pure placement rules (spec: 放置交互). Returns the updated data; never touches the scene.</summary>
    public static class PlacementLogic
    {
        public const float MinHeight = 30f, MaxHeight = 200f;
        public const float MinDistance = 50f, MaxDistance = 2000f;
        public const float LateralRatePerDistance = 0.05f; // m/s per metre of distance, at full stick
        public const float HeightRate = 20f;               // m/s at full stick
        public const float YawRate = 45f;                  // deg/s at full stick
        public const float DistanceRate = 0.25f;           // fraction of distance per second at full stick
        public const float StickDeadzone = 0.15f;          // thumbstick drift must not creep the saved placement

        public static PlacementData Step(PlacementData d, InputFrame i, CityFrame f, float dt)
        {
            if (i.ResetPressed)
            {
                var r = PlacementData.Default();
                r.anchorUuid = d.anchorUuid;
                r.frameMode = d.frameMode;
                return r;
            }

            if (i.PlaceDragHeld && TryRayToGround(i.RightAimRay, f.GroundY(d.groundDrop), out var hit))
            {
                var (lat, dist) = f.WorldToCity(hit);
                d.lateral = lat;
                d.distance = dist;
            }

            var move = Deadzone(i.Move);
            var turn = Deadzone(i.Turn);
            d.lateral += move.x * LateralRatePerDistance * d.distance * dt;
            d.heightMeters += move.y * HeightRate * dt;
            d.yaw += turn.x * YawRate * dt;
            d.distance += turn.y * DistanceRate * d.distance * dt;

            d.heightMeters = Mathf.Clamp(d.heightMeters, MinHeight, MaxHeight);
            d.distance = Mathf.Clamp(d.distance, MinDistance, MaxDistance);
            d.yaw = Mathf.Repeat(d.yaw, 360f);
            return d;
        }

        /// <summary>Per-axis deadzone: each placement axis is an independent control (lateral/height, yaw/distance).</summary>
        static Vector2 Deadzone(Vector2 v) => new Vector2(Mathf.Abs(v.x) < StickDeadzone ? 0f : v.x, Mathf.Abs(v.y) < StickDeadzone ? 0f : v.y);

        public static bool TryRayToGround(Ray r, float groundY, out Vector3 hit)
        {
            hit = default;
            if (Mathf.Abs(r.direction.y) < 1e-6f) return false;
            var t = (groundY - r.origin.y) / r.direction.y;
            if (t <= 0f) return false;
            hit = r.origin + r.direction * t;
            return true;
        }
    }
}
