namespace ZeroEngine.AutoBattle.Grid
{
    /// <summary>
    /// 战斗单位接口，定义单位在棋盘上的基本行为
    /// </summary>
    public interface IBattleUnit
    {
        /// <summary>
        /// 单位唯一ID
        /// </summary>
        string UnitId { get; }

        /// <summary>
        /// 单位所属阵营
        /// </summary>
        BattleTeam Team { get; }

        /// <summary>
        /// 当前所在格子
        /// </summary>
        GridCell CurrentCell { get; }

        /// <summary>
        /// 单位是否存活
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// 设置单位所在格子（由 GridBoard 调用）
        /// </summary>
        void SetCell(GridCell cell);
    }

    /// <summary>
    /// 战斗阵营
    /// </summary>
    public enum BattleTeam
    {
        /// <summary>
        /// 玩家方
        /// </summary>
        Player,

        /// <summary>
        /// 敌方
        /// </summary>
        Enemy
    }
}
