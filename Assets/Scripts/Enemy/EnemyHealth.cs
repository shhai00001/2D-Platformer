using System.Collections;
using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 敌人生命值。死亡后让尸体停留一会儿再销毁，
    /// 期间手动切换图层，避免尸体继续触发玩家攻击判定。
    /// </summary>
    public class EnemyHealth : Health
    {
        [Tooltip("死亡后多久销毁尸体。")]
        [SerializeField] private float corpseLifetime = 2f;

        [Header("击杀奖励")]
        [Tooltip("击杀后给玩家恢复几点血，0 表示不恢复。")]
        [SerializeField] private int healPlayerOnKill = 0;

        [SerializeField] private CharacterVisual visual;

        protected override void Awake()
        {
            base.Awake();
            if (visual == null) visual = GetComponent<CharacterVisual>();
        }

        private void OnEnable()
        {
            Damaged += HandleDamaged;
            Died += HandleDied;
        }

        private void OnDisable()
        {
            Damaged -= HandleDamaged;
            Died -= HandleDied;
        }

        private void HandleDamaged(Health h, DamageInfo info)
        {
            if (visual != null) visual.Flash();
        }

        private void HandleDied(Health h, DamageInfo info)
        {
            if (visual != null) visual.SetBlinking(false);

            if (healPlayerOnKill > 0 && info.source != null)
            {
                PlayerHealth player = info.source.GetComponentInParent<PlayerHealth>();
                if (player != null) player.Heal(healPlayerOnKill);
            }

            StartCoroutine(DespawnRoutine());
        }

        private IEnumerator DespawnRoutine()
        {
            // 关掉碰撞体，让尸体不再阻挡玩家、也不再被攻击判定扫到
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;

            yield return new WaitForSeconds(corpseLifetime);
            Destroy(gameObject);
        }
    }
}
