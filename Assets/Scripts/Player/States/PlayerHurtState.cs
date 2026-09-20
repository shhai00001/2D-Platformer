using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 受击状态。进入时注入击退速度并播放受击动画，
    /// 硬直时间内不接受任何输入，落地或计时结束后才恢复操控。
    /// </summary>
    public class PlayerHurtState : PlayerStateBase
    {
        private float _timer;

        public PlayerHurtState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            _timer = C.HurtDuration;

            C.Animator.ResetTrigger(AnimParams.Attack);
            C.Animator.SetTrigger(AnimParams.Hurt);
            C.Animator.SetFloat(AnimParams.Speed, 0f);

            C.ApplyKnockback(C.PendingKnockback);
            C.Visual?.Flash();
        }

        public override void Tick(float deltaTime)
        {
            _timer -= deltaTime;
            C.Animator.SetFloat(AnimParams.VerticalSpeed, C.Velocity.y);
            if (_timer > 0f) return;

            if (C.IsGrounded)
            {
                GoToGroundedLocomotion();
            }
            else if (C.Velocity.y <= 0.01f)
            {
                C.ChangeState(C.FallState);
            }
        }

        public override void FixedTick(float deltaTime)
        {
            // 硬直期间只做水平击退衰减，不接受玩家输入
            C.DampKnockback(deltaTime);
        }
    }
}
