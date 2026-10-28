using UnityEngine;

namespace Godzilla.Core
{
    /// <summary>
    /// The city coordinate frame: origin on the room floor under the window, Forward pointing
    /// out of the window (horizontal), Right = Cross(Up, Forward). 1 unit = 1 m.
    /// </summary>
    public readonly struct CityFrame
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Forward;
        public readonly Vector3 Right;
        public float FloorY => Origin.y;

        public CityFrame(Vector3 originOnFloor, Vector3 forwardXZ)
        {
            Origin = originOnFloor;
            var f = new Vector3(forwardXZ.x, 0f, forwardXZ.z);
            Forward = f.sqrMagnitude < 1e-8f ? Vector3.forward : f.normalized;
            Right = Vector3.Cross(Vector3.up, Forward);
        }

        public Vector3 CityToWorld(float lateral, float distance, float y)
        {
            return Origin + Right * lateral + Forward * distance + Vector3.up * (y - Origin.y);
        }

        public (float lateral, float distance) WorldToCity(Vector3 world)
        {
            var d = world - Origin;
            return (Vector3.Dot(d, Right), Vector3.Dot(d, Forward));
        }

        public float GroundY(float groundDrop) => Origin.y - groundDrop;

        /// <summary>Build a frame from a wall/window plane; the normal is flipped to point away from the room centre.</summary>
        public static CityFrame FromPlane(Vector3 center, Vector3 normal, Vector3 roomCenter, float floorY)
        {
            var outward = Vector3.Dot(normal, center - roomCenter) < 0f ? -normal : normal;
            var origin = new Vector3(center.x, floorY, center.z);
            return new CityFrame(origin, outward);
        }
    }
}
