using System;
using UnityEngine;

namespace Godzilla.Core
{
    /// <summary>Holds the current CityFrame and exposes the virtual city ground plane.</summary>
    public class CitySpaceManager : MonoBehaviour
    {
        public CityFrame Frame { get; private set; }
        public bool HasFrame { get; private set; }
        public event Action<CityFrame> FrameChanged;

        public void SetFrame(CityFrame f)
        {
            Frame = f;
            HasFrame = true;
            Debug.Log($"[City] frame origin={f.Origin} forward={f.Forward}");
            FrameChanged?.Invoke(f);
        }

        public Plane GroundPlane(float groundDrop) => new Plane(Vector3.up, new Vector3(0f, Frame.GroundY(groundDrop), 0f));
    }
}
