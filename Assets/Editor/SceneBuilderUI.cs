using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Platformer;

namespace Platformer.EditorTools
{
    /// <summary>SceneBuilder 的 UI 部分：HUD、主菜单、结算界面。</summary>
    public static partial class SceneBuilder
    {
        // ---------- UI 基础构件 ----------

        static Font GetUIFont()
        {
            // Unity 2022 起内置字体改名成 LegacyRuntime.ttf，老名字会返回 null 并报错
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { /* 老版本 Unity 走下面的分支 */ }
            if (font == null)
            {
                try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch { /* 两个都没有就只能用默认字体了 */ }
            }
            return font;
        }

        static GameObject CreateCanvasRoot(string name, int sortingOrder)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            return root;
        }

        static Image CreateImage(Transform parent, string name, Color color,
                                 Vector2 anchorMin, Vector2 anchorMax,
                                 Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>把 Image 变成横向填充条，用来做血条。</summary>
        static Image MakeFilled(Image img)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            return img;
        }

        static Text CreateText(Transform parent, string name, string content, int fontSize,
                               TextAnchor alignment, Color color,
                               Vector2 anchorMin, Vector2 anchorMax,
                               Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            Text text = go.AddComponent<Text>();
            text.font = GetUIFont();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 size,
                                   Vector2 anchoredPosition, Color baseColor, out Text labelText)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;

