using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 追击。朝玩家移动，进入攻击距离且冷却结束时切到攻击状态。
    ///
    /// 冷却中的处理是"避免连续攻击"的第二道保险：
    /// 贴得太近时会主动后撤，而不是站在原地反复触发攻击。
    /// </summary>
    public class EnemyChaseState : EnemyStateBase
    {
        public EnemyChaseState(EnemyController controller) : base(controller) { }

        public override void Enter()
        {
            C.SetFacing(C.HorizontalDirectionToPlayer());
        }

        public override void Tick(float deltaTime)
        {
            if (!C.HasTarget || C.DistanceToPlayer() > C.ChaseGiveUpDistance)
            {
                C.ChangeState(C.PatrolState);
                return;
            }

            C.SetFacing(C.HorizontalDirectionToPlayer());

            // 冷却结束 + 玩家在攻击范围内 → 出手
            if (C.CanAttack && C.IsPlayerInAttackRange())
            {
                C.ChangeState(C.AttackState);
            }
        }

        public override void FixedTick(float deltaTime)
        {
            float direction = C.HorizontalDirectionToPlayer();
            float distance = C.DistanceToPlayer();

            // 冷却中且贴脸 → 后撤拉开距离，等冷却好了再上
            if (!C.CanAttack && distance <= C.AttackRange * 0.9f)
            {
                C.Move(-direction * C.ChaseSpeed * 0.6f);
                return;
            }

            // 前方是悬崖就别追了，免得自己掉下去
            if (C.IsLedgeAhead())
            {
                C.StopMoving();
                return;
            }

            C.Move(direction * C.ChaseSpeed);
        }
    }
}
