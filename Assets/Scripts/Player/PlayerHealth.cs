using System.Collections;
using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 玩家生命值。负责在死亡后延迟一小段时间再触发结算，
    /// 让死亡动画有机会播完。
    /// </summary>
    public class PlayerHealth : Health
    {
        [Tooltip("死亡动画播放多久后进入失败结算。")]
        [SerializeField] private float deathDelay = 1.8f;

        [SerializeField] private CharacterVisual visual;

        private bool _deathRoutineStarted;

        protected override void Awake()
        {
            base.Awake();
            if (visual == null) visual = GetComponent<CharacterVisual>();
        }

        private void OnEnable()
        {
            Damaged += HandleDamaged;
            Died += HandleDied;
            Changed += HandleChanged;
        }

        private void OnDisable()
        {
            Damaged -= HandleDamaged;
            Died -= HandleDied;
            Changed -= HandleChanged;
        }

        private void HandleDamaged(Health h, DamageInfo info)
        {
            if (visual != null) visual.Flash();
        }

        private void HandleChanged(Health h)
        {
            // 血量回到满或复活时停止闪烁
            if (!IsInvulnerable && visual != null) visual.SetBlinking(false);
        }

        private void HandleDied(Health h, DamageInfo info)
        {
            if (_deathRoutineStarted) return;
            _deathRoutineStarted = true;
            if (visual != null) visual.SetBlinking(false);
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            // 用不受 timeScale 影响的等待，避免命中顿帧时卡住结算
            float t = 0f;
            while (t < deathDelay)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (GameManager.Instance != null) GameManager.Instance.NotifyPlayerDied();
        }

        /// <summary>复活 / 重开关卡时重置。</summary>
        public void Revive()
        {
            _deathRoutineStarted = false;
            StopAllCoroutines();
            ResetHealth();
            if (visual != null) visual.SetBlinking(false);
        }
    }
}
