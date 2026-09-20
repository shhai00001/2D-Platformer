using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Platformer;

namespace Platformer.EditorTools
{
    /// <summary>
    /// 一键生成入口，菜单位于 Tools/2D闯关游戏/。
    /// 第一次用点①，之后改完脚本可以只重跑需要的那一步。
    /// </summary>
    public static class GameBootstrapper
    {
        const string MenuRoot = "Tools/2D闯关游戏/";

        [MenuItem(MenuRoot + "① 一键生成全部（推荐）", false, 1)]
        public static void GenerateEverything()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                GenerateAllAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // 直接打开第一关，按 Play 就能玩
            EditorSceneManager.OpenScene(SceneBuilder.SceneFolder + "/" + SceneNames.Level01 + ".unity");

            EditorUtility.DisplayDialog(
                "生成完成",
                "全部资源已生成，并自动打开了 Level_01。\n" +
                "按 Play 试玩：\n" +
                "　A / D 或 ← →　移动\n" +
                "　空格　跳跃\n" +
                "　J 或鼠标左键　攻击\n" +
                "美术在 Assets/Generated/Art，可以直接换成自己的图。",
                "好");
        }

        /// <summary>
        /// 实际的生成流程。菜单和命令行批处理共用同一条路径，
        /// 所以 -executeMethod 跑出来的结果和手点菜单完全一致。
        /// </summary>
        public static void GenerateAllAssets()
        {
            Report("配置 Tag 与 Layer…", 0.05f);
            ProjectSetup.EnsureTags();
            ProjectSetup.EnsureLayers();

            Report("生成像素美术…", 0.18f);
            PixelArtGenerator.GeneratedArt art = PixelArtGenerator.GenerateAll();

            Report("生成动画与 Animator 状态机…", 0.42f);
            RuntimeAnimatorController playerController = AnimationBuilder.BuildPlayerController(art.player);
            RuntimeAnimatorController enemyController = AnimationBuilder.BuildEnemyController(art.enemy);

            Report("生成 Prefab…", 0.58f);
            GameObject playerPrefab = PrefabBuilder.BuildPlayerPrefab(art, playerController);
            GameObject enemyPrefab = PrefabBuilder.BuildEnemyPrefab(art, enemyController);

            Report("生成场景…", 0.72f);
            SceneBuilder.BuildMainMenuScene();
            SceneBuilder.BuildLevel(SceneBuilder.Level01, art, playerPrefab, enemyPrefab);
            SceneBuilder.BuildLevel(SceneBuilder.Level02, art, playerPrefab, enemyPrefab);
            SceneBuilder.BuildResultScene();

            Report("写入 Build Settings…", 0.95f);
            ProjectSetup.EnsureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>批处理模式下没有进度条窗口，改成打日志。</summary>
        static void Report(string step, float progress)
        {
            if (Application.isBatchMode) Debug.Log("[GameBootstrapper] " + step);
            else EditorUtility.DisplayProgressBar("2D 闯关游戏", step, progress);
        }

        /// <summary>
        /// 命令行入口：
        /// Unity.exe -batchmode -quit -projectPath &lt;路径&gt; -executeMethod Platformer.EditorTools.GameBootstrapper.GenerateForBatchMode
        /// </summary>
        public static void GenerateForBatchMode()
        {
            GenerateAllAssets();
            EditorUtility.ClearProgressBar();
            Debug.Log("[GameBootstrapper] 批处理生成完成");
        }

        [MenuItem(MenuRoot + "② 只重新初始化 Tag 与 Layer", false, 20)]
        public static void RegenerateSetup()
        {
            ProjectSetup.EnsureTags();
            ProjectSetup.EnsureLayers();
            EditorUtility.DisplayDialog("完成", "Tag 与 Layer 已就绪。", "好");
        }

        [MenuItem(MenuRoot + "③ 只重新生成美术与动画", false, 21)]
        public static void RegenerateArt()
        {
            PixelArtGenerator.GeneratedArt art = PixelArtGenerator.GenerateAll();
            AnimationBuilder.BuildPlayerController(art.player);
            AnimationBuilder.BuildEnemyController(art.enemy);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成",
                "美术与动画已重新生成。\n\n" +
                "Prefab 上的 Sprite 引用不会自动更新，如需同步请再跑一次④。", "好");
        }

        [MenuItem(MenuRoot + "④ 只重新生成 Prefab", false, 22)]
        public static void RegeneratePrefabs()
        {
            PixelArtGenerator.GeneratedArt art = LoadExistingArt();
            if (art == null) return;

            RuntimeAnimatorController playerController = Load<RuntimeAnimatorController>(
                AnimationBuilder.AnimFolder + "/Player.controller");
            RuntimeAnimatorController enemyController = Load<RuntimeAnimatorController>(
                AnimationBuilder.AnimFolder + "/Enemy.controller");

            if (playerController == null || enemyController == null)
            {
                EditorUtility.DisplayDialog("缺少动画控制器",
                    "找不到 Player.controller / Enemy.controller，请先执行③。", "好");
                return;
            }

            PrefabBuilder.BuildPlayerPrefab(art, playerController);
            PrefabBuilder.BuildEnemyPrefab(art, enemyController);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("完成", "Prefab 已重新生成。", "好");
        }

