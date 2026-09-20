using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 巡逻。在出生点左右各 patrolDistance 的范围内来回走，
    /// 遇到墙、走到悬崖边或超出巡逻范围就掉头并停顿一下。
    /// 一旦发现玩家立刻切到追击。
    /// </summary>
    public class EnemyPatrolState : EnemyStateBase
    {
        private float _direction = -1f;
        private float _pauseTimer;

        public EnemyPatrolState(EnemyController controller) : base(controller) { }

        public override void Enter()
        {
            _direction = C.Facing;
            _pauseTimer = 0f;
        }

        public override void Tick(float deltaTime)
        {
            if (C.HasTarget)
            {
                C.ChangeState(C.ChaseState);
                return;
            }

            if (_pauseTimer > 0f) _pauseTimer -= deltaTime;
        }

        public override void FixedTick(float deltaTime)
        {
            if (_pauseTimer > 0f)
            {
                C.StopMoving();
                return;
            }

            bool outOfRange = Mathf.Abs(C.Body.position.x - C.PatrolOriginX) >= C.PatrolDistance;
            if (outOfRange || C.IsLedgeAhead() || C.IsWallAhead())
            {
                TurnAround();
                return;
            }

            C.SetFacing(_direction);
            C.Move(_direction * C.PatrolSpeed);
        }

        private void TurnAround()
        {
            _direction = -_direction;
            C.SetFacing(_direction);
            C.Move(0f);
            _pauseTimer = C.PatrolTurnPause;
        }
    }
}
