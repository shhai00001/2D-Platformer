namespace Platformer
{
    /// <summary>受击。硬直期间不移动、不攻击，只让击退自然衰减。</summary>
    public class EnemyHurtState : EnemyStateBase
    {
        private float _timer;

        public EnemyHurtState(EnemyController controller) : base(controller) { }

        public override void Enter()
        {
            _timer = C.HurtDuration;

            C.Animator.ResetTrigger(AnimParams.Attack);
            C.Animator.SetTrigger(AnimParams.Hurt);

            C.ApplyKnockback(C.PendingKnockback);
            C.Visual?.Flash();
        }

        public override void Tick(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f) return;

            C.ChangeState(C.HasTarget ? (IState)C.ChaseState : C.PatrolState);
        }

        public override void FixedTick(float deltaTime)
        {
            C.DampKnockback(deltaTime);
        }
    }
}
