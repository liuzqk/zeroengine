namespace ZeroEngine.RPG.TurnBased
{
    /// <summary>
    /// 战斗阶段枚举 - 控制回合制战斗流程
    /// </summary>
    public enum BattlePhase
    {
        /// <summary>
        /// 未开始/结束状态
        /// </summary>
        None = 0,

        /// <summary>
        /// 战斗初始化 - 设置参战单位、初始状态
        /// </summary>
        Initialization = 1,

        /// <summary>
        /// 回合开始 - 触发回合开始效果、BP恢复、破盾恢复检查
        /// </summary>
        TurnStart = 2,

        /// <summary>
        /// 玩家输入 - 等待玩家选择行动
        /// </summary>
        PlayerCommand = 3,

        /// <summary>
        /// AI决策 - 敌方AI选择行动
        /// </summary>
        EnemyAI = 4,

        /// <summary>
        /// 行动执行 - 执行选中的行动
        /// </summary>
        ActionExecution = 5,

        /// <summary>
        /// 行动结算 - 处理行动后效果（伤害、Buff等）
        /// </summary>
        ActionResolution = 6,

        /// <summary>
        /// 回合结束 - 触发回合结束效果、状态衰减
        /// </summary>
        TurnEnd = 7,

        /// <summary>
        /// 战斗结束 - 胜利/失败/逃跑
        /// </summary>
        BattleEnd = 8
    }

    /// <summary>
    /// 战斗结果枚举
    /// </summary>
    public enum BattleResult
    {
        /// <summary>战斗进行中</summary>
        None = 0,
        /// <summary>玩家胜利</summary>
        Victory = 1,
        /// <summary>玩家失败</summary>
        Defeat = 2,
        /// <summary>逃跑成功</summary>
        Escape = 3,
        /// <summary>平局/超时</summary>
        Draw = 4
    }

    /// <summary>
    /// 行动类型枚举
    /// </summary>
    public enum BattleActionType
    {
        /// <summary>无操作/跳过</summary>
        None = 0,
        /// <summary>普通攻击</summary>
        Attack = 1,
        /// <summary>使用技能</summary>
        Skill = 2,
        /// <summary>使用物品</summary>
        Item = 3,
        /// <summary>防御</summary>
        Defend = 4,
        /// <summary>逃跑</summary>
        Escape = 5,
        /// <summary>切换位置/阵型</summary>
        Swap = 6,
        /// <summary>特殊行动</summary>
        Special = 7
    }
}
