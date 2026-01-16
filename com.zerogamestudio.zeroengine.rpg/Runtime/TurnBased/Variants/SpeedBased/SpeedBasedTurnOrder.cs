using System.Collections.Generic;
using System.Linq;

namespace ZeroEngine.RPG.TurnBased.Variants
{
    /// <summary>
    /// 速度排序制行动顺序计算器 - 八方旅人风格
    /// 每回合按速度从高到低排序，速度高的先行动
    /// </summary>
    public class SpeedBasedTurnOrder : ITurnOrderCalculator
    {
        /// <summary>
        /// 速度相同时是否随机打乱顺序
        /// </summary>
        public bool RandomizeOnTie { get; set; } = true;

        /// <summary>
        /// 策略名称
        /// </summary>
        public string StrategyName => "Speed Based (Octopath Style)";

        /// <summary>
        /// 计算当前回合的行动顺序
        /// </summary>
        public IEnumerable<ITurnBasedCombatant> CalculateOrder(
            IEnumerable<ITurnBasedCombatant> combatants,
            TurnBasedBattleContext context)
        {
            var aliveUnits = combatants.Where(c => c.IsAlive).ToList();

            if (RandomizeOnTie)
            {
                // 使用稳定排序，速度相同时保持随机顺序
                var shuffled = aliveUnits.OrderBy(_ => UnityEngine.Random.value).ToList();
                return shuffled.OrderByDescending(c => c.Speed);
            }
            else
            {
                // 纯速度排序
                return aliveUnits.OrderByDescending(c => c.Speed);
            }
        }

        /// <summary>
        /// 获取预览行动顺序
        /// </summary>
        public IEnumerable<ITurnBasedCombatant> GetFutureOrder(
            IEnumerable<ITurnBasedCombatant> combatants,
            int previewTurns)
        {
            var aliveUnits = combatants.Where(c => c.IsAlive).ToList();
            var result = new List<ITurnBasedCombatant>();

            // 对于速度排序制，每回合顺序相同
            var orderedUnits = aliveUnits.OrderByDescending(c => c.Speed).ToList();

            for (int turn = 0; turn < previewTurns; turn++)
            {
                result.AddRange(orderedUnits);
            }

            return result;
        }

        /// <summary>
        /// 单位行动后的回调 (速度排序制不需要特殊处理)
        /// </summary>
        public void OnCombatantActed(ITurnBasedCombatant combatant, TurnBasedBattleContext context)
        {
            // 速度排序制每回合重新计算，不需要维护状态
        }

        /// <summary>
        /// 重置计算器状态
        /// </summary>
        public void Reset()
        {
            // 速度排序制无状态，不需要重置
        }
    }
}
