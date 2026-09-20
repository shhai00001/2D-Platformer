using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 镜头震动。只负责算出偏移量，真正应用偏移的是 CameraFollow，
    /// 这样两者不会有执行顺序上的竞争。
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField] private float frequency = 22f;

        /// <summary>当前帧要叠加到相机上的偏移。</summary>
        public Vector2 Offset { get; private set; }

        private float _remaining;
        private float _duration;
        private float _amplitude;
        private float _seed;

        private void Awake()
        {
            Instance = this;
            _seed = Random.value * 100f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>触发一次震动，duration 秒内衰减。</summary>
        public void Shake(float duration, float amplitude)
        {
            // 已有更弱的震动在进行时不打断更强的
            if (_remaining > 0f && amplitude < _amplitude * (_remaining / Mathf.Max(_duration, 0.0001f)))
                return;

            _duration = Mathf.Max(0.0001f, duration);
            _remaining = _duration;
            _amplitude = amplitude;
        }

        private void Update()
        {
            if (_remaining <= 0f)
            {
                Offset = Vector2.zero;
                return;
            }

            _remaining -= Time.unscaledDeltaTime;
            float falloff = Mathf.Clamp01(_remaining / _duration);
            float t = (Time.unscaledTime + _seed) * frequency;

            // 两个不同频率的正弦，比 Random 更平滑，不会让画面抖成噪点
            Offset = new Vector2(
                (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f) * (_amplitude * falloff);

            if (_remaining <= 0f) Offset = Vector2.zero;
        }
    }
}
