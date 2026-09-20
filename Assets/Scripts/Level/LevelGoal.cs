using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 关卡终点。挂在带 Trigger 的 Collider2D 上，
    /// 玩家碰到就通知 GameManager 结算。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class LevelGoal : MonoBehaviour
    {
        [Tooltip("旗子视觉（一个子物体），会轻微上下浮动提示玩家。")]
        [SerializeField] private Transform flagVisual;
        [SerializeField] private float bobSpeed = 2.4f;
        [SerializeField] private float bobAmount = 0.12f;
        [SerializeField] private float pulseAmount = 0.06f;

        private bool _triggered;
        private Vector3 _flagHome;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Awake()
        {
            if (flagVisual != null) _flagHome = flagVisual.localPosition;
        }

        private void Update()
        {
            if (flagVisual == null) return;
            float t = Time.time * bobSpeed;
            flagVisual.localPosition = _flagHome + Vector3.up * (Mathf.Sin(t) * bobAmount);
            flagVisual.localScale = Vector3.one * (1f + Mathf.Sin(t * 2f) * pulseAmount);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            if (player.Health != null && player.Health.IsDead) return;

            _triggered = true;

            if (player.Body != null) player.Body.velocity = Vector2.zero;
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.25f, 0.18f);

            if (GameManager.Instance != null) GameManager.Instance.NotifyLevelCleared();
        }
    }
}
