using UnityEngine;
using Godzilla.Core;
using Godzilla.Input;

namespace Godzilla.Placement
{
    /// <summary>
    /// Owns the PlacementData, applies it to the character root (position/yaw/scale in the CityFrame),
    /// and persists it. Placement-mode input is routed here by AppFlow.
    /// </summary>
    public class CharacterPlacement : MonoBehaviour
    {
        [SerializeField] CitySpaceManager city;
        [SerializeField] Transform characterRoot;
        [SerializeField] float modelHeightMeters = 1.8f;

        public PlacementData Data { get; private set; } = PlacementData.Default();
        public float ModelHeightMeters => modelHeightMeters;
        public Transform CharacterRoot => characterRoot;
        public float ScaleFactor => Data.heightMeters / Mathf.Max(0.01f, modelHeightMeters);
        public float GroundY => city.Frame.GroundY(Data.groundDrop);
        public string SaveDir => Application.persistentDataPath;

        public void Configure(CitySpaceManager cityManager, Transform root, float modelHeight)
        {
            city = cityManager; characterRoot = root; modelHeightMeters = modelHeight;
        }

        /// <summary>Load for the current anchor. Returns the store result so AppFlow can pick the state.</summary>
        public PlacementStore.LoadResult LoadFor(string anchorUuid, string frameMode)
        {
            var result = PlacementStore.Load(SaveDir, anchorUuid, out var data);
            data.anchorUuid = anchorUuid;
            data.frameMode = frameMode;
            Data = data;
            Apply(Data);
            Debug.Log($"[Place] load={result} anchor={anchorUuid} mode={frameMode} h={data.heightMeters} d={data.distance} lat={data.lateral} yaw={data.yaw}");
            return result;
        }

        public void StepPlacement(InputFrame input, float dt)
        {
            Data = PlacementLogic.Step(Data, input, city.Frame, dt);
            Apply(Data);
        }

        public void Set(PlacementData d) { Data = d; Apply(d); }

        /// <summary>Scale, rotate and position the character root from the data. Feet on the virtual ground.</summary>
        public void Apply(PlacementData d)
        {
            if (characterRoot == null || city == null || !city.HasFrame) return;
            var f = city.Frame;
            var pos = f.CityToWorld(d.lateral, d.distance, f.GroundY(d.groundDrop));
            characterRoot.position = pos;
            characterRoot.rotation = Quaternion.LookRotation(f.Forward, Vector3.up) * Quaternion.Euler(0f, d.yaw, 0f);
            characterRoot.localScale = Vector3.one * (d.heightMeters / Mathf.Max(0.01f, modelHeightMeters));
        }

        public void Save()
        {
            PlacementStore.Save(Data, SaveDir);
            Debug.Log($"[Place] saved anchor={Data.anchorUuid} h={Data.heightMeters} d={Data.distance} lat={Data.lateral} yaw={Data.yaw} drop={Data.groundDrop}");
        }
    }
}
