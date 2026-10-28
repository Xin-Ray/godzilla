using System.Collections.Generic;
using UnityEngine;

namespace Godzilla.Core
{
    public enum FrameMode { Window, WallAsWindow, HeadFrame }

    /// <summary>A wall or window plane from the room scan, reduced to what the chooser needs.</summary>
    public readonly struct PlaneCandidate
    {
        public readonly string Uuid;
        public readonly Vector3 Center;
        public readonly Vector3 Normal;
        public readonly Vector3 Right;
        public readonly Vector3 Up;
        public readonly Vector2 HalfSize;
        public readonly bool IsWindow;
        public float Area => 4f * HalfSize.x * HalfSize.y;

        public PlaneCandidate(string uuid, Vector3 center, Vector3 normal, Vector3 right, Vector3 up, Vector2 halfSize, bool isWindow)
        {
            Uuid = uuid; Center = center; Normal = normal.normalized; Right = right.normalized; Up = up.normalized; HalfSize = halfSize; IsWindow = isWindow;
        }

        /// <summary>Ray/rect intersection in front of the ray origin. Returns distance along the ray or null.</summary>
        public float? Raycast(Ray ray)
        {
            var denom = Vector3.Dot(Normal, ray.direction);
            if (Mathf.Abs(denom) < 1e-6f) return null;
            var t = Vector3.Dot(Center - ray.origin, Normal) / denom;
            if (t <= 0f) return null;
            var p = ray.origin + ray.direction * t - Center;
            if (Mathf.Abs(Vector3.Dot(p, Right)) > HalfSize.x) return null;
            if (Mathf.Abs(Vector3.Dot(p, Up)) > HalfSize.y) return null;
            return t;
        }
    }

    /// <summary>
    /// Picks the plane that defines the CityFrame:
    /// nearest window hit by the head ray → largest window → nearest wall hit by the head ray (WallAsWindow) → HeadFrame.
    /// </summary>
    public static class WindowCalibrationLogic
    {
        public static (FrameMode mode, PlaneCandidate? pick) Choose(IReadOnlyList<PlaneCandidate> planes, Ray headRay)
        {
            PlaneCandidate? hitWindow = Nearest(planes, headRay, wantWindow: true);
            if (hitWindow.HasValue) return (FrameMode.Window, hitWindow);

            PlaneCandidate? largest = null;
            foreach (var p in planes)
                if (p.IsWindow && (!largest.HasValue || p.Area > largest.Value.Area)) largest = p;
            if (largest.HasValue) return (FrameMode.Window, largest);

            PlaneCandidate? hitWall = Nearest(planes, headRay, wantWindow: false);
            if (hitWall.HasValue) return (FrameMode.WallAsWindow, hitWall);

            return (FrameMode.HeadFrame, null);
        }

        static PlaneCandidate? Nearest(IReadOnlyList<PlaneCandidate> planes, Ray ray, bool wantWindow)
        {
            PlaneCandidate? best = null;
            float bestT = float.MaxValue;
            foreach (var p in planes)
            {
                if (p.IsWindow != wantWindow) continue;
                var t = p.Raycast(ray);
                if (t.HasValue && t.Value < bestT) { bestT = t.Value; best = p; }
            }
            return best;
        }
    }
}
