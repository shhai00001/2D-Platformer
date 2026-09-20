using UnityEngine;
using UnityEngine.UI;

namespace Platformer
{
    /// <summary>
    /// 关卡内 HUD。负责找到玩家、订阅血量变化、显示关卡信息与计时。
    /// 玩家是在关卡场景里生成的，所以用 FindFirstObjectByType 在 Start 里找。
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private Text levelText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text hintText;
        [SerializeField] private Text killText;

        [Header("提示")]
        [SerializeField] private string objectiveFormat = "抵达旗子即可过关";

        private PlayerHealth _player;

        private void Start()
        {
            _player = FindFirstObjectByType<PlayerHealth>();

            if (_player != null)
            {
                _player.Changed += HandleHealthChanged;
                HandleHealthChanged(_player);
            }
            else
            {
                Debug.LogWarning("[HudController] 场景里找不到 PlayerHealth，血条不会更新。");
            }

            GameManager gm = GameManager.Instance;
            if (gm != null && levelText != null)
            {
                levelText.text = $"第 {gm.CurrentLevelIndex + 1} 关 / 共 {gm.LevelCount} 关";
            }

            if (hintText != null) hintText.text = objectiveFormat;
        }

        private void OnDestroy()
        {
            if (_player != null) _player.Changed -= HandleHealthChanged;
        }

        private void HandleHealthChanged(Health health)
        {
            if (healthBar != null) healthBar.SetValue(health.CurrentHealth, health.MaxHealth);
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            if (timerText != null)
            {
                float t = gm.CurrentLevelTime;
                int minutes = Mathf.FloorToInt(t / 60f);
                int seconds = Mathf.FloorToInt(t % 60f);
                int centis = Mathf.FloorToInt((t * 100f) % 100f);
                timerText.text = $"{minutes:00}:{seconds:00}.{centis:00}";
            }

            if (killText != null)
            {
                killText.text = $"击杀 {gm.Stats.kills}";
            }
        }
    }
}
