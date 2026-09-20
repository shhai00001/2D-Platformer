using UnityEngine;

namespace Platformer
{
    /// <summary>阵营。攻击判定靠它来区分敌我，而不是靠 Tag 字符串。</summary>
    public enum Team
    {
        Player,
        Enemy,
        Neutral
    }

    /// <summary>一次伤害的完整描述：数值、击退、来源、硬直。</summary>
    public struct DamageInfo
    {
        /// <summary>扣多少血。</summary>
        public int amount;
        /// <summary>已经算好的击退速度（水平 + 上挑）。</summary>
        public Vector2 knockback;
        /// <summary>伤害来源，一般是攻击者的 GameObject。</summary>
        public GameObject source;
        /// <summary>受击硬直时长，受击状态会持续这么久。</summary>
        public float hitStun;
        /// <summary>命中点，用来放打击特效。</summary>
        public Vector2 hitPoint;
        /// <summary>为 true 时无视无敌帧（例如掉进深坑直接秒杀）。</summary>
        public bool ignoreInvulnerability;

        public DamageInfo(int amount, Vector2 knockback, GameObject source = null,
                          float hitStun = 0.18f, Vector2 hitPoint = default, bool ignoreInvulnerability = false)
        {
            this.amount = amount;
            this.knockback = knockback;
            this.source = source;
            this.hitStun = hitStun;
            this.hitPoint = hitPoint;
            this.ignoreInvulnerability = ignoreInvulnerability;
        }

        /// <summary>一击必杀，用于深坑 / 陷阱。</summary>
        public static DamageInfo Fatal(GameObject source = null)
            => new DamageInfo(9999, Vector2.zero, source, 0f, default, true);
    }

    /// <summary>任何可以被攻击的东西都实现这个接口。</summary>
    public interface IDamageable
    {
        bool IsDead { get; }
        /// <summary>返回 true 表示这次伤害真的生效了（没被无敌帧挡掉）。</summary>
        bool TakeDamage(DamageInfo info);
    }
}
