using UnityEngine;

namespace Platformer
{
    /// <summary>上升段。垂直速度掉到 0 以下就切到 Fall。</summary>
    public class PlayerJumpState : PlayerStateBase
    {
        public PlayerJumpState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            C.DoJump();
            C.Animator.SetBool(AnimParams.Grounded, false);
            C.Animator.SetFloat(AnimParams.VerticalSpeed, C.Velocity.y);
        }

        public override void Tick(float deltaTime)
        {
            C.Animator.SetFloat(AnimParams.Speed, Mathf.Abs(C.Velocity.x));
            C.Animator.SetFloat(AnimParams.VerticalSpeed, C.Velocity.y);

            // 空中可以出招
            if (C.Input.AttackPressed)
            {
                C.ChangeState(C.AttackState);
                return;
            }

            if (C.Velocity.y <= 0.01f)
            {
                C.ChangeState(C.FallState);
            }
        }

        public override void FixedTick(float deltaTime)
        {
            ApplyHorizontal(deltaTime);
        }
    }
}
