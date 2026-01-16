// ============================================================================
// ZeroEngine v2.6.0 - Battle Reward System
// 战斗结算管理器
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using ZeroEngine.Core;
using Debug = UnityEngine.Debug;
#if ZEROENGINE_PERSISTENCE
using ZeroEngine.Persistence;
#endif

namespace ZeroEngine.RPG.BattleReward
{
    /// <summary>
    /// 战斗结算管理器 - 计算和分配战斗奖励
    /// </summary>
    public class BattleRewardManager : MonoSingleton<BattleRewardManager>
#if ZEROENGINE_PERSISTENCE
        , ISaveable
#endif
    {
        [Header("配置")]
        [Tooltip("默认奖励配置")]
        [SerializeField] private BattleRewardConfigSO _defaultConfig;

        [Tooltip("敌人奖励数据库 (EnemyId -> RewardConfig)")]
        [SerializeField] private List<EnemyRewardConfig> _enemyRewardDatabase = new List<EnemyRewardConfig>();

        // 运行时缓存
        private Dictionary<string, EnemyRewardConfig> _rewardLookup;

        // 事件
        /// <summary>奖励计算完成</summary>
        public event Action<BattleRewardResult> OnRewardCalculated;

        /// <summary>奖励发放完成</summary>
        public event Action<BattleRewardResult> OnRewardGranted;

        /// <summary>角色升级</summary>
        public event Action<LevelUpInfo> OnLevelUp;

        /// <summary>职业升级</summary>
        public event Action<JobLevelUpInfo> OnJobLevelUp;

#if ZEROENGINE_PERSISTENCE
        // ISaveable 实现
        public string SaveKey => "BattleReward";
#endif

        // ========================================
        // Unity 生命周期
        // ========================================

        protected override void Awake()
        {
            base.Awake();
            BuildRewardLookup();
        }

        private void BuildRewardLookup()
        {
            _rewardLookup = new Dictionary<string, EnemyRewardConfig>();
            foreach (var config in _enemyRewardDatabase)
            {
                if (!string.IsNullOrEmpty(config.EnemyId))
                {
                    _rewardLookup[config.EnemyId] = config;
                }
            }
        }

        // ========================================
        // 公共 API
        // ========================================

        /// <summary>
        /// 开始战斗结算
        /// </summary>
        /// <param name="defeatedEnemies">击败的敌人列表</param>
        /// <param name="partyMembers">参战队员 ID 列表</param>
        /// <param name="averagePartyLevel">队伍平均等级</param>
        /// <param name="noDamageClear">是否无伤通关</param>
        /// <param name="fullClear">是否全歼</param>
        public BattleRewardResult Calculate(
            List<DefeatedEnemyInfo> defeatedEnemies,
            List<string> partyMembers,
            int averagePartyLevel,
            bool noDamageClear = false,
            bool fullClear = false)
        {
            var config = _defaultConfig;
            if (config == null)
            {
                Debug.LogError("[BattleRewardManager] 缺少默认奖励配置");
                return null;
            }

            var result = new BattleRewardResult
            {
                NoDamageClear = noDamageClear,
                FullClear = fullClear
            };

            // 1. 计算总经验/金币/JP
            int totalExp = 0;
            int totalGold = 0;
            int totalJP = 0;

            foreach (var enemy in defeatedEnemies)
            {
                var rewardConfig = GetEnemyRewardConfig(enemy.EnemyId);
                if (rewardConfig == null) continue;

                // 基础值 (根据敌人等级缩放)
                int baseExp = rewardConfig.CalculateExp(enemy.Level);
                int baseGold = rewardConfig.CalculateGold(enemy.Level);
                int baseJP = rewardConfig.CalculateJP(enemy.Level);

                // 等级修正
                float levelMod = config.CalculateLevelModifier(averagePartyLevel, enemy.Level);

                // 敌人类型倍率 (精英/Boss)
                float expTypeMod = config.GetEnemyTypeMultiplier(enemy.IsElite, enemy.IsBoss, true);
                float goldTypeMod = config.GetEnemyTypeMultiplier(enemy.IsElite, enemy.IsBoss, false);

                // 计算最终值
                totalExp += Mathf.RoundToInt(baseExp * levelMod * expTypeMod);
                totalGold += Mathf.RoundToInt(baseGold * goldTypeMod * config.GoldMultiplier);
                totalJP += baseJP;

                // 处理掉落
                ProcessDrops(enemy, rewardConfig, result);
            }

            // 2. 完美通关奖励
            if (noDamageClear)
            {
                totalExp = Mathf.RoundToInt(totalExp * config.NoDamageExpBonus);
            }
            if (fullClear)
            {
                totalGold = Mathf.RoundToInt(totalGold * config.FullClearGoldBonus);
            }

            result.TotalExp = totalExp;
            result.TotalGold = totalGold;
            result.TotalJP = totalJP;

            // 3. 分配给队员
            DistributeExp(result, config, partyMembers);
            DistributeJP(result, config, partyMembers);

            // 触发事件
            OnRewardCalculated?.Invoke(result);

            return result;
        }

        /// <summary>
        /// 发放奖励 (实际添加到各系统)
        /// </summary>
        public void GrantReward(BattleRewardResult result)
        {
            if (result == null) return;

            // 发放经验
            foreach (var kvp in result.ExpPerMember)
            {
                var memberId = kvp.Key;
                var exp = kvp.Value;

                // 调用外部接口添加经验
                var levelUp = GrantExpToMember(memberId, exp);
                if (levelUp.HasValue)
                {
                    result.LevelUps.Add(levelUp.Value);
                    OnLevelUp?.Invoke(levelUp.Value);
                }
            }

            // 发放 JP
            foreach (var kvp in result.JPPerMember)
            {
                var memberId = kvp.Key;
                var jp = kvp.Value;

                // 调用外部接口添加 JP
                var jobLevelUp = GrantJPToMember(memberId, jp);
                if (jobLevelUp.HasValue)
                {
                    result.JobLevelUps.Add(jobLevelUp.Value);
                    OnJobLevelUp?.Invoke(jobLevelUp.Value);
                }
            }

            // 发放金币
            GrantGold(result.TotalGold);

            // 发放掉落物品
            foreach (var drop in result.DroppedItems)
            {
                GrantItem(drop.ItemId, drop.Amount);
            }

            // 触发事件
            OnRewardGranted?.Invoke(result);

            LogDebugFormat("奖励已发放: Exp={0}, Gold={1}, JP={2}, Items={3}",
                result.TotalExp, result.TotalGold, result.TotalJP, result.DroppedItems.Count);
        }

        /// <summary>
        /// 一键计算并发放奖励
        /// </summary>
        public BattleRewardResult CalculateAndGrant(
            List<DefeatedEnemyInfo> defeatedEnemies,
            List<string> partyMembers,
            int averagePartyLevel,
            bool noDamageClear = false,
            bool fullClear = false)
        {
            var result = Calculate(defeatedEnemies, partyMembers, averagePartyLevel, noDamageClear, fullClear);
            if (result != null)
            {
                GrantReward(result);
            }
            return result;
        }

        /// <summary>
        /// 获取敌人奖励配置
        /// </summary>
        public EnemyRewardConfig GetEnemyRewardConfig(string enemyId)
        {
            if (_rewardLookup.TryGetValue(enemyId, out var config))
            {
                return config;
            }
            return null;
        }

        /// <summary>
        /// 注册敌人奖励配置
        /// </summary>
        public void RegisterEnemyReward(EnemyRewardConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.EnemyId)) return;

