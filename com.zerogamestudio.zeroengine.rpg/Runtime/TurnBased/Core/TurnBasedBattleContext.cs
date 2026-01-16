using System.Collections.Generic;
using System.Linq;

namespace ZeroEngine.RPG.TurnBased
{
    /// <summary>
    /// 回合制战斗上下文 - 存储单场战斗的所有状态信息
    /// </summary>
    public class TurnBasedBattleContext
    {
        /// <summary>
        /// 当前回合数 (从1开始)
        /// </summary>
        public int TurnCount { get; set; } = 0;

        /// <summary>
        /// 当前战斗阶段
        /// </summary>
        public BattlePhase CurrentPhase { get; set; } = BattlePhase.None;

        /// <summary>
        /// 战斗结果
        /// </summary>
        public BattleResult Result { get; set; } = BattleResult.None;

        /// <summary>
        /// 当前行动单位
        /// </summary>
        public ITurnBasedCombatant CurrentActor { get; set; }

        /// <summary>
        /// 所有参战单位
        /// </summary>
        public List<ITurnBasedCombatant> AllCombatants { get; } = new();

        /// <summary>
        /// 玩家方单位
        /// </summary>
        public List<ITurnBasedCombatant> PlayerUnits { get; } = new();

        /// <summary>
        /// 敌方单位
        /// </summary>
        public List<ITurnBasedCombatant> EnemyUnits { get; } = new();

        /// <summary>
        /// 当前回合的行动队列
        /// </summary>
        public Queue<ITurnBasedCombatant> ActionQueue { get; } = new();

        /// <summary>
        /// 本场战斗执行过的所有行动记录
        /// </summary>
        public List<BattleAction> ActionHistory { get; } = new();

        /// <summary>
        /// 最近一次行动 (没有则为 null)
        /// </summary>
        public BattleAction LastAction => ActionHistory.Count > 0
            ? ActionHistory[ActionHistory.Count - 1]
            : null;

        /// <summary>
        /// 自定义数据存储 (用于扩展)
        /// </summary>
        public Dictionary<string, object> CustomData { get; } = new();

        /// <summary>
        /// 战斗是否正在进行
        /// </summary>
        public bool IsInProgress => CurrentPhase != BattlePhase.None && CurrentPhase != BattlePhase.BattleEnd;

        /// <summary>
        /// 获取存活的玩家单位
        /// </summary>
        public IEnumerable<ITurnBasedCombatant> AlivePlayerUnits => PlayerUnits.Where(u => u.IsAlive);

        /// <summary>
        /// 获取存活的敌方单位
        /// </summary>
        public IEnumerable<ITurnBasedCombatant> AliveEnemyUnits => EnemyUnits.Where(u => u.IsAlive);

        /// <summary>
        /// 获取所有存活单位
        /// </summary>
        public IEnumerable<ITurnBasedCombatant> AliveUnits => AllCombatants.Where(u => u.IsAlive);

        /// <summary>
        /// 初始化战斗
        /// </summary>
        public void Initialize(IEnumerable<ITurnBasedCombatant> playerUnits,
            IEnumerable<ITurnBasedCombatant> enemyUnits)
        {
            Clear();

            foreach (var unit in playerUnits)
            {
                PlayerUnits.Add(unit);
                AllCombatants.Add(unit);
            }

            foreach (var unit in enemyUnits)
            {
                EnemyUnits.Add(unit);
                AllCombatants.Add(unit);
            }

            TurnCount = 0;
            CurrentPhase = BattlePhase.Initialization;
            Result = BattleResult.None;
        }

        /// <summary>
        /// 清理战斗上下文
        /// </summary>
        public void Clear()
        {
            TurnCount = 0;
            CurrentPhase = BattlePhase.None;
            Result = BattleResult.None;
            CurrentActor = null;

            AllCombatants.Clear();
            PlayerUnits.Clear();
            EnemyUnits.Clear();
            ActionQueue.Clear();
            ActionHistory.Clear();
            CustomData.Clear();
        }

        /// <summary>
        /// 记录行动
        /// </summary>
        public void RecordAction(BattleAction action)
        {
            ActionHistory.Add(action);
        }

        /// <summary>
        /// 检查战斗是否应该结束
        /// </summary>
        public BattleResult CheckBattleEnd()
        {
            if (!AlivePlayerUnits.Any())
            {
                return BattleResult.Defeat;
            }

            if (!AliveEnemyUnits.Any())
            {
                return BattleResult.Victory;
            }

            return BattleResult.None;
        }

        /// <summary>
        /// 设置自定义数据
        /// </summary>
        public void SetCustomData<T>(string key, T value)
        {
            CustomData[key] = value;
        }

        /// <summary>
        /// 获取自定义数据
        /// </summary>
        public bool TryGetCustomData<T>(string key, out T value)
        {
            if (CustomData.TryGetValue(key, out var obj) && obj is T typedValue)
            {
                value = typedValue;
                return true;
            }
            value = default;
            return false;
        }
    }
}
