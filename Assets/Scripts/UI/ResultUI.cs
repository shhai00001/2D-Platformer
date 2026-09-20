using UnityEngine;
using UnityEngine.UI;

namespace Platformer
{
    /// <summary>
    /// 结算界面。同一个场景按 GameManager.State 显示三种结果：
    /// 关卡完成（还有下一关）、通关胜利、挑战失败。
    /// </summary>
    public class ResultUI : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text statsText;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Text primaryButtonLabel;
        [SerializeField] private Button menuButton;

        private void Start()
        {
            GameManager gm = GameManager.Instance;
            GameState state = gm != null ? gm.State : GameState.Defeat;

            switch (state)
            {
                case GameState.Victory:
                    SetTexts("通 关 胜 利", "你击败了所有关卡的敌人", "再玩一次", new Color(1f, 0.85f, 0.35f));
                    if (primaryButton != null) primaryButton.onClick.AddListener(HandlePlayAgain);
                    break;

                case GameState.LevelCleared:
                    SetTexts("关 卡 完 成", "继续前往下一关", "下一关", new Color(0.5f, 0.9f, 1f));
                    if (primaryButton != null) primaryButton.onClick.AddListener(HandleNextLevel);
                    break;

                default:
                    SetTexts("挑 战 失 败", "别灰心，再来一次", "重试本关", new Color(1f, 0.45f, 0.45f));
                    if (primaryButton != null) primaryButton.onClick.AddListener(HandleRetry);
                    break;
            }

            if (statsText != null && gm != null) statsText.text = BuildStatsText(gm.Stats);
            if (menuButton != null) menuButton.onClick.AddListener(HandleMainMenu);
        }

        private void OnDestroy()
        {
            if (primaryButton != null) primaryButton.onClick.RemoveAllListeners();
            if (menuButton != null) menuButton.onClick.RemoveAllListeners();
        }

        private void SetTexts(string title, string subtitle, string primaryLabel, Color titleColor)
        {
            if (titleText != null)
            {
                titleText.text = title;
                titleText.color = titleColor;
            }
            if (subtitleText != null) subtitleText.text = subtitle;
            if (primaryButtonLabel != null) primaryButtonLabel.text = primaryLabel;
        }

        private static string BuildStatsText(RunStats stats)
        {
            int minutes = Mathf.FloorToInt(stats.elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(stats.elapsedTime % 60f);
            return $"用时　{minutes:00}:{seconds:00}\n" +
                   $"击杀　{stats.kills}\n" +
                   $"死亡　{stats.deaths}\n" +
                   $"通过　{stats.levelsCleared} 关";
        }

        private void HandlePlayAgain()
        {
            if (GameManager.Instance != null) GameManager.Instance.StartNewGame();
        }

        private void HandleNextLevel()
        {
            if (GameManager.Instance != null) GameManager.Instance.GoToNextLevel();
        }

        private void HandleRetry()
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        private void HandleMainMenu()
        {
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
        }
    }
}
