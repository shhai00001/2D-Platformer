namespace Platformer
{
    /// <summary>全局游戏状态，决定结算界面显示胜利还是失败。</summary>
    public enum GameState
    {
        /// <summary>还没开始（直接在主菜单场景按 Play 调试时会是这个）。</summary>
        Boot,
        MainMenu,
        Playing,
        /// <summary>通过了非最终关卡，等待进入下一关。</summary>
        LevelCleared,
        Victory,
        Defeat
    }

    /// <summary>一局游戏的统计数据，结算界面用。</summary>
    public struct RunStats
    {
        /// <summary>本局用时（秒）。</summary>
        public float elapsedTime;
        /// <summary>本局死亡次数。</summary>
        public int deaths;
        /// <summary>本局击杀数。</summary>
        public int kills;
        /// <summary>通过的关卡数。</summary>
        public int levelsCleared;
    }
}
