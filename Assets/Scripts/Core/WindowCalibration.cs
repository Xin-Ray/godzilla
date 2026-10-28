using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using Meta.XR.EnvironmentDepth;

namespace Godzilla.Core
{
    /// <summary>
    /// Owns the MRUK room load (device scan on hardware, the sim prefab in the editor), picks the window
    /// (or wall / head fallback), sets the CityFrame and builds the depth-only occluder
    /// (walls + ceiling + floor with window/door holes). Re-run with Calibrate().
    /// A failed or timed-out device load lands in HeadFrame (no phantom prefab room on hardware).
    /// </summary>
    public class WindowCalibration : MonoBehaviour
    {
        [SerializeField] CitySpaceManager city;
        [SerializeField] EffectMesh occluder;
        [SerializeField] EnvironmentDepthManager depth;
        [SerializeField] Material occluderMaterial;
        [SerializeField] Material occluderDebugMaterial;
        [SerializeField] Transform head;
        [SerializeField] GameObject simRoomPrefab;          // editor only
        [SerializeField] Renderer[] giantRenderers;          // Depth API material swap
        [SerializeField] Material giantLitMaterial;
        [SerializeField] Material giantOcclusionMaterial;
        [SerializeField] float loadTimeoutSeconds = 20f;

        public FrameMode Mode { get; private set; } = FrameMode.HeadFrame;
        public string AnchorUuid { get; private set; } = "";
        public bool Ready { get; private set; }
        public int HoleCount { get; private set; }
        /// <summary>DevPanel override: null = automatic.</summary>
        public FrameMode? ForcedMode { get; set; }
        public bool OccluderDebug { get; private set; }
        public bool OccluderEnabled { get; private set; } = true;
        public bool DepthApiOn { get; private set; }
        public event Action Calibrated;

        Coroutine _loading;

        public void Configure(CitySpaceManager cityManager, EffectMesh effectMesh, EnvironmentDepthManager depthManager,
            Material occluderMat, Material debugMat, Transform headAnchor, GameObject simRoom,
            Renderer[] giantRends, Material giantLit, Material giantOcclusion)
        {
            city = cityManager; occluder = effectMesh; depth = depthManager; occluderMaterial = occluderMat; occluderDebugMaterial = debugMat;
            head = headAnchor; simRoomPrefab = simRoom; giantRenderers = giantRends; giantLitMaterial = giantLit; giantOcclusionMaterial = giantOcclusion;
        }

        /// <summary>Start (or restart) calibration. Calls Calibrated when the frame is set.</summary>
        public void Calibrate()
        {
            Ready = false;
            if (_loading != null) StopCoroutine(_loading);
            _loading = StartCoroutine(LoadAndCalibrate());
        }

        IEnumerator LoadAndCalibrate()
        {
            var mruk = MRUK.Instance;
            if (mruk == null) { Debug.LogWarning("[Window] no MRUK in scene → HeadFrame"); Finish(null); yield break; }
            if (mruk.GetCurrentRoom() != null) { Finish(mruk.GetCurrentRoom()); yield break; }

            Task<MRUK.LoadDeviceResult> task;
#if UNITY_EDITOR
            if (simRoomPrefab == null) { Debug.LogWarning("[Window] editor: no sim room prefab → HeadFrame"); Finish(null); yield break; }
            Debug.Log("[Window] editor: loading sim room prefab");
            task = mruk.LoadSceneFromPrefab(simRoomPrefab);
#else
            Debug.Log("[Window] loading room from device…");
            task = mruk.LoadSceneFromDevice();
#endif
            float t = 0f;
            while (!task.IsCompleted && t < loadTimeoutSeconds) { t += Time.unscaledDeltaTime; yield return null; }
            if (!task.IsCompleted)
            {
                Debug.LogWarning($"[Window] room load timed out after {loadTimeoutSeconds:0} s → HeadFrame");
                Finish(null); yield break;
            }
            if (task.IsFaulted)
            {
                Debug.LogWarning($"[Window] room load failed: {task.Exception?.GetBaseException().Message} → HeadFrame");
                Finish(null); yield break;
            }
            var result = task.Result;
            MRUKRoom room = null;
            if (result == MRUK.LoadDeviceResult.Success || result == MRUK.LoadDeviceResult.DiscoveryOngoing)
            {
                // MRUK may report before the room objects exist (prefab path returns DiscoveryOngoing); wait for GetCurrentRoom.
                while (mruk.GetCurrentRoom() == null && t < loadTimeoutSeconds) { t += Time.unscaledDeltaTime; yield return null; }
                room = mruk.GetCurrentRoom();
            }
            if (room == null) Debug.LogWarning($"[Window] room load result={result} after {t:0.0}s → HeadFrame");
            Finish(room);
        }

