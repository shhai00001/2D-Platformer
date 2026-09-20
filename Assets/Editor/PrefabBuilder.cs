using UnityEditor;
using UnityEngine;
using Platformer;

namespace Platformer.EditorTools
{
    /// <summary>
    /// 生成玩家与敌人的 Prefab，并把所有 [SerializeField] 私有字段接好线。
    /// 因为字段是私有的，这里统一用 SerializedObject 赋值，不为了生成工具把字段改成 public。
    /// </summary>
    public static class PrefabBuilder
    {
        public const string PrefabFolder = "Assets/Generated/Prefabs";
        public const string VisualName = "Visual";

        // ---------- 序列化字段赋值（实现在 GameSetupUtil，这里只做简写） ----------

        static void SetRef(Object target, string field, Object value) => GameSetupUtil.SetRef(target, field, value);
        static void SetInt(Object target, string field, int value) => GameSetupUtil.SetInt(target, field, value);
        static void SetFloat(Object target, string field, float value) => GameSetupUtil.SetFloat(target, field, value);
        static void SetEnum(Object target, string field, int enumIndex) => GameSetupUtil.SetEnum(target, field, enumIndex);
        static void SetVector2(Object target, string field, Vector2 value) => GameSetupUtil.SetVector2(target, field, value);

        static int MaskOf(params string[] layerNames) => GameSetupUtil.MaskOf(layerNames);

        static void EnsureFolder(string folder) => PixelArtGenerator.EnsureFolder(folder);

        // ---------- 玩家 ----------

