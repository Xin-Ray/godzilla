using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;

namespace Godzilla.Editor
{
    /// <summary>
    /// Headless Android build:
    ///   Unity.exe -batchmode -quit -nographics -buildTarget Android -projectPath . -executeMethod Godzilla.Editor.QuestBatchBuild.Build
    /// Forces foveation + subsampled layout OFF (myroom lesson) before building.
    /// </summary>
    public static class QuestBatchBuild
    {
        public const string ApkPath = "Builds/Godzilla.apk";

        [MenuItem("Godzilla/Build Quest APK")]
        public static void Build()
        {
            try
            {
                FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
                Disable("com.meta.openxr.feature.foveation");
                Disable("com.meta.openxr.feature.subsampledLayout");
                AssetDatabase.SaveAssets();

                Directory.CreateDirectory("Builds");
                var opts = new BuildPlayerOptions
                {
                    scenes = new[] { ProjectBootstrap.ScenePath },
                    locationPathName = ApkPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None,
                };
                var report = BuildPipeline.BuildPlayer(opts);
                var s = report.summary;
                Debug.Log($"[Build] result={s.result} errors={s.totalErrors} warnings={s.totalWarnings} size={s.totalSize / (1024 * 1024)}MB time={s.totalTime}");
                if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Build] EXCEPTION: {e}");
                if (Application.isBatchMode) EditorApplication.Exit(2);
                throw;
            }
        }

        static void Disable(string featureId)
        {
            var f = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, featureId);
            if (f == null) { Debug.Log($"[Build] feature {featureId} not found"); return; }
            if (f.enabled) { f.enabled = false; EditorUtility.SetDirty(f); Debug.Log($"[Build] forced OFF: {featureId}"); }
        }
    }
}
