using System;

namespace Godzilla.Placement
{
    /// <summary>Everything needed to put the giant back where it was. Expressed in the CityFrame of <see cref="anchorUuid"/>.</summary>
    [Serializable]
    public class PlacementData
    {
        public int version = 1;
        public string anchorUuid = "";
        public string frameMode = "";      // "Window" | "WallAsWindow" | "HeadFrame"
        public float lateral = 0f;         // m along the window plane
        public float distance = 320f;      // m out of the window
        public float yaw = 0f;             // degrees
        public float heightMeters = 100f;  // 30..200
        public float groundDrop = 120f;    // m below the room floor
        public float animTimeScale = 0.35f;
        public string savedAtUtc = "";

        public const int CurrentVersion = 1;

        public static PlacementData Default() => new PlacementData();

        /// <summary>Clamp everything a hand-edited or corrupted file could break (0/negative/NaN scale, giant inside the room).</summary>
        public PlacementData Sanitize()
        {
            var def = Default();
            lateral = Finite(lateral, def.lateral);
            distance = UnityEngine.Mathf.Clamp(Finite(distance, def.distance), 50f, 2000f);
            yaw = UnityEngine.Mathf.Repeat(Finite(yaw, def.yaw), 360f);
            heightMeters = UnityEngine.Mathf.Clamp(Finite(heightMeters, def.heightMeters), 30f, 200f);
            groundDrop = UnityEngine.Mathf.Clamp(Finite(groundDrop, def.groundDrop), 0f, 1000f);
            animTimeScale = UnityEngine.Mathf.Clamp(Finite(animTimeScale, def.animTimeScale), 0.1f, 1.5f);
            anchorUuid = anchorUuid ?? "";
            frameMode = frameMode ?? "";
            return this;
        }

        static float Finite(float v, float fallback) => float.IsNaN(v) || float.IsInfinity(v) ? fallback : v;
    }
}
