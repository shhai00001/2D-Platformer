using UnityEngine;

namespace Platformer
{
    /// <summary>移动：地面上的水平跑动。转向在 FixedTick 里处理。</summary>
    public class PlayerMoveState : PlayerStateBase
    {
        public PlayerMoveState(PlayerController controller) : base(controller) { }

        public override void Tick(float deltaTime)
        {
            C.Animator.SetFloat(AnimParams.Speed, Mathf.Abs(C.Velocity.x));

            if (!C.IsGrounded)
            {
                C.ChangeState(C.FallState);
                return;
            }

            if (C.Input.AttackPressed)
            {
                C.ChangeState(C.AttackState);
                return;
            }

            if (C.ConsumeJumpIfAvailable())
            {
                C.ChangeState(C.JumpState);
                return;
            }

            if (!C.Input.HasMoveInput)
            {
                C.ChangeState(C.IdleState);
            }
        }

        public override void FixedTick(float deltaTime)
        {
            C.SetFacing(C.Input.MoveDirection);
            C.MoveHorizontal(C.Input.MoveDirection, deltaTime);
        }
    }
}
