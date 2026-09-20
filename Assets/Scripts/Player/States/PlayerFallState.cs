using UnityEngine;

namespace Platformer
{
    /// <summary>下落段。落地、出招或二段跳都会离开这个状态。</summary>
    public class PlayerFallState : PlayerStateBase
    {
        public PlayerFallState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            C.Animator.SetBool(AnimParams.Grounded, false);
            C.Animator.SetFloat(AnimParams.VerticalSpeed, C.Velocity.y);
        }

        public override void Tick(float deltaTime)
        {
            C.Animator.SetFloat(AnimParams.Speed, Mathf.Abs(C.Velocity.x));
            C.Animator.SetFloat(AnimParams.VerticalSpeed, C.Velocity.y);

            if (C.IsGrounded)
            {
                GoToGroundedLocomotion();
                return;
            }

            if (C.Input.AttackPressed)
            {
                C.ChangeState(C.AttackState);
                return;
            }

            // maxAirJumps 为 0 时这里永远不成立，就是普通的一段跳
            if (C.ConsumeJumpIfAvailable())
            {
                C.ChangeState(C.JumpState);
            }
        }

        public override void FixedTick(float deltaTime)
        {
            ApplyHorizontal(deltaTime);
        }
    }
}
