using System;
using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 敌人攻击。和玩家一样，判定时机由攻击动画的 Animation Event 触发，
    /// 保证"看到爪子挥到身上"和"真的扣血"是同一帧。
    /// </summary>
    public class EnemyAttack : MonoBehaviour
    {
        [SerializeField] private AttackHitbox hitbox;

        /// <summary>攻击动画是否播完（由末尾的 Animation Event 置位）。</summary>
        public bool Finished { get; private set; }

        public event Action<Vector2> HitSomething;

        private void Awake()
        {
            if (hitbox == null) hitbox = GetComponentInChildren<AttackHitbox>();
            if (hitbox != null) hitbox.HitLanded += OnHitLanded;
        }

        private void OnDestroy()
        {
            if (hitbox != null) hitbox.HitLanded -= OnHitLanded;
        }

        private void OnHitLanded(Health target, DamageInfo info)
        {
            HitSomething?.Invoke(info.hitPoint);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.1f, 0.12f);
        }

        public void BeginAttack(int facing)
        {
            Finished = false;
            if (hitbox != null)
            {
                hitbox.SetFacing(facing);
                hitbox.BeginSwing();
            }
        }

        public void SetFacing(int facing)
        {
            if (hitbox != null) hitbox.SetFacing(facing);
        }

        // ---- 以下方法由 Animation Event 调用，不要改名 ----

        /// <summary>攻击动画的判定帧。</summary>
        public void AnimationEvent_AttackHit()
        {
            hitbox?.PerformSwing();
        }

        /// <summary>攻击动画最后一帧，通知状态机可以退出攻击状态。</summary>
        public void AnimationEvent_AttackEnd()
        {
            Finished = true;
        }
    }
}
