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

    /// <summary>Composes the App root: MRUK + occluder, calibration, placement, input, flow, UI, sim rig, character.</summary>
    public static class SceneComposer
    {
        public const string OccluderMatPath = "Assets/Materials/DepthOnlyOccluder.mat";
        public const string OccluderDebugMatPath = "Assets/Materials/OccluderDebug.mat";
        public const string GiantMatPath = "Assets/Materials/GiantPlaceholder.mat";
        public const string GiantOcclusionMatPath = "Assets/Materials/GiantOcclusion.mat";
        public const string InputAssetPath = "Assets/Input/GiantControls.inputactions";
        public const string SimRoomPrefab = "Packages/com.meta.xr.mrutilitykit/Core/Rooms/Prefabs/Bedroom/Bedroom00.prefab";

        public static void Compose(GameObject app, GameObject cameraRig)
        {
            // OVRCameraRig only resolves its anchor properties at runtime; find them by path in edit mode.
            var head = cameraRig.transform.Find("TrackingSpace/CenterEyeAnchor");
            var rightHand = cameraRig.transform.Find("TrackingSpace/RightHandAnchor");
            if (head == null || rightHand == null) throw new Exception("OVRCameraRig anchors not found");

            // --- MRUK + occluder + depth
            var mrukGo = new GameObject("MRUK");
            var mruk = mrukGo.AddComponent<Meta.XR.MRUtilityKit.MRUK>();
            if (mruk.SceneSettings == null) mruk.SceneSettings = new Meta.XR.MRUtilityKit.MRUK.MRUKSettings();
            // WindowCalibration owns the load: device scan on hardware, the sim prefab in the editor. Never let MRUK
            // auto-fall back to a prefab on device (a phantom bedroom would occlude the real city).
            mruk.SceneSettings.DataSource = Meta.XR.MRUtilityKit.MRUK.SceneDataSource.Device;
            mruk.SceneSettings.LoadSceneOnStartup = false;
            mruk.SceneSettings.RoomIndex = 0;
            var simRoom = AssetDatabase.LoadAssetAtPath<GameObject>(SimRoomPrefab);
            if (simRoom == null) throw new Exception($"missing {SimRoomPrefab}");
            mruk.SceneSettings.RoomPrefabs = new[] { simRoom };

            var effect = mrukGo.AddComponent<Meta.XR.MRUtilityKit.EffectMesh>();
            effect.SpawnOnStart = Meta.XR.MRUtilityKit.MRUK.RoomFilter.None; // WindowCalibration builds it
            effect.Labels = Meta.XR.MRUtilityKit.MRUKAnchor.SceneLabels.WALL_FACE | Meta.XR.MRUtilityKit.MRUKAnchor.SceneLabels.CEILING | Meta.XR.MRUtilityKit.MRUKAnchor.SceneLabels.FLOOR;
            effect.CutHoles = Meta.XR.MRUtilityKit.MRUKAnchor.SceneLabels.WINDOW_FRAME | Meta.XR.MRUtilityKit.MRUKAnchor.SceneLabels.DOOR_FRAME;
            effect.MeshMaterial = Material(OccluderMatPath, "Godzilla/DepthOnlyOccluder", Color.white);

            var depthGo = new GameObject("EnvironmentDepth");
            var depth = depthGo.AddComponent<Meta.XR.EnvironmentDepth.EnvironmentDepthManager>();
            depth.enabled = false;
            depthGo.SetActive(false); // its Awake logs an error without an XR provider (editor); WindowCalibration activates it on demand

            // --- character: rigged humanoid if present, else capsule placeholder
            var character = BuildRiggedCharacter() ?? BuildPlaceholderCharacter();
            var giantAnim = character.GetComponent<Godzilla.Character.GiantAnimationController>();

            // --- components on App
            var city = app.AddComponent<Godzilla.Core.CitySpaceManager>();
            var window = app.AddComponent<Godzilla.Core.WindowCalibration>();
            var debugMat = Material(OccluderDebugMatPath, "Universal Render Pipeline/Unlit", new Color(0f, 1f, 1f, 0.3f), transparent: true);
            var giantLit = AssetDatabase.LoadAssetAtPath<Material>(GiantMatPath);
            var giantOcclusion = Material(GiantOcclusionMatPath, "EnvironmentDepth/OcclusionLit", Color.white);
            if (giantOcclusion.HasProperty("_Color")) giantOcclusion.SetColor("_Color", new Color(0.85f, 0.85f, 0.9f));
            window.Configure(city, effect, depth, effect.MeshMaterial, debugMat, head, simRoom,
                character.GetComponentsInChildren<Renderer>(true), giantLit, giantOcclusion);

            var placement = app.AddComponent<Godzilla.Placement.CharacterPlacement>();
            placement.Configure(city, character.transform, 1.8f);

            var input = app.AddComponent<Godzilla.Input.GiantInput>();
            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputAssetPath);
            if (actions == null) throw new Exception($"missing {InputAssetPath}");
            input.Configure(actions, head, rightHand);

            var flow = app.AddComponent<Godzilla.App.AppFlow>();
            var hudGo = new GameObject("PlayerHud");
            var hud = hudGo.AddComponent<Godzilla.UI.PlayerHud>();
            hud.Configure(head, flow);
            var devGo = new GameObject("DevPanel");
            var dev = devGo.AddComponent<Godzilla.UI.DevPanel>();
            dev.Configure(head, window, placement, flow);
            flow.Configure(input, window, placement, hud, dev);

            var sim = app.AddComponent<Godzilla.Sim.EditorSimRig>();
            sim.Configure(head, input);

            if (giantAnim != null)
            {
                var giantCtrl = app.AddComponent<Godzilla.Character.GiantCharacterController>();
                giantCtrl.Configure(flow, placement, giantAnim);
                giantAnim.Configure(character.GetComponentInChildren<Animator>(), giantCtrl);
                dev.ConfigureGiant(giantCtrl, giantAnim);
            }
        }

        static void Log(string msg) => Debug.Log($"[Bootstrap] {msg}");
        public const string RiggedBodyPath = "Assets/Characters/Placeholder/RoomAssistantChar.fbx";
        public const string YBotPath = "Assets/Characters/Placeholder/YBot.fbx";

        /// <summary>Instantiates the Humanoid body (Y Bot preferred) with the generated Animator Controller. Null when no body/controller exists.</summary>
        static GameObject BuildRiggedCharacter()
        {
            var bodyPath = AssetDatabase.LoadAssetAtPath<GameObject>(YBotPath) != null ? YBotPath : RiggedBodyPath;
            var body = AssetDatabase.LoadAssetAtPath<GameObject>(bodyPath);
            if (body == null) { Log("no rigged body found; using capsule placeholder"); return null; }

            GiantAnimatorBuilder.Build();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(GiantAnimatorBuilder.ControllerPath);
            if (controller == null) { Log("animator controller missing; using capsule placeholder"); return null; }

            var root = new GameObject("Giant");
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(body);
            inst.name = "Body";
            inst.transform.SetParent(root.transform, false);
            // normalise to a 1.8 m tall body with feet at the root origin
            var renderers = inst.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                float h = Mathf.Max(0.01f, b.size.y);
                float s = 1.8f / h;
                inst.transform.localScale = Vector3.one * s;
                inst.transform.localPosition = new Vector3(-b.center.x * s, -b.min.y * s, -b.center.z * s);
                Log($"rigged body {System.IO.Path.GetFileName(bodyPath)} height={h:0.00} m → scaled {s:0.000}");
            }
            var mat = Material(GiantMatPath, "Universal Render Pipeline/Lit", new Color(0.85f, 0.85f, 0.9f));
            mat.SetColor("_EmissionColor", Color.white * 0.6f);
            mat.EnableKeyword("_EMISSION");
            foreach (var r in renderers) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();

            var animator = inst.GetComponent<Animator>() ?? inst.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // On the root so Compose can fetch it with GetComponent on the character root.
            root.AddComponent<Godzilla.Character.GiantAnimationController>();
            return root;
        }

        static GameObject BuildPlaceholderCharacter()
        {
            // 1.8 m tall stack: body capsule + head sphere + two arm capsules. Root pivot at the feet.
            var root = new GameObject("Giant");
            var mat = Material(GiantMatPath, "Universal Render Pipeline/Lit", new Color(0.85f, 0.85f, 0.9f));
            mat.SetColor("_EmissionColor", Color.white * 0.6f);
            mat.EnableKeyword("_EMISSION");

            Part(root, PrimitiveType.Capsule, "Body", new Vector3(0, 0.95f, 0), new Vector3(0.5f, 0.55f, 0.3f), mat);
            Part(root, PrimitiveType.Sphere, "Head", new Vector3(0, 1.62f, 0), new Vector3(0.32f, 0.32f, 0.32f), mat);
            Part(root, PrimitiveType.Capsule, "ArmL", new Vector3(-0.38f, 1.05f, 0), new Vector3(0.14f, 0.35f, 0.14f), mat);
            Part(root, PrimitiveType.Capsule, "ArmR", new Vector3(0.38f, 1.05f, 0), new Vector3(0.14f, 0.35f, 0.14f), mat);
            Part(root, PrimitiveType.Capsule, "LegL", new Vector3(-0.13f, 0.4f, 0), new Vector3(0.18f, 0.42f, 0.18f), mat);
            Part(root, PrimitiveType.Capsule, "LegR", new Vector3(0.13f, 0.4f, 0), new Vector3(0.18f, 0.42f, 0.18f), mat);
            return root;
        }

        static void Part(GameObject root, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var p = GameObject.CreatePrimitive(type);
            UnityEngine.Object.DestroyImmediate(p.GetComponent<Collider>());
            p.name = name;
            p.transform.SetParent(root.transform, false);
            p.transform.localPosition = pos;
            p.transform.localScale = scale;
            p.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static Material Material(string path, string shaderName, Color color, bool transparent = false)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new Exception($"shader not found: {shaderName}");
            if (mat == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f); // URP transparent
                mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 1);
                mat.renderQueue = 2999;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
