using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Platformer;

namespace Platformer.EditorTools
{
    /// <summary>
    /// 关卡场景的搭建逻辑。地形直接用代码摆放而不是 Tilemap ——
    /// 关卡数据就是几个数组，改数值比在编辑器里拖格子快，也方便做多关。
    /// </summary>
    public static partial class SceneBuilder
    {
        public const string SceneFolder = "Assets/Generated/Scenes";

        // ---------- 关卡数据结构 ----------

        /// <summary>一段地面。left/right 是左右边界，top 是地表高度，往下延伸 height。</summary>
        public struct GroundSeg
        {
            public float left, right, top, height;
            public GroundSeg(float left, float right, float top, float height = 4f)
            {
                this.left = left; this.right = right; this.top = top; this.height = height;
            }
        }

        /// <summary>一块悬空平台。centerX 是中心，top 是站上去的高度。</summary>
        public struct PlatformDef
        {
            public float centerX, top, width;
            public PlatformDef(float centerX, float top, float width)
            {
                this.centerX = centerX; this.top = top; this.width = width;
            }
        }

        public class LevelLayout
        {
            public string sceneName;
            public float width;
            public Vector2 spawn;
            public Vector2 goal;
            public GroundSeg[] grounds;
            public PlatformDef[] platforms;
            public Vector2[] enemies;
            public Vector2 cameraMin;
            public Vector2 cameraMax;
        }

        // ---------- 两个关卡 ----------

        // 坑宽都控制在 3 个单位左右。玩家满跳的水平距离约 4.5 个单位，
        // 这个宽度属于"需要跳，但不至于跳到烦躁"的区间。
        public static LevelLayout Level01 => new LevelLayout
        {
            sceneName = SceneNames.Level01,
            width = 64f,
            spawn = new Vector2(2f, 1f),
            goal = new Vector2(61f, 0f),
            grounds = new[]
            {
                new GroundSeg(0f, 15f, 0f),
                new GroundSeg(18f, 32f, 0f),
                new GroundSeg(35f, 50f, 0f),
                new GroundSeg(53f, 64f, 0f),
            },
            platforms = new[]
            {
                new PlatformDef(12f, 2.5f, 4f),
                new PlatformDef(22f, 2.5f, 3f),
                new PlatformDef(27f, 5f, 3f),
                new PlatformDef(38f, 2.5f, 3f),
                new PlatformDef(44f, 5f, 3f),
                new PlatformDef(48f, 2.5f, 3f),
            },
            enemies = new[]
            {
                new Vector2(8f, 0f),
                new Vector2(25f, 0f),
                new Vector2(41f, 0f),
                new Vector2(57f, 0f),
            },
            cameraMin = new Vector2(0f, -4f),
            cameraMax = new Vector2(64f, 12f),
        };

        public static LevelLayout Level02 => new LevelLayout
        {
            sceneName = SceneNames.Level02,
            width = 72f,
            spawn = new Vector2(2f, 1f),
            goal = new Vector2(69f, 0f),
            grounds = new[]
            {
                new GroundSeg(0f, 10f, 0f),
                new GroundSeg(13f, 22f, 0f),
                new GroundSeg(25f, 34f, 0f),
                new GroundSeg(37.5f, 46f, 0f),
                new GroundSeg(49.5f, 58f, 0f),
                new GroundSeg(61f, 72f, 0f),
            },
            platforms = new[]
            {
                new PlatformDef(11f, 2.5f, 3f),
                new PlatformDef(17.5f, 4.5f, 3f),
                new PlatformDef(23f, 2.5f, 3f),
                new PlatformDef(30f, 5f, 3f),
                new PlatformDef(35f, 2.5f, 3f),
                new PlatformDef(42f, 4.5f, 3f),
                new PlatformDef(47f, 2.5f, 3f),
                new PlatformDef(54f, 5f, 3f),
                new PlatformDef(59f, 2.5f, 3f),
            },
            enemies = new[]
            {
                new Vector2(6f, 0f),
                new Vector2(18f, 0f),
                new Vector2(30f, 0f),
                new Vector2(42f, 0f),
                new Vector2(54f, 0f),
                new Vector2(67f, 0f),
            },
            cameraMin = new Vector2(0f, -4f),
            cameraMax = new Vector2(72f, 12f),
        };

