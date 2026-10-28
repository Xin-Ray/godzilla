using UnityEngine;

namespace Godzilla.Character
{
    /// <summary>
    /// Thin Animator bridge. Parameter and state names are shared with GiantAnimatorBuilder (editor).
    /// One-shot action completion is detected by polling the animator state each frame (entered an action state,
    /// then left it) rather than a StateMachineBehaviour, which batch-mode controller generation cannot attach.
    /// </summary>
    public class GiantAnimationController : MonoBehaviour
    {
        public const string ParamSpeed = "Speed";
        public const string ParamGrounded = "Grounded";
        public const string TrigJump = "Jump";
        public const string TrigPunch = "Punch";
        public const string TrigKick = "Kick";
        public const string TrigRoar = "Roar";
        public const string StateLocomotion = "Locomotion";

        [SerializeField] Animator animator;
        [SerializeField] GiantCharacterController controller;

        public string CurrentStateName { get; private set; } = "-";
        /// <summary>Upper bound for a one-shot action at animTimeScale 1; scaled by 1/animTimeScale at runtime. Safety net if the exit callback is missed.</summary>
        public float MaxActionSeconds = 4f;
        float _lockDeadline = -1f;
        bool _wasInAction;
        static readonly int HashPunch = Animator.StringToHash("Punch"), HashKick = Animator.StringToHash("Kick"), HashRoar = Animator.StringToHash("Roar");
        static bool IsAction(AnimatorStateInfo s) => s.shortNameHash == HashPunch || s.shortNameHash == HashKick || s.shortNameHash == HashRoar;

        public void Configure(Animator a, GiantCharacterController c) { animator = a; controller = c; }

        public void Apply(float speed01, bool grounded, GiantAction action, float animTimeScale)
        {
            if (animator == null) return;
            animator.speed = animTimeScale;
            animator.SetFloat(ParamSpeed, speed01);
            animator.SetBool(ParamGrounded, grounded);
            switch (action)
            {
                case GiantAction.Jump: animator.SetTrigger(TrigJump); break;
                case GiantAction.Punch: animator.SetTrigger(TrigPunch); break;
                case GiantAction.Kick: animator.SetTrigger(TrigKick); break;
                case GiantAction.Roar: animator.SetTrigger(TrigRoar); break;
            }
            if (action == GiantAction.Punch || action == GiantAction.Kick || action == GiantAction.Roar)
                _lockDeadline = Time.time + MaxActionSeconds / Mathf.Max(0.05f, animTimeScale);
        }

        /// <summary>Releases the locomotion lock (action state exited, or the safety timeout fired).</summary>
        public void OnActionFinished()
        {
            _lockDeadline = -1f;
            controller?.Logic.OnActionFinished();
        }

        void Update()
        {
            if (controller != null && controller.Logic.Locked && _lockDeadline > 0f && Time.time > _lockDeadline)
            {
                Debug.LogWarning("[Giant] action lock timed out without a state exit — releasing");
                OnActionFinished();
            }
            if (animator == null) return;
            var info = animator.GetCurrentAnimatorStateInfo(0);
            bool inTransition = animator.IsInTransition(0);
            bool actionNow = IsAction(info) || (inTransition && IsAction(animator.GetNextAnimatorStateInfo(0)));
            if (_wasInAction && !actionNow && controller != null && controller.Logic.Locked)
            {
                Debug.Log("[Giant] action state exited → unlock");
                OnActionFinished();
            }
            _wasInAction = actionNow;
            CurrentStateName = info.IsName(StateLocomotion) ? (animator.GetFloat(ParamSpeed) < 0.25f ? "Idle" : animator.GetFloat(ParamSpeed) < 0.75f ? "Walk" : "Run")
                : info.IsName("Jump") ? "Jump" : info.IsName("Punch") ? "Punch" : info.IsName("Kick") ? "Kick" : info.IsName("Roar") ? "Roar" : "?";
        }
    }
}
