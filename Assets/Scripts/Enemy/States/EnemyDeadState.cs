using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 死亡。停止一切行为并锁死刚体，剩下的销毁流程交给 EnemyHealth。
    /// EnemyController.Update 在 IsDead 时会直接返回，所以状态机不会再被驱动。
    /// </summary>
    public class EnemyDeadState : EnemyStateBase
    {
        public EnemyDeadState(EnemyController controller) : base(controller) { }

        public override void Enter()
        {
            C.Animator.ResetTrigger(AnimParams.Attack);
            C.Animator.ResetTrigger(AnimParams.Hurt);
            C.Animator.SetFloat(AnimParams.Speed, 0f);
            C.Animator.SetTrigger(AnimParams.Die);

            C.Body.velocity = Vector2.zero;
            C.Body.gravityScale = 0f;
            C.Body.bodyType = RigidbodyType2D.Kinematic;

            if (C.Attack != null) C.Attack.enabled = false;
            C.Visual?.SetBlinking(false);

            if (GameManager.Instance != null) GameManager.Instance.RegisterKill();
        }

        public override void Tick(float deltaTime) { }
        public override void FixedTick(float deltaTime) { }
    }
}