        // ---------- 序列化赋值简写 ----------

        static void SetRef(Object t, string f, Object v) => GameSetupUtil.SetRef(t, f, v);
        static void SetFloat(Object t, string f, float v) => GameSetupUtil.SetFloat(t, f, v);
        static void SetVector2(Object t, string f, Vector2 v) => GameSetupUtil.SetVector2(t, f, v);
        static void SetInt(Object t, string f, int v) => GameSetupUtil.SetInt(t, f, v);
        static void EnsureFolder(string folder) => PixelArtGenerator.EnsureFolder(folder);

        // ---------- 相机 ----------

        static GameObject BuildCamera(LevelLayout layout, Transform parent)
        {
            GameObject go = new GameObject("Main Camera");
            go.transform.SetParent(parent, false);
            go.tag = "MainCamera";

            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.36f, 0.56f, 0.74f);

            go.transform.position = new Vector3(layout.spawn.x, layout.spawn.y + 1.5f, -10f);
            go.AddComponent<AudioListener>();

            CameraFollow follow = go.AddComponent<CameraFollow>();
            SetFloat(follow, "lookAheadY", 1.4f);
            SetFloat(follow, "lookAheadX", 1.6f);
            SetVector2(follow, "boundsMin", layout.cameraMin);
            SetVector2(follow, "boundsMax", layout.cameraMax);

            go.AddComponent<CameraShake>();
            return go;
        }

        // ---------- 背景 ----------

        static GameObject CreateSprite(string name, Transform parent, Sprite sprite, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            return go;
        }

        static SpriteRenderer CreateTiledSprite(string name, Transform parent, Sprite sprite, int sortingOrder)
        {
            GameObject go = CreateSprite(name, parent, sprite, sortingOrder);
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            // 必须先切成 Tiled，之后设置 size 才会生效
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            return sr;
        }

        static void AddParallax(GameObject go, float factor)
        {
            ParallaxLayer layer = go.AddComponent<ParallaxLayer>();
            SetFloat(layer, "factor", factor);
        }

        static void BuildBackground(Transform parent, LevelLayout layout, PixelArtGenerator.GeneratedArt art)
        {
            GameObject root = new GameObject("Background");
            root.transform.SetParent(parent, false);

            float centerX = layout.width * 0.5f;
            float coverWidth = layout.width + 40f;

            // 天空直接拉伸铺满，不参与视差
            GameObject sky = CreateSprite("Sky", root.transform, art.sky, -60);
            sky.transform.position = new Vector3(centerX, 9f, 0f);
            float skyUnitWidth = art.sky.rect.width / PixelArtGenerator.PPU;
            sky.transform.localScale = new Vector3(coverWidth / skyUnitWidth, 8f, 1f);

            // 两层远山。factor 越小动得越慢、看起来越远
            SpriteRenderer far = CreateTiledSprite("HillsFar", root.transform, art.hillsFar, -50);
            far.size = new Vector2(coverWidth, art.hillsFar.rect.height / PixelArtGenerator.PPU);
            far.transform.position = new Vector3(centerX, -0.4f, 0f);
            AddParallax(far.gameObject, 0.72f);

            SpriteRenderer near = CreateTiledSprite("HillsNear", root.transform, art.hillsNear, -40);
            near.size = new Vector2(coverWidth, art.hillsNear.rect.height / PixelArtGenerator.PPU);
            near.transform.position = new Vector3(centerX, -0.9f, 0f);
            AddParallax(near.gameObject, 0.55f);
        }

        // ---------- 地形 ----------

        static void BuildGround(Transform parent, PixelArtGenerator.GeneratedArt art, GroundSeg seg)
        {
            float width = seg.right - seg.left;
            float thickness = Mathf.Max(2f, seg.height);
            float centerX = (seg.left + seg.right) * 0.5f;

            GameObject root = new GameObject($"Ground_{Mathf.RoundToInt(seg.left)}_{Mathf.RoundToInt(seg.top)}");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(centerX, seg.top - thickness * 0.5f, 0f);
            root.layer = LayerMask.NameToLayer(GameLayers.Ground);

            // 一个方块碰撞体覆盖整块地面，比逐格拼碰撞体省得多
            BoxCollider2D col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, thickness);
            col.offset = Vector2.zero;

