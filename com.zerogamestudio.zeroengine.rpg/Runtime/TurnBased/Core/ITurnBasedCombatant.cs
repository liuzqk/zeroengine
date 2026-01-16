using ZeroEngine.Combat;

namespace ZeroEngine.RPG.TurnBased
{
    /// <summary>
    /// 回合制战斗单位接口 - 继承自 ICombatant，添加回合制特有属性
    /// </summary>
    public interface ITurnBasedCombatant : ICombatant
    {
        /// <summary>
        /// 速度属性 (用于行动顺序计算)
        /// </summary>
        int Speed { get; }

        /// <summary>
        /// 当前是否可以行动 (考虑眩晕、破盾等状态)
        /// </summary>
        bool CanAct { get; }

        /// <summary>
        /// 是否已在本回合行动
        /// </summary>
        bool HasActed { get; set; }

        /// <summary>
        /// 是否为玩家控制的单位
        /// </summary>
        bool IsPlayerControlled { get; }

        /// <summary>
        /// 当回合开始时调用 (BP恢复、Buff结算等)
        /// </summary>
        void OnTurnStart();

        /// <summary>
        /// 当回合结束时调用 (状态衰减等)
        /// </summary>
        void OnTurnEnd();

        /// <summary>
        /// 重置回合状态 (新一轮开始时)
        /// </summary>
        void ResetTurnState();
    }
}
