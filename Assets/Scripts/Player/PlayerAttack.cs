using System;
using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 玩家攻击。真正的判定时机不在这里，而在攻击动画的 Animation Event 上：
    /// 动画播到"刀锋挥出"的那一帧会回调 AnimationEvent_AttackHit，
    /// 这时才调用 AttackHitbox.PerformSwing()。
    /// 这样判定和画面永远对齐，改动画不用改代码。
    /// </summary>
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private AttackHitbox hitbox;

        /// <summary>动作播完（由动画末尾的 Animation Event 置位）。</summary>
        public bool Finished { get; private set; }
        /// <summary>本次挥砍是否已经命中过目标。</summary>
        public bool DidHit { get; private set; }

        /// <summary>命中目标时派发，参数为受击点的世界坐标。</summary>
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
            DidHit = true;
            HitSomething?.Invoke(info.hitPoint);

            // 命中顿帧 + 镜头轻震，打击感来源
            HitStop.Play(0.055f);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.12f, 0.16f);
        }

        /// <summary>进入攻击状态时调用，重置本次挥砍的状态。</summary>
        public void BeginAttack(int facing)
        {
            Finished = false;
            DidHit = false;
            if (hitbox != null)
            {
                hitbox.SetFacing(facing);
                hitbox.BeginSwing();
            }
        }

        /// <summary>朝向变化时同步给判定盒。</summary>
        public void SetFacing(int facing)
        {
            if (hitbox != null) hitbox.SetFacing(facing);
        }

        // ---- 以下两个方法由 Animation Event 调用，不要改名 ----

        /// <summary>攻击动画的判定帧。在此刻做一次 OverlapBox 判定。</summary>
        public void AnimationEvent_AttackHit()
        {
            hitbox?.PerformSwing();
        }

        /// <summary>攻击动画最后一帧。通知状态机可以退出攻击状态了。</summary>
        public void AnimationEvent_AttackEnd()
        {
            Finished = true;
        }
    }

    /// <summary>
    /// 命中瞬间把时间缩放压到很低再弹回来，制造"顿帧"手感。
    /// 用不受时间缩放影响的协程来恢复，避免卡死。
    /// </summary>
    public static class HitStop
    {
        private class Runner : MonoBehaviour { }

        private static Runner _runner;

        public static void Play(float duration)
        {
            if (duration <= 0f) return;
            if (_runner == null)
            {
                GameObject go = new GameObject("[HitStopRunner]");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _runner = go.AddComponent<Runner>();
            }
            Time.timeScale = 0.05f;
            _runner.StartCoroutine(Restore(duration));
        }

        private static System.Collections.IEnumerator Restore(float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Time.timeScale = 1f;
        }
    }
}
