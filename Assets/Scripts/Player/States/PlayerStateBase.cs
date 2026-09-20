namespace Platformer
{
    /// <summary>
    /// 玩家各状态的公共基类。只持有控制器引用和几个共用小工具，
    /// 具体行为交给子类，保持每个状态文件短小可读。
    /// </summary>
    public abstract class PlayerStateBase : IState
    {
        protected readonly PlayerController C;

        protected PlayerStateBase(PlayerController controller)
        {
            C = controller;
        }

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void FixedTick(float deltaTime) { }
        public virtual void Exit() { }

        /// <summary>有方向输入就加速，没有就减速。</summary>
        protected void ApplyHorizontal(float deltaTime, float controlScale = 1f)
        {
            if (C.Input.HasMoveInput) C.MoveHorizontal(C.Input.MoveDirection, deltaTime, controlScale);
            else C.Decelerate(deltaTime, controlScale);
        }

        /// <summary>落地后按当前输入决定回到 Idle 还是 Move。</summary>
        protected void GoToGroundedLocomotion()
        {
            C.ChangeState(C.Input.HasMoveInput ? (IState)C.MoveState : C.IdleState);
        }
    }
}
