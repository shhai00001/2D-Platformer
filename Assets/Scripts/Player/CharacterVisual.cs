using System.Collections;
using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 角色的视觉表现：朝向翻转、受击闪白、无敌闪烁。
    /// 翻转用的是 SpriteRenderer.flipX 而不是缩放取负，
    /// 这样攻击判定盒的镜像由代码显式处理，物理体也不会出现负缩放。
    /// </summary>
    public class CharacterVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color flashColor = new Color(1f, 0.45f, 0.45f, 1f);
        [SerializeField] private float flashDuration = 0.12f;
        [SerializeField] private float blinkInterval = 0.08f;

        private Color _baseColor = Color.white;
        private Coroutine _flashRoutine;
        private Coroutine _blinkRoutine;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) _baseColor = spriteRenderer.color;
        }

        /// <summary>1 = 朝右，-1 = 朝左。</summary>
        public void SetFacing(int facing)
        {
            if (spriteRenderer != null) spriteRenderer.flipX = facing < 0;
        }

        /// <summary>受伤时闪一下。</summary>
        public void Flash()
        {
            if (spriteRenderer == null) return;
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = _baseColor;
            _flashRoutine = null;
        }

        /// <summary>无敌帧期间半透明闪烁。</summary>
        public void SetBlinking(bool on, float duration = 0f)
        {
            if (_blinkRoutine != null) { StopCoroutine(_blinkRoutine); _blinkRoutine = null; }
            if (spriteRenderer == null) return;

            if (!on)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = _baseColor;
                return;
            }
            _blinkRoutine = StartCoroutine(BlinkRoutine(duration));
        }

        private IEnumerator BlinkRoutine(float duration)
        {
            float end = duration > 0f ? Time.time + duration : float.MaxValue;
            while (Time.time < end)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
                yield return new WaitForSeconds(blinkInterval);
            }
            spriteRenderer.enabled = true;
            spriteRenderer.color = _baseColor;
            _blinkRoutine = null;
        }
    }
}
