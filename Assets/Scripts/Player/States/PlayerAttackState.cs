using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 攻击状态。进入时触发 Animator 的 Attack 触发器并重置判定盒，
    /// 退出条件由攻击动画末尾的 Animation Event 给出（PlayerAttack.Finished），
    /// 另有超时兜底，防止动画事件丢失导致角色卡死。
    /// </summary>
    public class PlayerAttackState : PlayerStateBase
    {
        private float _elapsed;

        public PlayerAttackState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            _elapsed = 0f;
            C.Attack.BeginAttack(C.Facing);
            C.Animator.SetTrigger(AnimParams.Attack);
            C.Animator.SetFloat(AnimParams.Speed, 0f);
        }

        public override void Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            bool finished = C.Attack.Finished || _elapsed >= C.AttackTimeout;
            if (!finished) return;

            if (!C.IsGrounded)
            {
                C.ChangeState(C.Velocity.y > 0.01f ? (IState)C.JumpState : C.FallState);
                return;
            }
            GoToGroundedLocomotion();
        }

        public override void FixedTick(float deltaTime)
        {
            // 地面攻击时刹停，空中攻击保留大部分惯性，只做少量修正
            if (C.IsGrounded) C.Decelerate(deltaTime, 1.2f);
            else ApplyHorizontal(deltaTime, 0.35f);
        }
    }
}
