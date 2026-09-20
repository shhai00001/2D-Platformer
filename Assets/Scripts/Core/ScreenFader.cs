using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Platformer
{
    /// <summary>
    /// 全屏淡入淡出遮罩，挂在 GameManager 的常驻 Canvas 上。
    /// 用 unscaledDeltaTime，因此命中顿帧（timeScale 被压低）时照样能正常淡出。
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        private Image _image;

        public bool IsFading { get; private set; }
        public float Alpha => _image != null ? _image.color.a : 0f;

        public void Setup(Image image)
        {
            _image = image;
            _image.raycastTarget = false;
            SetAlphaImmediate(0f);
        }

        public void SetAlphaImmediate(float alpha)
        {
            if (_image == null) return;
            Color c = _image.color;
            c.a = Mathf.Clamp01(alpha);
            _image.color = c;
            _image.enabled = c.a > 0.001f;
        }

        /// <summary>渐变到指定透明度。</summary>
        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (_image == null) yield break;

            IsFading = true;
            float startAlpha = _image.color.a;

            if (duration <= 0f)
            {
                SetAlphaImmediate(targetAlpha);
                IsFading = false;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                SetAlphaImmediate(Mathf.Lerp(startAlpha, targetAlpha, t / duration));
                yield return null;
            }

            SetAlphaImmediate(targetAlpha);
            IsFading = false;
        }

        /// <summary>淡到全黑。</summary>
        public IEnumerator FadeOut(float duration) => FadeTo(1f, duration);

        /// <summary>从全黑淡回透明。</summary>
        public IEnumerator FadeIn(float duration) => FadeTo(0f, duration);
    }
}