            SpriteRenderer grass = CreateTiledSprite("Grass", root.transform, art.groundTile, 0);
            grass.size = new Vector2(width, 1f);
            grass.transform.localPosition = new Vector3(0f, thickness * 0.5f - 0.5f, 0f);

            float dirtHeight = thickness - 1f;
            if (dirtHeight > 0.01f)
            {
                SpriteRenderer dirt = CreateTiledSprite("Dirt", root.transform, art.dirtTile, -1);
                dirt.size = new Vector2(width, dirtHeight);
                dirt.transform.localPosition = new Vector3(0f, -0.5f - dirtHeight * 0.5f, 0f);
            }
        }

        static void BuildPlatform(Transform parent, PixelArtGenerator.GeneratedArt art, PlatformDef def)
        {
            // 平台贴图是 16x8 像素，在 PPU 16 下正好 1 x 0.5 个世界单位
            const float thickness = 0.5f;

            GameObject root = new GameObject($"Platform_{Mathf.RoundToInt(def.centerX)}");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(def.centerX, def.top - thickness * 0.5f, 0f);
            root.layer = LayerMask.NameToLayer(GameLayers.Ground);

            BoxCollider2D col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(def.width, thickness);
            col.offset = Vector2.zero;

            SpriteRenderer sr = CreateTiledSprite("Sprite", root.transform, art.platformTile, 0);
            sr.size = new Vector2(def.width, thickness);
        }

        static void BuildTerrain(Transform parent, LevelLayout layout, PixelArtGenerator.GeneratedArt art)
        {
            GameObject root = new GameObject("Level");
            root.transform.SetParent(parent, false);

            for (int i = 0; i < layout.grounds.Length; i++) BuildGround(root.transform, art, layout.grounds[i]);
            for (int i = 0; i < layout.platforms.Length; i++) BuildPlatform(root.transform, art, layout.platforms[i]);
        }

        // ---------- 终点与死亡区 ----------

        static void BuildGoal(Transform parent, LevelLayout layout, PixelArtGenerator.GeneratedArt art)
        {
            GameObject root = new GameObject("Goal");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(layout.goal.x, layout.goal.y, 0f);
            GameSetupUtil.SafeSetTag(root, "Goal");

            // 旗子放在子物体上，浮动动画才不会带着碰撞体一起动
            GameObject flag = new GameObject("Flag");
            flag.transform.SetParent(root.transform, false);
            SpriteRenderer sr = flag.AddComponent<SpriteRenderer>();
            sr.sprite = art.goal;
            sr.sortingOrder = 5;

            BoxCollider2D col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.4f, 2.2f);
            col.offset = new Vector2(0f, 1.1f);

            LevelGoal goal = root.AddComponent<LevelGoal>();
            SetRef(goal, "flagVisual", flag.transform);
        }

        static void BuildDeathZone(Transform parent, LevelLayout layout)
        {
            GameObject go = new GameObject("DeathZone");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(layout.width * 0.5f, layout.cameraMin.y - 5f, 0f);

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(layout.width + 80f, 4f);

            go.AddComponent<DeathZone>();
        }

        // ---------- 组装一个完整的关卡场景 ----------

        public static void BuildLevel(LevelLayout layout, PixelArtGenerator.GeneratedArt art,
                                      GameObject playerPrefab, GameObject enemyPrefab)
        {
            EnsureFolder(SceneFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera(layout, null);
            BuildBackground(null, layout, art);
            BuildTerrain(null, layout, art);
            BuildGoal(null, layout, art);
            BuildDeathZone(null, layout);

            GameObject enemiesRoot = new GameObject("Enemies");
            for (int i = 0; i < layout.enemies.Length; i++)
            {
                GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, scene);
                enemy.name = $"Enemy_{i}";
                enemy.transform.SetParent(enemiesRoot.transform, true);
                // 抬高一点点，避免碰撞体初始就嵌在地面里导致抖动
                enemy.transform.position = new Vector3(layout.enemies[i].x, layout.enemies[i].y + 0.05f, 0f);
            }

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "Player";
            player.transform.position = new Vector3(layout.spawn.x, layout.spawn.y, 0f);

            BuildHud();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SceneFolder + "/" + layout.sceneName + ".unity");
            Debug.Log($"[SceneBuilder] 已生成关卡场景 {layout.sceneName}");
        }
    }
}
