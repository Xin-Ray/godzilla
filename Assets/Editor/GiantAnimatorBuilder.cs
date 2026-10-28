using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Godzilla.Character;

namespace Godzilla.Editor
{
    /// <summary>
    /// Generates Assets/Characters/Placeholder/Giant.controller from the clips in Assets/Characters/Placeholder/Anim.
    /// Base layer: Locomotion blend tree (Idle 0 / Walk 0.5 / Run 1) → Jump → one-shot Punch/Kick/Roar (exit detected by polling in GiantAnimationController).
    /// Missing clips fall back (Run→Walk@1.6x, Punch→Kick, Roar→Idle) so the controller always builds; re-run after adding clips.
    /// </summary>
    public static class GiantAnimatorBuilder
    {
        public const string ControllerPath = "Assets/Characters/Placeholder/Giant.controller";
        public const string AnimDir = "Assets/Characters/Placeholder/Anim";

        [MenuItem("Godzilla/Build Giant Animator")]
        public static void Build()
        {
            var idle = Clip("Anim_idle2", "Anim_idle");
            var walk = Clip("Anim_walk");
            var run = Clip("Anim_run");
            var jump = Clip("Anim_jump");
            var punch = Clip("Anim_punch");
            var kick = Clip("Anim_kick");
            var roar = Clip("Anim_roar");
            if (idle == null || walk == null || jump == null || kick == null)
                throw new System.Exception("GiantAnimatorBuilder: idle/walk/jump/kick clips are required");
            bool runFallback = run == null; if (runFallback) run = walk;
            if (punch == null) punch = kick;
            if (roar == null) roar = idle;

            if (File.Exists(ControllerPath)) AssetDatabase.DeleteAsset(ControllerPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ctrl.AddParameter(GiantAnimationController.ParamSpeed, AnimatorControllerParameterType.Float);
            ctrl.AddParameter(GiantAnimationController.ParamGrounded, AnimatorControllerParameterType.Bool);
            foreach (var t in new[] { GiantAnimationController.TrigJump, GiantAnimationController.TrigPunch, GiantAnimationController.TrigKick, GiantAnimationController.TrigRoar })
                ctrl.AddParameter(t, AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;

            // Locomotion blend tree
            var tree = new BlendTree { name = "LocomotionTree", blendType = BlendTreeType.Simple1D, blendParameter = GiantAnimationController.ParamSpeed, useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, ctrl);
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, 0.5f);
            tree.AddChild(run, 1f);
            if (runFallback)
            {
                var children = tree.children; children[2].timeScale = 1.6f; tree.children = children;
            }
            var loco = sm.AddState(GiantAnimationController.StateLocomotion, new Vector3(300, 0));
            loco.motion = tree;
            sm.defaultState = loco;

            // Jump
            var jumpState = sm.AddState("Jump", new Vector3(300, 120));
            jumpState.motion = jump;
            var toJump = sm.AddAnyStateTransition(jumpState);
            toJump.AddCondition(AnimatorConditionMode.If, 0, GiantAnimationController.TrigJump);
            toJump.duration = 0.1f; toJump.hasExitTime = false; toJump.canTransitionToSelf = false;
            var jumpBack = jumpState.AddTransition(loco);
            jumpBack.hasExitTime = true; jumpBack.exitTime = 0.9f; jumpBack.duration = 0.15f;

            // One-shot actions
            OneShot(sm, loco, "Punch", punch, GiantAnimationController.TrigPunch, new Vector3(300, 240));
            OneShot(sm, loco, "Kick", kick, GiantAnimationController.TrigKick, new Vector3(300, 300));
            OneShot(sm, loco, "Roar", roar, GiantAnimationController.TrigRoar, new Vector3(300, 360));

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Build] animator built: run={(runFallback ? "walk@1.6x" : "run")} punch={punch.name} roar={roar.name}");
        }

        static void OneShot(AnimatorStateMachine sm, AnimatorState back, string name, AnimationClip clip, string trigger, Vector3 pos)
        {
            var st = sm.AddState(name, pos);
            st.motion = clip;
            var t = sm.AddAnyStateTransition(st);
            t.AddCondition(AnimatorConditionMode.If, 0, trigger);
            t.duration = 0.15f; t.hasExitTime = false; t.canTransitionToSelf = false;
            var b = st.AddTransition(back);
            b.hasExitTime = true; b.exitTime = 0.95f; b.duration = 0.15f;
        }

        static AnimationClip Clip(params string[] names)
        {
            foreach (var n in names)
            {
                var path = Directory.GetFiles(AnimDir).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(n, System.StringComparison.OrdinalIgnoreCase) && !f.EndsWith(".meta"));
                if (path == null) continue;
                var clips = AssetDatabase.LoadAllAssetsAtPath(path.Replace('\\', '/')).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();
                if (clips.Length > 0) return clips[0];
            }
            return null;
        }
    }
}