            Image img = go.GetComponent<Image>();
            // 底色留白，真实颜色交给 ColorBlock 乘出来，这样悬停/按下状态好控制
            img.color = Color.white;

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.35f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.3f);
            colors.selectedColor = baseColor;
            colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.4f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.targetGraphic = img;

            labelText = CreateText(go.transform, "Label", label, 38, TextAnchor.MiddleCenter, Color.white,
                                   Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            // 用的是旧版 Input Manager，所以配 StandaloneInputModule
            go.AddComponent<StandaloneInputModule>();
        }

        // ---------- 关卡内 HUD ----------

        static void BuildHud()
        {
            GameObject canvasRoot = CreateCanvasRoot("HUD Canvas", 100);

            // --- 血条（底 → 残影条 → 即时条，靠层级顺序压住） ---
            GameObject barRoot = new GameObject("HealthBar", typeof(RectTransform));
            barRoot.transform.SetParent(canvasRoot.transform, false);
            RectTransform barRt = barRoot.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(0f, 1f);
            barRt.pivot = new Vector2(0f, 1f);
            barRt.anchoredPosition = new Vector2(48f, -48f);
            barRt.sizeDelta = new Vector2(380f, 38f);

            HealthBar healthBar = barRoot.AddComponent<HealthBar>();

            CreateImage(barRoot.transform, "Background", new Color(0.06f, 0.07f, 0.11f, 0.9f),
                        Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Vector2 pad = new Vector2(4f, 4f);
            Image delayed = MakeFilled(CreateImage(barRoot.transform, "Delayed",
                        new Color(0.92f, 0.62f, 0.22f, 1f), Vector2.zero, Vector2.one, pad, -pad));
            Image fill = MakeFilled(CreateImage(barRoot.transform, "Fill",
                        new Color(0.38f, 0.86f, 0.42f, 1f), Vector2.zero, Vector2.one, pad, -pad));

            Text valueText = CreateText(barRoot.transform, "Value", "5 / 5", 28, TextAnchor.MiddleLeft,
                        new Color(1f, 1f, 1f, 0.9f),
                        new Vector2(0f, 0f), new Vector2(0f, 1f),
                        new Vector2(404f, 0f), new Vector2(600f, 0f));

            GameSetupUtil.SetRef(healthBar, "fillImage", fill);
            GameSetupUtil.SetRef(healthBar, "delayedFillImage", delayed);
            GameSetupUtil.SetRef(healthBar, "valueText", valueText);

            // --- 关卡信息、计时、击杀数 ---
            Text levelText = CreateText(canvasRoot.transform, "LevelText", "第 1 关", 30, TextAnchor.UpperLeft,
                        new Color(1f, 1f, 1f, 0.92f),
                        new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(48f, -104f), new Vector2(700f, -146f));

            Text killText = CreateText(canvasRoot.transform, "KillText", "击杀 0", 28, TextAnchor.UpperRight,
                        new Color(1f, 0.92f, 0.72f, 0.95f),
                        new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-460f, -104f), new Vector2(-48f, -146f));

            Text timerText = CreateText(canvasRoot.transform, "TimerText", "00:00.00", 34, TextAnchor.UpperRight,
                        Color.white,
                        new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-460f, -48f), new Vector2(-48f, -96f));

            Text hintText = CreateText(canvasRoot.transform, "HintText", "", 26, TextAnchor.LowerCenter,
                        new Color(1f, 1f, 1f, 0.75f),
                        new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(-600f, 42f), new Vector2(600f, 86f));

            CreateText(canvasRoot.transform, "Controls", "A / D 或 ← → 移动　　空格跳跃　　J 攻击", 24,
                        TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.55f),
                        new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(48f, -154f), new Vector2(900f, -192f));

            // --- 接线 ---
            GameObject hudGo = new GameObject("HUD");
            HudController hud = hudGo.AddComponent<HudController>();
            GameSetupUtil.SetRef(hud, "healthBar", healthBar);
            GameSetupUtil.SetRef(hud, "levelText", levelText);
            GameSetupUtil.SetRef(hud, "timerText", timerText);
            GameSetupUtil.SetRef(hud, "hintText", hintText);
            GameSetupUtil.SetRef(hud, "killText", killText);
        }

        // ---------- 主菜单场景 ----------

        static GameObject BuildSimpleCamera(string name, Color background)
        {
            GameObject go = new GameObject(name);
            go.tag = "MainCamera";
            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            go.transform.position = new Vector3(0f, 0f, -10f);
            go.AddComponent<AudioListener>();
            return go;
        }

        public static void BuildMainMenuScene()
        {
            EnsureFolder(SceneFolder);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildSimpleCamera("Main Camera", new Color(0.13f, 0.16f, 0.26f));
            EnsureEventSystem();

            GameObject canvasRoot = CreateCanvasRoot("Menu Canvas", 100);

            Text title = CreateText(canvasRoot.transform, "Title", "勇 者 闯 关", 110, TextAnchor.MiddleCenter,
                        new Color(1f, 0.88f, 0.45f),
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(-800f, 210f), new Vector2(800f, 400f));

            CreateText(canvasRoot.transform, "Subtitle", "两个关卡　一群史莱姆　一把剑", 34, TextAnchor.MiddleCenter,
                        new Color(1f, 1f, 1f, 0.7f),
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(-800f, 130f), new Vector2(800f, 186f));

            Button startButton = CreateButton(canvasRoot.transform, "StartButton", "开始游戏",
                        new Vector2(420f, 92f), new Vector2(0f, -40f),
                        new Color(0.24f, 0.45f, 0.72f), out _);

            Button quitButton = CreateButton(canvasRoot.transform, "QuitButton", "退出游戏",
                        new Vector2(420f, 92f), new Vector2(0f, -164f),
                        new Color(0.30f, 0.32f, 0.42f), out _);

            Text hint = CreateText(canvasRoot.transform, "Hint", "", 26, TextAnchor.LowerCenter,
                        new Color(1f, 1f, 1f, 0.6f),
                        new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(-800f, 60f), new Vector2(800f, 112f));

            GameObject uiGo = new GameObject("MenuUI");
            MainMenuUI ui = uiGo.AddComponent<MainMenuUI>();
            GameSetupUtil.SetRef(ui, "startButton", startButton);
            GameSetupUtil.SetRef(ui, "quitButton", quitButton);
            GameSetupUtil.SetRef(ui, "titleText", title);
            GameSetupUtil.SetRef(ui, "hintText", hint);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SceneFolder + "/" + SceneNames.MainMenu + ".unity");
            Debug.Log("[SceneBuilder] 已生成主菜单场景");
        }

        // ---------- 结算场景（胜利 / 失败共用） ----------

        public static void BuildResultScene()
        {
            EnsureFolder(SceneFolder);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildSimpleCamera("Main Camera", new Color(0.10f, 0.11f, 0.18f));
            EnsureEventSystem();

            GameObject canvasRoot = CreateCanvasRoot("Result Canvas", 100);

            Text title = CreateText(canvasRoot.transform, "Title", "", 104, TextAnchor.MiddleCenter,
                        Color.white,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(-800f, 230f), new Vector2(800f, 400f));

            Text subtitle = CreateText(canvasRoot.transform, "Subtitle", "", 34, TextAnchor.MiddleCenter,
                        new Color(1f, 1f, 1f, 0.75f),
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(-800f, 160f), new Vector2(800f, 212f));

            Text stats = CreateText(canvasRoot.transform, "Stats", "", 34, TextAnchor.MiddleCenter,
                        new Color(1f, 1f, 1f, 0.9f),
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(-400f, -120f), new Vector2(400f, 110f));

            Button primary = CreateButton(canvasRoot.transform, "PrimaryButton", "",
                        new Vector2(420f, 92f), new Vector2(0f, -230f),
                        new Color(0.24f, 0.45f, 0.72f), out Text primaryLabel);

            Button menu = CreateButton(canvasRoot.transform, "MenuButton", "返回主菜单",
                        new Vector2(420f, 78f), new Vector2(0f, -340f),
                        new Color(0.30f, 0.32f, 0.42f), out _);

            GameObject uiGo = new GameObject("ResultUI");
            ResultUI ui = uiGo.AddComponent<ResultUI>();
            GameSetupUtil.SetRef(ui, "titleText", title);
            GameSetupUtil.SetRef(ui, "subtitleText", subtitle);
            GameSetupUtil.SetRef(ui, "statsText", stats);
            GameSetupUtil.SetRef(ui, "primaryButton", primary);
            GameSetupUtil.SetRef(ui, "primaryButtonLabel", primaryLabel);
            GameSetupUtil.SetRef(ui, "menuButton", menu);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SceneFolder + "/" + SceneNames.Result + ".unity");
            Debug.Log("[SceneBuilder] 已生成结算场景");
        }
    }
}