        public static GameObject BuildPlayerPrefab(PixelArtGenerator.GeneratedArt art,
                                                   RuntimeAnimatorController controller)
        {
            EnsureFolder(PrefabFolder);
            string path = PrefabFolder + "/Player.prefab";

            GameObject root = new GameObject("Player");
            GameSetupUtil.SafeSetTag(root, "Player");
            root.layer = LayerMask.NameToLayer(GameLayers.Player);

            // --- 刚体与碰撞体 ---
            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 4f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sharedMaterial = GetOrCreateFrictionlessMaterial();

            CapsuleCollider2D col = root.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            col.size = new Vector2(0.66f, 1.34f);
            col.offset = new Vector2(0f, 0.68f);

            // --- 视觉子物体：Animator 的 Sprite 曲线路径写死成 "Visual"，名字不能改 ---
            GameObject visualGo = new GameObject(VisualName);
            visualGo.transform.SetParent(root.transform, false);
            SpriteRenderer sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sprite = art.player.idle[0];
            sr.sortingOrder = 10;

            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            // --- 检测点 ---
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(root.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, 0.03f, 0f);

            // --- 攻击判定盒：空物体，不需要 Collider2D ---
            GameObject hitboxGo = new GameObject("Hitbox");
            hitboxGo.transform.SetParent(root.transform, false);
            hitboxGo.transform.localPosition = Vector3.zero;
            AttackHitbox hitbox = hitboxGo.AddComponent<AttackHitbox>();

            // --- 逻辑组件 ---
            PlayerInputReader input = root.AddComponent<PlayerInputReader>();
            CharacterVisual visual = root.AddComponent<CharacterVisual>();
            PlayerAttack attack = root.AddComponent<PlayerAttack>();
            PlayerController controllerComp = root.AddComponent<PlayerController>();
            PlayerHealth health = root.AddComponent<PlayerHealth>();

            // --- 接线 ---
            SetRef(visual, "spriteRenderer", sr);
            SetRef(attack, "hitbox", hitbox);

            SetRef(controllerComp, "animator", animator);
            SetRef(controllerComp, "visual", visual);
            SetRef(controllerComp, "attack", attack);
            SetRef(controllerComp, "health", health);
            SetRef(controllerComp, "groundCheck", groundCheck.transform);
            SetVector2(controllerComp, "groundCheckSize", new Vector2(0.6f, 0.14f));
            SetInt(controllerComp, "groundMask", MaskOf(GameLayers.Ground));

            SetEnum(health, "team", (int)Team.Player);
            SetInt(health, "maxHealth", 5);
            SetFloat(health, "invulnerableDuration", 0.9f);
            SetRef(health, "visual", visual);

            // 打敌人：只检测 Enemy 层
            SetVector2(hitbox, "size", new Vector2(1.2f, 0.9f));
            SetVector2(hitbox, "localOffset", new Vector2(0.8f, 0.72f));
            SetInt(hitbox, "targetMask", MaskOf(GameLayers.Enemy));
            SetInt(hitbox, "damage", 1);

            _ = input;   // PlayerInputReader 不需要额外接线

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static PhysicsMaterial2D GetOrCreateFrictionlessMaterial()
        {
            EnsureFolder(PrefabFolder);
            string path = PrefabFolder + "/Frictionless.physicsMaterial2D";
            PhysicsMaterial2D mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (mat == null)
            {
                mat = new PhysicsMaterial2D("Frictionless");
                AssetDatabase.CreateAsset(mat, path);
            }
            // 摩擦力为 0，贴墙时按住方向键才不会挂在墙上
            mat.friction = 0f;
            mat.bounciness = 0f;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------- 敌人 ----------

        public static GameObject BuildEnemyPrefab(PixelArtGenerator.GeneratedArt art,
                                                  RuntimeAnimatorController controller)
        {
            EnsureFolder(PrefabFolder);
            string path = PrefabFolder + "/Enemy.prefab";

            GameObject root = new GameObject("Enemy");
            GameSetupUtil.SafeSetTag(root, "Enemy");
            root.layer = LayerMask.NameToLayer(GameLayers.Enemy);

            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 4f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sharedMaterial = GetOrCreateFrictionlessMaterial();

            CapsuleCollider2D col = root.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            col.size = new Vector2(0.78f, 0.78f);
            col.offset = new Vector2(0f, 0.39f);

            GameObject visualGo = new GameObject(VisualName);
            visualGo.transform.SetParent(root.transform, false);
            SpriteRenderer sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sprite = art.enemy.idle[0];
            sr.sortingOrder = 10;

            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            // 悬崖探测点摆在前方、脚底略下方：
            // 站着时它会落在实体地面里，走到边缘就悬空 —— 这就是"前方没有路了"的信号
            GameObject ledgeCheck = new GameObject("LedgeCheck");
            ledgeCheck.transform.SetParent(root.transform, false);
            ledgeCheck.transform.localPosition = new Vector3(0.55f, -0.12f, 0f);

            GameObject wallCheck = new GameObject("WallCheck");
            wallCheck.transform.SetParent(root.transform, false);
            wallCheck.transform.localPosition = new Vector3(0.5f, 0.4f, 0f);

            GameObject hitboxGo = new GameObject("Hitbox");
            hitboxGo.transform.SetParent(root.transform, false);
            hitboxGo.transform.localPosition = Vector3.zero;
            AttackHitbox hitbox = hitboxGo.AddComponent<AttackHitbox>();

            CharacterVisual visual = root.AddComponent<CharacterVisual>();
            EnemyAttack attack = root.AddComponent<EnemyAttack>();
            EnemyController controllerComp = root.AddComponent<EnemyController>();
            EnemyHealth health = root.AddComponent<EnemyHealth>();

            SetRef(visual, "spriteRenderer", sr);
            SetRef(attack, "hitbox", hitbox);

            SetRef(controllerComp, "animator", animator);
            SetRef(controllerComp, "visual", visual);
            SetRef(controllerComp, "attack", attack);
            SetRef(controllerComp, "health", health);
            SetRef(controllerComp, "ledgeCheck", ledgeCheck.transform);
            SetRef(controllerComp, "wallCheck", wallCheck.transform);
            SetVector2(controllerComp, "ledgeCheckSize", new Vector2(0.22f, 0.2f));
            SetVector2(controllerComp, "wallCheckSize", new Vector2(0.2f, 0.6f));
            SetInt(controllerComp, "groundMask", MaskOf(GameLayers.Ground));
            SetInt(controllerComp, "obstacleMask", MaskOf(GameLayers.Ground));
            SetInt(controllerComp, "playerMask", MaskOf(GameLayers.Player));
            SetInt(controllerComp, "sightBlockerMask", MaskOf(GameLayers.Ground));

            SetEnum(health, "team", (int)Team.Enemy);
            SetInt(health, "maxHealth", 3);
            SetFloat(health, "invulnerableDuration", 0.35f);
            SetFloat(health, "corpseLifetime", 1.5f);
            SetRef(health, "visual", visual);

            // 打玩家：只检测 Player 层
            SetVector2(hitbox, "size", new Vector2(1.1f, 0.85f));
            SetVector2(hitbox, "localOffset", new Vector2(0.65f, 0.42f));
            SetInt(hitbox, "targetMask", MaskOf(GameLayers.Player));
            SetInt(hitbox, "damage", 1);
            SetFloat(hitbox, "knockbackForce", 7.5f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
