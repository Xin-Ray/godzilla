using NUnit.Framework;
using UnityEngine;
using Godzilla.Character;
using Godzilla.Input;

namespace Godzilla.Tests
{
    public class GiantAnimLogicTests
    {
        static InputFrame Walk() => new InputFrame { Move = new Vector2(0, 1f) };
        static InputFrame RunFwd() => new InputFrame { Move = new Vector2(0, 1f), Run = true };

        [Test]
        public void Speed_RunIsOne_WalkHalf_IdleZero()
        {
            var l = new GiantAnimLogic();
            for (int k = 0; k < 200; k++) l.Tick(Walk(), grounded: true, dt: 0.016f);
            Assert.AreEqual(0.5f, l.Speed01, 0.01f);
            for (int k = 0; k < 200; k++) l.Tick(RunFwd(), true, 0.016f);
            Assert.AreEqual(1f, l.Speed01, 0.01f);
            for (int k = 0; k < 200; k++) l.Tick(new InputFrame(), true, 0.016f);
            Assert.AreEqual(0f, l.Speed01, 0.01f);
        }

        [Test]
        public void Speed_Smooths_NotInstant()
        {
            var l = new GiantAnimLogic();
            l.Tick(RunFwd(), true, 0.016f);
            Assert.Less(l.Speed01, 0.5f, "one frame does not jump to full run");
            Assert.Greater(l.Speed01, 0f);
        }

        [Test]
        public void Punch_SetsPendingAndLocks()
        {
            var l = new GiantAnimLogic();
            l.Tick(new InputFrame { PunchPressed = true }, true, 0.016f);
            Assert.AreEqual(GiantAction.Punch, l.Pending);
            Assert.IsTrue(l.Locked);
            Assert.AreEqual(GiantAction.Punch, l.ConsumePending());
        }

        [Test]
        public void ConsumePending_ReturnsOnce()
        {
            var l = new GiantAnimLogic();
            l.Tick(new InputFrame { KickPressed = true }, true, 0.016f);
            Assert.AreEqual(GiantAction.Kick, l.ConsumePending());
            Assert.AreEqual(GiantAction.None, l.ConsumePending());
        }

        [Test]
        public void Attack_WhileLocked_Ignored()
        {
            var l = new GiantAnimLogic();
            l.Tick(new InputFrame { PunchPressed = true }, true, 0.016f);
            l.ConsumePending();
            l.Tick(new InputFrame { KickPressed = true, RoarPressed = true, PunchPressed = true }, true, 0.016f);
            Assert.AreEqual(GiantAction.None, l.ConsumePending(), "mashing during an action queues nothing");
            Assert.IsTrue(l.Locked);
        }

        [Test]
        public void Lock_ReleasedOnce_ThenAttacksAllowedAgain()
        {
            var l = new GiantAnimLogic();
            l.Tick(new InputFrame { RoarPressed = true }, true, 0.016f);
            l.ConsumePending();
            l.OnActionFinished();
            Assert.IsFalse(l.Locked);
            l.OnActionFinished(); // idempotent
            Assert.IsFalse(l.Locked);
            l.Tick(new InputFrame { PunchPressed = true }, true, 0.016f);
            Assert.AreEqual(GiantAction.Punch, l.ConsumePending());
        }

        [Test]
        public void Jump_RequiresGrounded_AndNotLocked()
        {
            var l = new GiantAnimLogic();
            l.Tick(new InputFrame { JumpPressed = true }, grounded: false, dt: 0.016f);
            Assert.AreEqual(GiantAction.None, l.ConsumePending(), "no jump in the air");
            l.Tick(new InputFrame { JumpPressed = true }, grounded: true, dt: 0.016f);
            Assert.AreEqual(GiantAction.Jump, l.ConsumePending());
            Assert.IsFalse(l.Locked, "jump does not lock locomotion");

            l.Tick(new InputFrame { PunchPressed = true }, true, 0.016f);
            l.ConsumePending();
            l.Tick(new InputFrame { JumpPressed = true }, true, 0.016f);
            Assert.AreEqual(GiantAction.None, l.ConsumePending(), "no jump while an attack plays");
        }

        [Test]
        public void MoveInput_IgnoredWhileLocked()
        {
            var l = new GiantAnimLogic();
            l.Tick(new InputFrame { PunchPressed = true }, true, 0.016f);
            for (int k = 0; k < 200; k++) l.Tick(RunFwd(), true, 0.016f);
            Assert.AreEqual(0f, l.Speed01, 0.01f, "locked: speed decays to idle even with stick held");
            Assert.AreEqual(Vector2.zero, l.MoveAllowed(RunFwd()));
        }
    }
}
