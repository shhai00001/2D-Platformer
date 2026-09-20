using System;

namespace Platformer
{
    /// <summary>一个状态需要实现的接口。玩家和敌人共用同一套状态机骨架。</summary>
    public interface IState
    {
        void Enter();
        void Tick(float deltaTime);
        void FixedTick(float deltaTime);
        void Exit();
    }

    /// <summary>
    /// 极简状态机：只负责切换与计时，具体逻辑写在各个 IState 实现里。
    /// 逻辑状态机负责"能不能动 / 能不能出招"，Animator 状态机负责"播哪个动画"。
    /// </summary>
    public class StateMachine
    {
        public IState Current { get; private set; }
        public IState Previous { get; private set; }
        public float TimeInState { get; private set; }

        /// <summary>参数：上一个状态, 新状态。</summary>
        public event Action<IState, IState> StateChanged;

        public void Initialize(IState initial)
        {
            if (initial == null) return;
            Current = initial;
            TimeInState = 0f;
            Current.Enter();
        }

        /// <summary>切到新状态；传入相同状态或 null 时不做任何事。</summary>
        public void ChangeState(IState next)
        {
            if (next == null || ReferenceEquals(next, Current)) return;

            Previous = Current;
            Current?.Exit();
            Current = next;
            TimeInState = 0f;
            Current.Enter();
            StateChanged?.Invoke(Previous, Current);
        }

        /// <summary>强制重进当前状态（例如需要重播攻击动作）。</summary>
        public void RestartCurrent()
        {
            if (Current == null) return;
            Current.Exit();
            TimeInState = 0f;
            Current.Enter();
        }

        public void Tick(float deltaTime)
        {
            if (Current == null) return;
            TimeInState += deltaTime;
            Current.Tick(deltaTime);
        }

        public void FixedTick(float deltaTime)
        {
            Current?.FixedTick(deltaTime);
        }

        public bool Is<T>() where T : class, IState => Current as T != null;
    }
}