            _rewardLookup[config.EnemyId] = config;
            if (!_enemyRewardDatabase.Contains(config))
            {
                _enemyRewardDatabase.Add(config);
            }
        }

        // ========================================
        // 内部方法 - 分配逻辑
        // ========================================

        private void DistributeExp(BattleRewardResult result, BattleRewardConfigSO config, List<string> members)
        {
            if (members == null || members.Count == 0) return;

            switch (config.ExpMode)
            {
                case ExpDistributionMode.EqualSplit:
                    int expPerMember = result.TotalExp / members.Count;
                    foreach (var member in members)
                    {
                        result.ExpPerMember[member] = expPerMember;
                    }
                    break;

                case ExpDistributionMode.FullToAll:
                    foreach (var member in members)
                    {
                        result.ExpPerMember[member] = result.TotalExp;
                    }
                    break;

                case ExpDistributionMode.DamageContribution:
                    // 需要外部伤害统计，暂时平均分配
                    goto case ExpDistributionMode.EqualSplit;

                case ExpDistributionMode.KillBased:
                    // 需要外部击杀统计，暂时平均分配
                    goto case ExpDistributionMode.EqualSplit;

                case ExpDistributionMode.SurvivorsOnly:
                    // 需要外部存活状态，暂时全员分配
                    goto case ExpDistributionMode.FullToAll;
            }
        }

        private void DistributeJP(BattleRewardResult result, BattleRewardConfigSO config, List<string> members)
        {
            if (members == null || members.Count == 0) return;

            switch (config.JPMode)
            {
                case JPDistributionMode.EqualSplit:
                    int jpPerMember = result.TotalJP / members.Count;
                    foreach (var member in members)
                    {
                        result.JPPerMember[member] = jpPerMember;
                    }
                    break;

                case JPDistributionMode.FullToAll:
                    foreach (var member in members)
                    {
                        result.JPPerMember[member] = result.TotalJP;
                    }
                    break;

                case JPDistributionMode.ActiveJobsOnly:
                    // 需要职业系统集成，暂时全员分配
                    goto case JPDistributionMode.FullToAll;
            }
        }

        private void ProcessDrops(DefeatedEnemyInfo enemy, EnemyRewardConfig config, BattleRewardResult result)
        {
            // 固定掉落
            foreach (var drop in config.FixedDrops)
            {
                if (drop.RollDrop())
                {
                    result.DroppedItems.Add(new ItemDropResult
                    {
                        ItemId = drop.ItemId,
                        Amount = drop.GetRandomAmount(),
                        SourceEnemyId = enemy.EnemyId
                    });
                }
            }

            // LootTable 掉落 (需要 LootTableManager 集成)
            if (!string.IsNullOrEmpty(config.LootTableId))
            {
                // TODO: 集成 LootTableManager
                // var lootResults = LootTableManager.Instance?.Roll(config.LootTableId);
            }
        }

        // ========================================
        // 外部系统集成点 (需要项目实现)
        // ========================================

        /// <summary>
        /// 给角色添加经验 (需要外部实现)
        /// </summary>
        protected virtual LevelUpInfo? GrantExpToMember(string memberId, int exp)
        {
            // 默认实现: 仅打印日志
            LogDebugFormat("角色 {0} 获得 {1} 经验", memberId, exp);

            // 项目需要重写此方法，调用角色系统
            // 示例:
            // var member = PartyManager.Instance?.GetMember(memberId);
            // if (member != null)
            // {
            //     int oldLevel = member.Level;
            //     member.AddExp(exp);
            //     if (member.Level > oldLevel)
            //     {
            //         return new LevelUpInfo { MemberId = memberId, OldLevel = oldLevel, NewLevel = member.Level };
            //     }
            // }

            return null;
        }

        /// <summary>
        /// 给角色添加 JP (需要外部实现)
        /// </summary>
        protected virtual JobLevelUpInfo? GrantJPToMember(string memberId, int jp)
        {
            // 默认实现: 仅打印日志
            LogDebugFormat("角色 {0} 获得 {1} JP", memberId, jp);

            // 项目需要重写此方法，调用职业系统
            // 示例:
            // var member = PartyManager.Instance?.GetMember(memberId);
            // if (member != null)
            // {
            //     var job = member.CurrentJob;
            //     int oldLevel = job.Level;
            //     job.AddJP(jp);
            //     if (job.Level > oldLevel)
            //     {
            //         return new JobLevelUpInfo { MemberId = memberId, JobType = job.JobType, OldLevel = oldLevel, NewLevel = job.Level };
            //     }
            // }

            return null;
        }

        /// <summary>
        /// 添加金币 (需要外部实现)
        /// </summary>
        protected virtual void GrantGold(int amount)
        {
            LogDebugFormat("获得 {0} 金币", amount);

            // 项目需要重写此方法
            // 示例: CurrencyManager.Instance?.AddCurrency("Gold", amount);
        }

        /// <summary>
        /// 添加物品 (需要外部实现)
        /// </summary>
        protected virtual void GrantItem(string itemId, int amount)
        {
            LogDebugFormat("获得物品 {0} x{1}", itemId, amount);

            // 项目需要重写此方法
            // 示例: InventoryManager.Instance?.AddItem(itemId, amount);
        }

