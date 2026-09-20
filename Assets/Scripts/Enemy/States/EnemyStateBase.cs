namespace Platformer
{
    /// <summary>敌人各状态的公共基类。</summary>
    public abstract class EnemyStateBase : IState
    {
        protected readonly EnemyController C;

        protected EnemyStateBase(EnemyController controller)
        {
            C = controller;
        }

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void FixedTick(float deltaTime) { }
        public virtual void Exit() { }
    }
}
