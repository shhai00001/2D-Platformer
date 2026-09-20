using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 集中定义项目用到的 Layer 名称，避免在代码里散落魔法字符串。
    /// 这些 Layer 由编辑器工具 Tools/2D闯关游戏/① 一键生成全部 自动创建。
    /// </summary>
    public static class GameLayers
    {
        public const string Ground = "Ground";
        public const string Player = "Player";
        public const string Enemy = "Enemy";

        /// <summary>地面检测（脚下 / 悬崖探测）用的掩码。</summary>
        public static int GroundMask => LayerMask.GetMask(Ground);

        /// <summary>玩家所在层的掩码。</summary>
        public static int PlayerMask => LayerMask.GetMask(Player);

        /// <summary>敌人所在层的掩码。</summary>
        public static int EnemyMask => LayerMask.GetMask(Enemy);

        /// <summary>玩家攻击判定要打的目标：敌人。</summary>
        public static int PlayerAttackTargets => LayerMask.GetMask(Enemy);

        /// <summary>敌人攻击判定要打的目标：玩家。</summary>
        public static int EnemyAttackTargets => LayerMask.GetMask(Player);

        /// <summary>敌人的视野遮挡物：地面/墙壁。</summary>
        public static int SightBlockers => LayerMask.GetMask(Ground);
    }

    /// <summary>场景名常量，配合 GameManager 做场景切换。</summary>
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string Result = "Result";
        public const string Level01 = "Level_01";
        public const string Level02 = "Level_02";

        public static readonly string[] All = { MainMenu, Level01, Level02, Result };
    }

    /// <summary>
    /// Animator 参数名转哈希，比字符串查找快，也避免拼写错误。
    /// 参数由 AnimatorBuilder 自动创建，两边必须保持一致。
    /// </summary>
    public static class AnimParams
    {
        /// <summary>水平速度绝对值，驱动 Idle/Move 切换。</summary>
        public static readonly int Speed = Animator.StringToHash("Speed");
        /// <summary>是否踩在地面上，驱动 Jump/Fall 切换。</summary>
        public static readonly int Grounded = Animator.StringToHash("Grounded");
        /// <summary>垂直速度，用于区分上升与下落。</summary>
        public static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");

        // 一次性触发的动作
        public static readonly int Attack = Animator.StringToHash("Attack");
        public static readonly int Hurt = Animator.StringToHash("Hurt");
        public static readonly int Die = Animator.StringToHash("Die");
    }

    /// <summary>物理层索引缓存，避免每次调用 LayerMask.GetMask 产生字符串开销。</summary>
    public static class LayerIds
    {
        public static int Ground => LayerMask.NameToLayer(GameLayers.Ground);
        public static int Player => LayerMask.NameToLayer(GameLayers.Player);
        public static int Enemy => LayerMask.NameToLayer(GameLayers.Enemy);
    }
}
