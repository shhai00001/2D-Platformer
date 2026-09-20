using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Platformer
{
    /// <summary>
    /// 全局游戏管理器。整局游戏只有一个实例，跨场景常驻，
    /// 负责关卡流程、胜负结算、场景切换与淡入淡出。
    ///
    /// 不需要手动放进场景：标了 RuntimeInitializeOnLoadMethod，
    /// 在任何场景直接按 Play 都会自动创建，方便单独调试某一关。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("关卡顺序")]
        [SerializeField] private string[] levels = { SceneNames.Level01, SceneNames.Level02 };

        [Header("转场")]
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private float fadeInDuration = 0.3f;

        public GameState State { get; private set; } = GameState.Boot;
        /// <summary>状态变化时派发，UI 用它来刷新。</summary>
        public event Action<GameState> StateChanged;

        public int CurrentLevelIndex { get; private set; }
        public int LevelCount => levels != null ? levels.Length : 0;
        public bool IsLastLevel => CurrentLevelIndex >= LevelCount - 1;
        public string CurrentLevelName =>
            (levels != null && CurrentLevelIndex >= 0 && CurrentLevelIndex < levels.Length)
                ? levels[CurrentLevelIndex] : string.Empty;

        public RunStats Stats { get; private set; }
        public bool IsTransitioning { get; private set; }
        public ScreenFader Fader { get; private set; }

        /// <summary>本关已经玩了多久。</summary>
        public float CurrentLevelTime { get; private set; }

        /// <summary>暂停一次输入响应，避免转场期间玩家还能操作。</summary>
        public bool AcceptsGameplayInput => State == GameState.Playing && !IsTransitioning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            GameObject go = new GameObject("[GameManager]");
            go.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Stats = new RunStats();
            CreatePersistentUI();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (State == GameState.Playing && !IsTransitioning)
            {
                float dt = Time.unscaledDeltaTime;
                CurrentLevelTime += dt;

                RunStats s = Stats;
                s.elapsedTime += dt;
                Stats = s;
            }
        }

        // ---------- 常驻 UI（转场遮罩） ----------

        private void CreatePersistentUI()
        {
            GameObject canvasGo = new GameObject("[PersistentCanvas]");
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject fadeGo = new GameObject("Fade", typeof(RectTransform));
            fadeGo.transform.SetParent(canvasGo.transform, false);

            RectTransform rt = fadeGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image image = fadeGo.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            Fader = canvasGo.AddComponent<ScreenFader>();
            Fader.Setup(image);
        }

        // ---------- 流程 ----------

        /// <summary>开始新游戏：清空统计并从第一关开始。</summary>
        public void StartNewGame()
        {
            Stats = new RunStats();
            CurrentLevelIndex = 0;
            SetState(GameState.Playing);
            LoadScene(CurrentLevelName);
        }

        /// <summary>重玩本关（失败界面用）。</summary>
        public void RestartLevel()
        {
            SetState(GameState.Playing);
            LoadScene(string.IsNullOrEmpty(CurrentLevelName) ? SceneNames.Level01 : CurrentLevelName);
        }

        /// <summary>进入下一关；已经是最后一关就回主菜单。</summary>
        public void GoToNextLevel()
        {
            int next = CurrentLevelIndex + 1;
            if (next >= LevelCount)
            {
                ReturnToMainMenu();
                return;
            }

            CurrentLevelIndex = next;
            SetState(GameState.Playing);
            LoadScene(CurrentLevelName);
        }

        public void ReturnToMainMenu()
        {
            SetState(GameState.MainMenu);
            LoadScene(SceneNames.MainMenu);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>玩家死亡，由 PlayerHealth 延迟调用。</summary>
        public void NotifyPlayerDied()
        {
            if (State == GameState.Defeat || State == GameState.Victory) return;

            RunStats s = Stats;
            s.deaths++;
            Stats = s;

            SetState(GameState.Defeat);
            LoadScene(SceneNames.Result);
        }

        /// <summary>到达终点，由 LevelGoal 调用。</summary>
        public void NotifyLevelCleared()
        {
            if (State != GameState.Playing || IsTransitioning) return;

            RunStats s = Stats;
            s.levelsCleared++;
            Stats = s;

            // 还有下一关就直接进，最后一关才进结算界面
            if (!IsLastLevel)
            {
                SetState(GameState.LevelCleared);
                LoadScene(SceneNames.Result);
            }
            else
            {
                SetState(GameState.Victory);
                LoadScene(SceneNames.Result);
            }
        }

        /// <summary>敌人死亡时调用。</summary>
        public void RegisterKill()
        {
            RunStats s = Stats;
            s.kills++;
            Stats = s;
        }

        public void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(State);
        }

        // ---------- 场景切换 ----------

        private void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (IsTransitioning) return;
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            IsTransitioning = true;

            // 清掉可能残留的命中顿帧
            Time.timeScale = 1f;

            if (Fader != null) yield return Fader.FadeOut(fadeOutDuration);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError($"[GameManager] 场景 \"{sceneName}\" 不在 Build Settings 里，无法加载。" +
                               "请执行菜单 Tools/2D闯关游戏/① 一键生成全部 来自动配置。");
                IsTransitioning = false;
                if (Fader != null) yield return Fader.FadeIn(fadeInDuration);
                yield break;
            }

            while (!op.isDone) yield return null;

            // 多等一帧，让新场景的 Awake/Start 跑完，避免淡入时画面还没准备好
            yield return null;

            if (Fader != null) yield return Fader.FadeIn(fadeInDuration);

            IsTransitioning = false;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            int index = Array.IndexOf(levels, scene.name);
            if (index >= 0)
            {
                CurrentLevelIndex = index;
                CurrentLevelTime = 0f;
                if (State != GameState.Playing) SetState(GameState.Playing);
                return;
            }

            if (scene.name == SceneNames.MainMenu)
            {
                // 从结算界面回主菜单时重置，避免状态残留
                if (State != GameState.MainMenu) SetState(GameState.MainMenu);
            }

            // Result 场景保持进入时的 Victory / Defeat / LevelCleared 状态不变
        }
    }
}
