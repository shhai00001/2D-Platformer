namespace Platformer
{
    /// <summary>
    /// 攻击。进入的瞬间就调用 MarkAttackUsed() 开始冷却计时 ——
    /// 因此无论攻击动画播多久、被打断几次，"下一次能出手的时间"都是确定的，
    /// 从根上杜绝了连续攻击。
    ///
    /// 退出条件用攻击动画末尾的 Animation Event，超时兜底。
    /// </summary>
    public class EnemyAttackState : EnemyStateBase
    {
        private float _elapsed;

        public EnemyAttackState(EnemyController controller) : base(controller) { }

        public override void Enter()
        {
            _elapsed = 0f;

            C.StopMoving();
            C.MarkAttackUsed();
            C.Attack.BeginAttack(C.Facing);
            C.Animator.SetTrigger(AnimParams.Attack);
        }

        public override void Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            bool finished = C.Attack.Finished || _elapsed >= C.AttackTimeout;
            if (!finished) return;

            // 打完看还追不追得上：还在锁定范围内就继续追，否则回去巡逻
            C.ChangeState(C.HasTarget ? (IState)C.ChaseState : C.PatrolState);
        }

        public override void FixedTick(float deltaTime)
        {
            C.StopMoving();
        }
    }
}
