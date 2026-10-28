using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace Godzilla.Tests
{
    public class InputActionsAssetTests
    {
        const string AssetPath = "Assets/Input/GiantControls.inputactions";

        static InputActionAsset Load()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            Assert.IsNotNull(asset, $"missing {AssetPath}");
            return asset;
        }

        [Test]
        public void HasGiantMapWithAllActions()
        {
            var map = Load().FindActionMap("Giant", throwIfNotFound: true);
            foreach (var name in new[] { "Move", "Turn", "Jump", "Punch", "Kick", "Roar", "Run", "Menu", "PlaceDrag", "Confirm" })
                Assert.IsNotNull(map.FindAction(name), $"action {name} missing");
        }

        [Test]
        public void Jump_BoundToRightPrimaryButtonAndSpace()
        {
            var jump = Load().FindActionMap("Giant").FindAction("Jump");
            var paths = jump.bindings.Select(b => b.path).ToArray();
            Assert.IsTrue(paths.Any(p => p.Contains("{RightHand}/primaryButton")), string.Join(",", paths));
            Assert.IsTrue(paths.Any(p => p == "<Keyboard>/space"), string.Join(",", paths));
        }

        [Test]
        public void Move_BoundToLeftStickAndWasd()
        {
            var move = Load().FindActionMap("Giant").FindAction("Move");
            var paths = move.bindings.Select(b => b.path).ToArray();
            Assert.IsTrue(paths.Any(p => p.Contains("{LeftHand}/primary2DAxis")), string.Join(",", paths));
            Assert.IsTrue(paths.Contains("<Keyboard>/w") && paths.Contains("<Keyboard>/a") && paths.Contains("<Keyboard>/s") && paths.Contains("<Keyboard>/d"), string.Join(",", paths));
        }

        [Test]
        public void Run_BoundToLeftTriggerAndShift()
        {
            var run = Load().FindActionMap("Giant").FindAction("Run");
            var paths = run.bindings.Select(b => b.path).ToArray();
            Assert.IsTrue(paths.Any(p => p.Contains("{LeftHand}/trigger")), string.Join(",", paths));
            Assert.IsTrue(paths.Contains("<Keyboard>/leftShift"), string.Join(",", paths));
        }
    }
}
