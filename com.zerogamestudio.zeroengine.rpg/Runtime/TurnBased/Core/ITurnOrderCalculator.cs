using System.Collections.Generic;

namespace ZeroEngine.RPG.TurnBased
{
    /// <summary>
    /// 行动顺序计算策略接口 - 支持多种回合制变体
    /// </summary>
    public interface ITurnOrderCalculator
    {
        /// <summary>
        /// 策略名称 (用于 UI 显示和调试)
        /// </summary>
        string StrategyName { get; }

        /// <summary>
        /// 计算当前回合的行动顺序
        /// </summary>
        /// <param name="combatants">所有参战单位</param>
        /// <param name="context">战斗上下文</param>
        /// <returns>按行动顺序排列的单位列表</returns>
        IEnumerable<ITurnBasedCombatant> CalculateOrder(
            IEnumerable<ITurnBasedCombatant> combatants,
            TurnBasedBattleContext context);

        /// <summary>
        /// 获取预览行动顺序 (用于 UI 显示未来几个回合的行动顺序)
        /// </summary>
        /// <param name="combatants">所有参战单位</param>
        /// <param name="previewTurns">预览回合数</param>
        /// <returns>预览的行动顺序列表</returns>
        IEnumerable<ITurnBasedCombatant> GetFutureOrder(
            IEnumerable<ITurnBasedCombatant> combatants,
            int previewTurns);

        /// <summary>
        /// 当有单位行动后更新内部状态 (某些系统如ATB需要)
        /// </summary>
        /// <param name="combatant">刚行动的单位</param>
        /// <param name="context">战斗上下文</param>
        void OnCombatantActed(ITurnBasedCombatant combatant, TurnBasedBattleContext context);

        /// <summary>
        /// 重置计算器状态 (战斗开始时调用)
        /// </summary>
        void Reset();
    }
}
