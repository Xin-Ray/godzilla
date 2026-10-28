using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;

namespace Godzilla.Editor
{
    /// <summary>
    /// Builds Assets/Scenes/City.unity from code. Idempotent: re-running rebuilds the scene
    /// from scratch so the script stays the single source of truth for scene composition.
    /// Run headless:  Unity.exe -batchmode -quit -projectPath . -executeMethod Godzilla.Editor.ProjectBootstrap.Run
    /// </summary>
    public static class ProjectBootstrap
    {
        public const string ScenePath = "Assets/Scenes/City.unity";
        const string CameraRigPrefab = "Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab";

        static readonly string[] RequiredAndroidFeatures =
        {
            "com.unity.openxr.feature.metaquest",
            "com.meta.openxr.feature.metaxr",
            "com.unity.openxr.feature.input.oculustouch",
            "com.unity.openxr.feature.arfoundation-meta-session",
            "com.unity.openxr.feature.arfoundation-meta-camera",
            "com.unity.openxr.feature.arfoundation-meta-occlusion",
        };

        [MenuItem("Godzilla/Bootstrap Scene")]
        public static void Run()
        {
            try
            {
                Log("bootstrap start");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var rig = BuildCameraRig();
                BuildPassthrough();
                BuildLight();
                var app = new GameObject("App");
                SceneComposer.Compose(app, rig);

                System.IO.Directory.CreateDirectory("Assets/Scenes");
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                Log($"scene saved: {ScenePath}");

                ReportOpenXRFeatures();
                AssetDatabase.SaveAssets();
                Log("bootstrap done");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Bootstrap] FAILED: {e}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        static GameObject BuildCameraRig()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPrefab);
            if (prefab == null) throw new Exception($"missing prefab {CameraRigPrefab}");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rig.name = "OVRCameraRig";

            var manager = rig.GetComponent<OVRManager>() ?? rig.AddComponent<OVRManager>();
            manager.isInsightPassthroughEnabled = true;
            manager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
            manager.enableDynamicResolution = false; // myroom lesson: never on
            var so = new SerializedObject(manager);
            SetBool(so, "requestScenePermissionOnStartup", true);
            SetBool(so, "_enableDynamicResolution", false);
            so.ApplyModifiedPropertiesWithoutUndo();

            var cam = rig.GetComponentsInChildren<Camera>(true).First(c => c.gameObject.name == "CenterEyeAnchor");
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 3000f;
            cam.tag = "MainCamera";
            // Left/right eye cameras exist in the prefab but are disabled when usePerEyeCameras = false.
            foreach (var c in rig.GetComponentsInChildren<Camera>(true))
            {
                c.farClipPlane = 3000f;
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = new Color(0, 0, 0, 0);
            }
            Log("camera rig: passthrough on, floor origin, far clip 3000, dynamic resolution off");
            return rig;
        }

        static void BuildPassthrough()
        {
            var go = new GameObject("Passthrough");
            var layer = go.AddComponent<OVRPassthroughLayer>();
            layer.overlayType = OVROverlay.OverlayType.Underlay;
            layer.projectionSurfaceType = OVRPassthroughLayer.ProjectionSurfaceType.Reconstructed;
            layer.compositionDepth = 0;
        }

        static void BuildLight()
        {
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.85f, 0.7f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.None;
            go.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
        }

        static void ReportOpenXRFeatures()
        {
            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null) { Debug.LogError("[Bootstrap] no OpenXR settings for Android"); return; }
            var enabled = settings.GetFeatures().Where(f => f != null && f.enabled).Select(f => f.GetType().Name).OrderBy(n => n).ToArray();
            Log("android openxr features enabled: " + string.Join(", ", enabled));
            foreach (var id in RequiredAndroidFeatures)
            {
                var f = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, id);
                var state = f == null ? "MISSING" : (f.enabled ? "on" : "OFF");
                if (f == null || !f.enabled) Debug.LogError($"[Bootstrap] required feature {id}: {state}");
                else Log($"feature {id}: on");
            }
            foreach (var id in new[] { "com.meta.openxr.feature.foveation", "com.meta.openxr.feature.subsampledLayout" })
            {
                var f = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, id);
                if (f != null && f.enabled) { f.enabled = false; EditorUtility.SetDirty(f); Log($"feature {id}: forced OFF"); }
            }
        }

        static void SetBool(SerializedObject so, string prop, bool value)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogWarning($"[Bootstrap] property {prop} not found"); return; }
            p.boolValue = value;
        }

        static void Log(string msg) => Debug.Log($"[Bootstrap] {msg}");
    }

    /// <summary>Later tasks register the App-root components here so the scene stays reproducible.</summary>
    public static class SceneComposer
    {
        public static void Compose(GameObject app, GameObject cameraRig)
        {
            // Task 3+: MRUK, EffectMesh, EnvironmentDepthManager, CitySpaceManager, WindowCalibration, ...
        }
    }
}
