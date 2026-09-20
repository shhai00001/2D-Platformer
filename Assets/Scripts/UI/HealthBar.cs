using UnityEngine;
using UnityEngine.UI;

namespace Platformer
{
    /// <summary>
    /// 血条。前面是即时条，后面跟一条延迟追赶的"残影条"，
    /// 掉血时能明显看出这次掉了多少。
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [Tooltip("延迟追赶的残影条，颜色偏暗。")]
        [SerializeField] private Image delayedFillImage;
        [SerializeField] private Text valueText;

        [Header("残影")]
        [SerializeField] private float catchUpDelay = 0.4f;
        [SerializeField] private float catchUpSpeed = 1.1f;

        private float _target = 1f;
        private float _delayed = 1f;
        private float _catchUpTimer;

        public void SetValue(int current, int max)
        {
            float normalized = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

            if (fillImage != null) fillImage.fillAmount = normalized;

            if (normalized < _target)
            {
                // 掉血：残影条从旧值开始追
                _catchUpTimer = catchUpDelay;
            }
            else
            {
                // 回血：残影条直接跟上，不要出现反向追赶的怪现象
                _delayed = normalized;
                if (delayedFillImage != null) delayedFillImage.fillAmount = normalized;
            }

            _target = normalized;

            if (valueText != null) valueText.text = $"{current}/{max}";
        }

        private void Update()
        {
            if (delayedFillImage == null) return;

            if (_catchUpTimer > 0f)
            {
                _catchUpTimer -= Time.unscaledDeltaTime;
                return;
            }

            if (Mathf.Approximately(_delayed, _target)) return;

            _delayed = Mathf.MoveTowards(_delayed, _target, catchUpSpeed * Time.unscaledDeltaTime);
            delayedFillImage.fillAmount = _delayed;
        }
    }
}
