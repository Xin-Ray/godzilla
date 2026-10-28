using UnityEditor;
using UnityEngine;

namespace Godzilla.Editor
{
    /// <summary>
    /// Import rules for everything under Assets/Characters/: Humanoid rig; locomotion/idle clips loop;
    /// root XZ motion is baked into the pose (clips play "in place") because GiantMover owns displacement.
    /// </summary>
    public class CharacterImportSettings : AssetPostprocessor
    {
        const string Root = "Assets/Characters/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(Root + "Placeholder/Anim/")) return;
            var importer = (ModelImporter)assetImporter;
            var name = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            bool loop = name.Contains("idle") || name.Contains("walk") || name.Contains("run");

            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;
            foreach (var c in clips)
            {
                c.loopTime = loop;
                c.loopPose = loop;
                c.lockRootRotation = true;
                c.lockRootHeightY = true;   // feet stay on the ground; jump height comes from GiantMover
                c.lockRootPositionXZ = true; // bake XZ: in-place playback
                c.keepOriginalOrientation = true;
                c.keepOriginalPositionY = true;
                c.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
        }
    }
}
