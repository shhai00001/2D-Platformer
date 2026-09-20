using System;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 攻击判定盒。挂在一个空的子物体上，本身不需要 Collider2D ——
    /// 判定完全靠 Physics2D.OverlapBoxAll 加 LayerMask 完成，不会干扰物理。
    /// 时序由 Animation Event 驱动：动画播到判定帧时调用 PerformSwing()。
    /// 一次挥砍对同一目标最多生效一次（由 _hitThisSwing 去重）。
    /// </summary>
    public class AttackHitbox : MonoBehaviour
    {
        [Header("判定范围（本地坐标，x 会随朝向镜像）")]
        [SerializeField] private Vector2 size = new Vector2(1.15f, 0.85f);
        [SerializeField] private Vector2 localOffset = new Vector2(0.75f, 0.55f);

        [Header("伤害")]
        [SerializeField] private int damage = 1;
        [SerializeField] private float knockbackForce = 9f;
        [Tooltip("击退的上挑角度，让受击方略微飞起来。")]
        [SerializeField] private float knockbackLiftAngle = 32f;
        [SerializeField] private float hitStun = 0.18f;

        [Header("筛选")]
        [Tooltip("只检测这些 Layer 上的 Health。玩家打敌人、敌人打玩家靠它区分。")]
        [SerializeField] private LayerMask targetMask;
        [Tooltip("一次挥砍最多命中几个目标。")]
        [SerializeField] private int maxTargetsPerSwing = 1;

        [Header("调试")]
        [SerializeField] private bool drawGizmo = true;

        /// <summary>命中回调，参数为被打的 Health 与伤害信息。</summary>
        public event Action<Health, DamageInfo> HitLanded;

        private readonly HashSet<Health> _hitThisSwing = new HashSet<Health>();
        private readonly List<Health> _candidates = new List<Health>(8);
        private int _hitCountThisSwing;
        private int _facing = 1;
        private int _lastSwingFrame = -1;

        public int Facing => _facing;
        public int Damage => damage;

        /// <summary>判定盒在世界空间中的中心点。</summary>
        public Vector2 WorldCenter =>
            (Vector2)transform.position + new Vector2(localOffset.x * _facing, localOffset.y);

        /// <summary>朝向改变时调用，让判定盒镜像到角色面前。</summary>
        public void SetFacing(int facing)
        {
            _facing = facing >= 0 ? 1 : -1;
        }

        /// <summary>每次挥砍开始前由攻击状态调用，清空命中记录。</summary>
        public void BeginSwing()
        {
            _hitThisSwing.Clear();
            _hitCountThisSwing = 0;
            _lastSwingFrame = -1;
        }

        /// <summary>
        /// 执行一次判定。由 Animation Event 在攻击动画的判定帧调用。
        /// 返回本次真正命中的目标数量。
        /// </summary>
        public int PerformSwing()
        {
            // 同一帧被重复触发时只算一次
            if (Time.frameCount == _lastSwingFrame) return 0;
            _lastSwingFrame = Time.frameCount;

            if (_hitCountThisSwing >= maxTargetsPerSwing) return 0;

            _candidates.Clear();
            Collider2D[] hits = Physics2D.OverlapBoxAll(WorldCenter, size, 0f, targetMask);
            for (int i = 0; i < hits.Length; i++)
            {
                Health health = hits[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead) continue;
                if (_hitThisSwing.Contains(health)) continue;
                if (_candidates.Contains(health)) continue;
                _candidates.Add(health);
            }

            if (_candidates.Count == 0) return 0;

            // 近战优先打最近的，避免一次挥砍打到远处的敌人
            Vector2 center = WorldCenter;
            _candidates.Sort(delegate (Health a, Health b)
            {
                float da = ((Vector2)a.transform.position - center).sqrMagnitude;
                float db = ((Vector2)b.transform.position - center).sqrMagnitude;
                return da.CompareTo(db);
            });

            int landed = 0;
            for (int i = 0; i < _candidates.Count; i++)
            {
                if (_hitCountThisSwing >= maxTargetsPerSwing) break;

                Health health = _candidates[i];
                _hitThisSwing.Add(health);

                Vector2 knockback = MathUtil.KnockbackVelocity(_facing, knockbackForce, knockbackLiftAngle);
                Collider2D col = health.GetComponentInChildren<Collider2D>();
                Vector2 hitPoint = col != null ? col.ClosestPoint(center) : (Vector2)health.transform.position;

                DamageInfo info = new DamageInfo(damage, knockback, gameObject, hitStun, hitPoint);
                if (health.TakeDamage(info))
                {
                    _hitCountThisSwing++;
                    landed++;
                    HitLanded?.Invoke(health, info);
                }
            }
            return landed;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo) return;
            Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.35f);
            Gizmos.DrawCube(WorldCenter, size);
            Gizmos.color = new Color(1f, 0.35f, 0.2f, 1f);
            Gizmos.DrawWireCube(WorldCenter, size);
        }
    }
}
