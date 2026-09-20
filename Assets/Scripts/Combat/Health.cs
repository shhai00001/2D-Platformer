using System;
using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 通用生命值组件。玩家和敌人共用，靠 Team 区分敌我。
    /// 无敌帧保证一次挥砍不会连续扣血；死亡事件只会派发一次。
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [Header("基础")]
        [SerializeField] private Team team = Team.Player;
        [SerializeField] private int maxHealth = 5;

        [Header("无敌帧")]
        [Tooltip("受击后多久内免疫再次伤害，0 表示不免疫。")]
        [SerializeField] private float invulnerableDuration = 0.7f;

        [Header("调试")]
        [SerializeField] private bool immuneToDamage = false;

        /// <summary>血量变化时派发（包括治疗）。</summary>
        public event Action<Health> Changed;
        /// <summary>受伤且已扣血后派发。</summary>
        public event Action<Health, DamageInfo> Damaged;
        /// <summary>死亡时派发，只会派发一次。</summary>
        public event Action<Health, DamageInfo> Died;

        public Team Team => team;
        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;
        public float Normalized => maxHealth > 0 ? (float)CurrentHealth / maxHealth : 0f;

        private float _invulnerableUntil = -999f;

        protected virtual void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public bool TakeDamage(DamageInfo info)
        {
            if (immuneToDamage) return false;
            if (IsDead) return false;
            if (info.amount <= 0) return false;
            if (IsInvulnerable && !info.ignoreInvulnerability) return false;

            CurrentHealth = Mathf.Max(0, CurrentHealth - info.amount);
            _invulnerableUntil = Time.time + invulnerableDuration;

            Changed?.Invoke(this);
            Damaged?.Invoke(this, info);

            if (CurrentHealth == 0)
            {
                Died?.Invoke(this, info);
            }
            return true;
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            int before = CurrentHealth;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            if (CurrentHealth != before) Changed?.Invoke(this);
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            _invulnerableUntil = -999f;
            Changed?.Invoke(this);
        }

        public void SetMaxHealth(int value, bool refill = true)
        {
            maxHealth = Mathf.Max(1, value);
            CurrentHealth = refill ? maxHealth : Mathf.Min(CurrentHealth, maxHealth);
            Changed?.Invoke(this);
        }

        /// <summary>临时开启无敌（复活保护、剧情演出等）。</summary>
        public void SetInvulnerable(float duration)
        {
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + duration);
        }
    }
}