        [MenuItem(MenuRoot + "⑤ 只重新生成场景", false, 23)]
        public static void RegenerateScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            GameObject playerPrefab = Load<GameObject>(PrefabBuilder.PrefabFolder + "/Player.prefab");
            GameObject enemyPrefab = Load<GameObject>(PrefabBuilder.PrefabFolder + "/Enemy.prefab");

            if (playerPrefab == null || enemyPrefab == null)
            {
                EditorUtility.DisplayDialog("缺少 Prefab",
                    "找不到 Player.prefab / Enemy.prefab，请先执行④。", "好");
                return;
            }

            PixelArtGenerator.GeneratedArt art = LoadExistingArt();
            if (art == null) return;

            SceneBuilder.BuildMainMenuScene();
            SceneBuilder.BuildLevel(SceneBuilder.Level01, art, playerPrefab, enemyPrefab);
            SceneBuilder.BuildLevel(SceneBuilder.Level02, art, playerPrefab, enemyPrefab);
            SceneBuilder.BuildResultScene();
            ProjectSetup.EnsureBuildSettings();

            EditorSceneManager.OpenScene(SceneBuilder.SceneFolder + "/" + SceneNames.Level01 + ".unity");
            EditorUtility.DisplayDialog("完成", "场景已重新生成，Build Settings 也已同步。", "好");
        }

        [MenuItem(MenuRoot + "打开生成目录", false, 40)]
        public static void OpenGeneratedFolder()
        {
            EditorUtility.RevealInFinder("Assets/Generated");
        }

        // ---------- 从磁盘读回已生成的美术 ----------

        static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        /// <summary>按 "<前缀>_<组名>_<序号>" 的命名约定把一组帧读回来。</summary>
        static Sprite[] LoadFrames(string folder, string prefix, string group, int count)
        {
            Sprite[] frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = Load<Sprite>($"{folder}/{prefix}_{group}_{i}.png");
                if (frames[i] == null) return null;
            }
            return frames;
        }

        static PixelArtGenerator.CharacterArt LoadPlayerArt(string folder)
        {
            return new PixelArtGenerator.CharacterArt
            {
                idle   = LoadFrames(folder, "player", "idle", 2),
                run    = LoadFrames(folder, "player", "run", 4),
                jump   = LoadFrames(folder, "player", "jump", 1),
                fall   = LoadFrames(folder, "player", "fall", 1),
                attack = LoadFrames(folder, "player", "atk", 5),
                hurt   = LoadFrames(folder, "player", "hurt", 1),
                death  = LoadFrames(folder, "player", "death", 1),
            };
        }

        static PixelArtGenerator.CharacterArt LoadEnemyArt(string folder)
        {
            Sprite[] idle = LoadFrames(folder, "slime", "idle", 2);
            return new PixelArtGenerator.CharacterArt
            {
                idle   = idle,
                run    = LoadFrames(folder, "slime", "walk", 4),
                attack = LoadFrames(folder, "slime", "atk", 3),
                hurt   = LoadFrames(folder, "slime", "hurt", 1),
                death  = LoadFrames(folder, "slime", "death", 1),
                // 史莱姆不会跳，跳跃/下落帧复用待机帧
                jump   = idle,
                fall   = idle,
            };
        }

        static PixelArtGenerator.GeneratedArt LoadExistingArt()
        {
            const string folder = PixelArtGenerator.ArtFolder;

            PixelArtGenerator.GeneratedArt art = new PixelArtGenerator.GeneratedArt
            {
                player       = LoadPlayerArt(folder),
                enemy        = LoadEnemyArt(folder),
                groundTile   = Load<Sprite>(folder + "/tile_ground.png"),
                dirtTile     = Load<Sprite>(folder + "/tile_dirt.png"),
                platformTile = Load<Sprite>(folder + "/tile_platform.png"),
                goal         = Load<Sprite>(folder + "/goal_flag.png"),
                sky          = Load<Sprite>(folder + "/bg_sky.png"),
                hillsFar     = Load<Sprite>(folder + "/bg_hills_far.png"),
                hillsNear    = Load<Sprite>(folder + "/bg_hills_near.png"),
            };

            bool ok = art.player != null && art.player.idle != null
                   && art.enemy != null && art.enemy.idle != null
                   && art.groundTile != null;

            if (!ok)
            {
                EditorUtility.DisplayDialog("缺少美术资源",
                    "读不到已生成的美术文件，请先执行①或③。", "好");
                return null;
            }
            return art;
        }
    }
}
