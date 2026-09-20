using UnityEngine;
using UnityEngine.UI;

namespace Platformer
{
    /// <summary>主菜单。开始游戏 / 退出。</summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text hintText;

        private void Start()
        {
            if (titleText != null) titleText.text = "勇 者 闯 关";
            if (hintText != null)
            {
                hintText.text = "A / D 或 ← → 移动　空格跳跃　J 攻击";
            }

            if (startButton != null) startButton.onClick.AddListener(HandleStart);
            if (quitButton != null) quitButton.onClick.AddListener(HandleQuit);
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(HandleStart);
            if (quitButton != null) quitButton.onClick.RemoveListener(HandleQuit);
        }

        private void HandleStart()
        {
            if (GameManager.Instance != null) GameManager.Instance.StartNewGame();
        }

        private void HandleQuit()
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        }
    }
}
