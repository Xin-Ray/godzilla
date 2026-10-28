using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Godzilla.Tests
{
    /// <summary>Locomotion clips must play in place (root XZ baked) — GiantMover owns displacement, so any clip root motion would double it.</summary>
    public class ClipRootMotionTests
    {
        const string AnimDir = "Assets/Characters/Placeholder/Anim";

        static AnimationClip Load(string name)
        {
            var path = Directory.GetFiles(AnimDir).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == name && !f.EndsWith(".meta"));
            Assert.IsNotNull(path, $"missing {name} in {AnimDir}");
            var clip = AssetDatabase.LoadAllAssetsAtPath(path.Replace('\\', '/')).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
            Assert.IsNotNull(clip, $"no AnimationClip inside {path}");
            return clip;
        }

        [TestCase("Anim_walk")]
        [TestCase("Anim_idle2")]
        public void LocomotionClip_IsHumanoid_LoopsAndHasNoRootXZMotion(string name)
        {
            var clip = Load(name);
            Assert.IsTrue(clip.isHumanMotion, "imported as Humanoid");
            Assert.IsTrue(clip.isLooping, "locomotion clips loop");
            Assert.IsFalse(clip.hasRootCurves && !clip.hasGenericRootTransform && RootXZDisplacement(clip) > 0.05f, "root XZ baked into pose");
            Assert.Less(RootXZDisplacement(clip), 0.05f, "root XZ displacement over the clip ≈ 0 (in place)");
        }

        [TestCase("Anim_jump")]
        [TestCase("Anim_kick")]
        public void ActionClip_IsHumanoid_DoesNotLoop(string name)
        {
            var clip = Load(name);
            Assert.IsTrue(clip.isHumanMotion);
            Assert.IsFalse(clip.isLooping, "one-shot actions do not loop");
        }

        /// <summary>Horizontal distance the root moves from the first to the last frame, sampled on a temporary humanoid-less GameObject.</summary>
        static float RootXZDisplacement(AnimationClip clip)
        {
            var go = new GameObject("clip_probe");
            try
            {
                var start = SampleRoot(go, clip, 0f);
                var end = SampleRoot(go, clip, clip.length);
                return Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(end.x, 0, end.z));
            }
            finally { Object.DestroyImmediate(go); }
        }

        static Vector3 SampleRoot(GameObject go, AnimationClip clip, float t)
        {
            go.transform.position = Vector3.zero;
            clip.SampleAnimation(go, t);
            return go.transform.position;
        }
    }
}
