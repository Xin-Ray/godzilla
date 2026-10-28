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

        public static PlacementData Default() => new PlacementData();
    }
}
