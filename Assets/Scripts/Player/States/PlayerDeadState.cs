using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 死亡状态。锁死输入、清空速度、关掉攻击判定，
    /// 之后由 PlayerHealth 延迟一小段时间触发失败结算。
    /// </summary>
    public class PlayerDeadState : PlayerStateBase
    {
        public PlayerDeadState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            C.Input.Enabled = false;
            C.Input.Sample();

            C.Animator.ResetTrigger(AnimParams.Attack);
            C.Animator.ResetTrigger(AnimParams.Hurt);
            C.Animator.SetBool(AnimParams.Grounded, true);
            C.Animator.SetFloat(AnimParams.Speed, 0f);
            C.Animator.SetTrigger(AnimParams.Die);

            C.Body.velocity = Vector2.zero;
            C.Body.gravityScale = 0f;
            C.Body.bodyType = RigidbodyType2D.Kinematic;

            if (C.Attack != null) C.Attack.enabled = false;
            C.Visual?.SetBlinking(false);
        }

        public override void Tick(float deltaTime) { }
        public override void FixedTick(float deltaTime) { }
    }
}
