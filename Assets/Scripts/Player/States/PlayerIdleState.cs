namespace Platformer
{
    /// <summary>待机：站在地面、没有输入。随时可以起跳、出招或转入移动。</summary>
    public class PlayerIdleState : PlayerStateBase
    {
        public PlayerIdleState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            C.Animator.SetFloat(AnimParams.Speed, 0f);
        }

        public override void Tick(float deltaTime)
        {
            if (!C.IsGrounded)
            {
                C.ChangeState(C.FallState);
                return;
            }

            if (C.Input.AttackPressed)
            {
                C.ChangeState(C.AttackState);
                return;
            }

            if (C.ConsumeJumpIfAvailable())
            {
                C.ChangeState(C.JumpState);
                return;
            }

            if (C.Input.HasMoveInput)
            {
                C.ChangeState(C.MoveState);
            }
        }

        public override void FixedTick(float deltaTime)
        {
            C.Decelerate(deltaTime);
        }
    }
}