        void Finish(MRUKRoom room)
        {
            _loading = null;
            var planes = room != null ? CollectPlanes(room) : new List<PlaneCandidate>();
            var headRay = head != null ? new Ray(head.position, head.forward) : new Ray(Vector3.zero, Vector3.forward);

            if (ForcedMode == FrameMode.WallAsWindow) planes.RemoveAll(p => p.IsWindow);
            if (ForcedMode == FrameMode.HeadFrame) planes.Clear();

            var (mode, pick) = WindowCalibrationLogic.Choose(planes, headRay);
            Mode = mode;
            float floorY = FloorY(room, headRay.origin);

            if (pick.HasValue)
            {
                var roomCenter = room != null ? room.GetRoomBounds().center : Vector3.zero;
                city.SetFrame(CityFrame.FromPlane(pick.Value.Center, pick.Value.Normal, roomCenter, floorY));
                AnchorUuid = pick.Value.Uuid;
            }
            else
            {
                var origin = new Vector3(headRay.origin.x, floorY, headRay.origin.z);
                city.SetFrame(new CityFrame(origin, headRay.direction));
                AnchorUuid = "headframe";
            }

            BuildOccluder(room, pick);
            Ready = true;
            Debug.Log($"[Window] mode={Mode} anchor={AnchorUuid} holes={HoleCount} forced={(ForcedMode.HasValue ? ForcedMode.ToString() : "none")}");
            Calibrated?.Invoke();
        }

        static float FloorY(MRUKRoom room, Vector3 headPos)
        {
            if (room != null && room.FloorAnchors != null && room.FloorAnchors.Count > 0) return room.FloorAnchors[0].transform.position.y;
            return headPos.y - 1.6f; // no scan: assume standing head height
        }

        static List<PlaneCandidate> CollectPlanes(MRUKRoom room)
        {
            var list = new List<PlaneCandidate>();
            foreach (var a in room.Anchors)
            {
                bool isWindow = a.HasAnyLabel(MRUKAnchor.SceneLabels.WINDOW_FRAME);
                bool isWall = a.HasAnyLabel(MRUKAnchor.SceneLabels.WALL_FACE);
                if (!isWindow && !isWall) continue;
                if (!a.PlaneRect.HasValue) continue;
                var rect = a.PlaneRect.Value;
                var t = a.transform;
                var center = t.TransformPoint(new Vector3(rect.center.x, rect.center.y, 0f));
                list.Add(new PlaneCandidate(AnchorKey(a, center), center, t.forward, t.right, t.up, rect.size * 0.5f, isWindow));
            }
            return list;
        }

        /// <summary>
        /// Device anchors have OS-persistent UUIDs. Prefab rooms (editor sim) get a fresh Guid every load, so there we
        /// key by label + rounded centre so a saved placement can still be restored between editor runs.
        /// </summary>
        static string AnchorKey(MRUKAnchor a, Vector3 center)
        {
            if (!Application.isEditor) return a.Anchor.Uuid.ToString();
            return $"{a.Label}:{center.x:0.0},{center.y:0.0},{center.z:0.0}";
        }

        void BuildOccluder(MRUKRoom room, PlaneCandidate? pick)
        {
            HoleCount = 0;
            if (occluder == null) return;
            occluder.DestroyMesh();
            if (room == null || Mode == FrameMode.HeadFrame || !OccluderEnabled) return;

            occluder.Labels = MRUKAnchor.SceneLabels.WALL_FACE | MRUKAnchor.SceneLabels.CEILING | MRUKAnchor.SceneLabels.FLOOR;
            occluder.CutHoles = MRUKAnchor.SceneLabels.WINDOW_FRAME | MRUKAnchor.SceneLabels.DOOR_FRAME;
            occluder.MeshMaterial = OccluderDebug ? occluderDebugMaterial : occluderMaterial;
            occluder.CreateMesh(room);

            foreach (var a in room.Anchors)
                if (a.HasAnyLabel(occluder.CutHoles)) HoleCount++;

            if (Mode == FrameMode.WallAsWindow && pick.HasValue)
            {
                foreach (var a in room.Anchors)
                {
                    var rect = a.PlaneRect.HasValue ? a.PlaneRect.Value : new Rect();
                    var c = a.transform.TransformPoint(new Vector3(rect.center.x, rect.center.y, 0f));
                    if (AnchorKey(a, c) != pick.Value.Uuid) continue;
                    occluder.DestroyMesh(a);
                    HoleCount++;
                    break;
                }
            }
        }

        public void SetOccluderVisible(bool debug)
        {
            OccluderDebug = debug;
            if (Ready) Rebuild();
        }

        public void SetOccluderEnabled(bool on)
        {
            OccluderEnabled = on;
            if (Ready) Rebuild();
        }

        void Rebuild()
        {
            var room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
            PlaneCandidate? pick = null;
            if (room != null)
                foreach (var p in CollectPlanes(room)) if (p.Uuid == AnchorUuid) pick = p;
            BuildOccluder(room, pick);
        }

        /// <summary>
        /// Depth API occlusion: activates EnvironmentDepthManager and swaps the giant onto Meta's OcclusionLit material
        /// (the only shader that honours the depth keywords). Off = plain URP Lit again.
        /// </summary>
        public void SetDepthApi(bool on)
        {
            if (depth == null) return;
            if (on && !EnvironmentDepthManager.IsSupported) { Debug.LogWarning("[Window] depth API not supported here"); return; }
            if (on) depth.gameObject.SetActive(true);
            depth.OcclusionShadersMode = OcclusionShadersMode.SoftOcclusion;
            depth.enabled = on;
            DepthApiOn = on;
            var mat = on ? giantOcclusionMaterial : giantLitMaterial;
            if (mat != null && giantRenderers != null)
                foreach (var r in giantRenderers)
                    if (r != null) { var mats = r.sharedMaterials; for (int k = 0; k < mats.Length; k++) mats[k] = mat; r.sharedMaterials = mats; }
            Debug.Log($"[Window] depthApi={on}");
        }
    }
}