#if ZEROENGINE_PERSISTENCE
        // ========================================
        // ISaveable 实现
        // ========================================

        public object ExportSaveData()
        {
            // 如果需要保存战斗统计数据
            return new BattleRewardSaveData();
        }

        public void ImportSaveData(object data)
        {
            if (data is BattleRewardSaveData saveData)
            {
                // 恢复数据
            }
        }

        public void ResetToDefault()
        {
            // 重置到默认状态
        }

        [Serializable]
        private class BattleRewardSaveData
        {
            // 可以存储累计战斗统计等
            public int TotalBattlesWon;
            public int TotalExpEarned;
            public int TotalGoldEarned;
        }
#endif

        // ========================================
        // 条件编译 Debug 日志 (零 GC)
        // ========================================

        /// <summary>
        /// 条件编译格式化 Debug 日志 (仅 Development Build 有效)
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void LogDebugFormat(string format, object arg0)
        {
            Debug.LogFormat("[BattleRewardManager] " + format, arg0);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void LogDebugFormat(string format, object arg0, object arg1)
        {
            Debug.LogFormat("[BattleRewardManager] " + format, arg0, arg1);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void LogDebugFormat(string format, object arg0, object arg1, object arg2, object arg3)
        {
            Debug.LogFormat("[BattleRewardManager] " + format, arg0, arg1, arg2, arg3);
        }
    }

    /// <summary>
    /// 击败敌人信息
    /// </summary>
    [Serializable]
    public struct DefeatedEnemyInfo
    {
        /// <summary>敌人 ID</summary>
        public string EnemyId;

        /// <summary>敌人等级</summary>
        public int Level;

        /// <summary>是否为精英</summary>
        public bool IsElite;

        /// <summary>是否为 Boss</summary>
        public bool IsBoss;

        /// <summary>击杀者 ID (用于按击杀分配)</summary>
        public string KillerId;

        /// <summary>受到的总伤害 (用于按贡献分配)</summary>
        public Dictionary<string, float> DamageContribution;
    }
}
