// ============================================================================
// ZeroEngine v2.6.0 - Battle Reward System
// 战利品数据定义
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.RPG.BattleReward
{
    /// <summary>
    /// 经验分配模式
    /// </summary>
    public enum ExpDistributionMode
    {
        /// <summary>平均分配给所有参战者</summary>
        EqualSplit,
        /// <summary>每人获得完整经验</summary>
        FullToAll,
        /// <summary>按伤害贡献分配</summary>
        DamageContribution,
        /// <summary>按击杀分配</summary>
        KillBased,
        /// <summary>只给存活者</summary>
        SurvivorsOnly
    }

    /// <summary>
    /// JP 分配模式
    /// </summary>
    public enum JPDistributionMode
    {
        /// <summary>平均分配</summary>
        EqualSplit,
        /// <summary>每人获得完整 JP</summary>
        FullToAll,
        /// <summary>只给主/副职业</summary>
        ActiveJobsOnly
    }

    /// <summary>
    /// 单个敌人的奖励配置
    /// </summary>
    [Serializable]
    public class EnemyRewardConfig
    {
        [Tooltip("敌人 ID")]
        public string EnemyId;

        [Tooltip("基础经验值")]
        public int BaseExp = 100;

        [Tooltip("基础金币")]
        public int BaseGold = 50;

        [Tooltip("基础 JP (职业经验)")]
        public int BaseJP = 10;

        [Tooltip("经验等级缩放系数 (每级增加的比例)")]
        [Range(0f, 0.5f)]
        public float ExpLevelScaling = 0.1f;

        [Tooltip("金币等级缩放系数")]
        [Range(0f, 0.5f)]
        public float GoldLevelScaling = 0.1f;

        [Tooltip("掉落表 ID (用于 LootTableManager)")]
        public string LootTableId;

        [Tooltip("额外掉落物品 (固定掉落)")]
        public List<ItemDropEntry> FixedDrops = new List<ItemDropEntry>();

        /// <summary>
        /// 计算缩放后的经验值
        /// </summary>
        public int CalculateExp(int enemyLevel)
        {
            return Mathf.RoundToInt(BaseExp * (1f + ExpLevelScaling * (enemyLevel - 1)));
        }

        /// <summary>
        /// 计算缩放后的金币
        /// </summary>
        public int CalculateGold(int enemyLevel)
        {
            return Mathf.RoundToInt(BaseGold * (1f + GoldLevelScaling * (enemyLevel - 1)));
        }

        /// <summary>
        /// 计算 JP
        /// </summary>
        public int CalculateJP(int enemyLevel)
        {
            // JP 不随等级缩放，保持固定
            return BaseJP;
        }
    }

    /// <summary>
    /// 物品掉落条目
    /// </summary>
    [Serializable]
    public class ItemDropEntry
    {
        [Tooltip("物品 ID")]
        public string ItemId;

        [Tooltip("数量范围 (最小)")]
        [Min(1)]
        public int MinAmount = 1;

        [Tooltip("数量范围 (最大)")]
        [Min(1)]
        public int MaxAmount = 1;

        [Tooltip("掉落概率")]
        [Range(0f, 1f)]
        public float DropChance = 1f;

        /// <summary>
        /// 获取随机数量
        /// </summary>
        public int GetRandomAmount()
        {
            return UnityEngine.Random.Range(MinAmount, MaxAmount + 1);
        }

        /// <summary>
        /// 检查是否掉落
        /// </summary>
        public bool RollDrop()
        {
            return UnityEngine.Random.value <= DropChance;
        }
    }

    /// <summary>
    /// 战斗结算配置 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "BattleRewardConfig", menuName = "ZeroEngine/RPG/Battle Reward Config")]
    public class BattleRewardConfigSO : ScriptableObject
    {
        [Header("经验配置")]
        [Tooltip("经验分配模式")]
        public ExpDistributionMode ExpMode = ExpDistributionMode.FullToAll;

        [Tooltip("等级差惩罚 (玩家比敌人高时减少经验)")]
        public bool ApplyLevelPenalty = true;

        [Tooltip("每级差减少的经验比例")]
        [Range(0f, 0.2f)]
        public float LevelPenaltyPerLevel = 0.1f;

        [Tooltip("最低经验比例 (不会低于这个值)")]
        [Range(0f, 1f)]
        public float MinExpRatio = 0.1f;

        [Tooltip("等级差奖励 (玩家比敌人低时增加经验)")]
        public bool ApplyLevelBonus = true;

        [Tooltip("每级差增加的经验比例")]
        [Range(0f, 0.3f)]
        public float LevelBonusPerLevel = 0.15f;

        [Tooltip("最高经验比例 (不会高于这个值)")]
        [Range(1f, 3f)]
        public float MaxExpRatio = 2f;

        [Header("JP 配置")]
        [Tooltip("JP 分配模式")]
        public JPDistributionMode JPMode = JPDistributionMode.FullToAll;

        [Tooltip("副职业 JP 比例")]
        [Range(0f, 1f)]
        public float SecondaryJobJPRatio = 0.5f;

        [Header("金币配置")]
        [Tooltip("金币加成 (全局倍率)")]
        [Range(0.5f, 3f)]
        public float GoldMultiplier = 1f;

        [Header("精英/Boss 加成")]
        [Tooltip("精英敌人经验倍率")]
        [Range(1f, 5f)]
        public float EliteExpMultiplier = 2f;

        [Tooltip("精英敌人金币倍率")]
        [Range(1f, 5f)]
        public float EliteGoldMultiplier = 2f;

        [Tooltip("Boss 敌人经验倍率")]
        [Range(1f, 10f)]
        public float BossExpMultiplier = 5f;

        [Tooltip("Boss 敌人金币倍率")]
        [Range(1f, 10f)]
        public float BossGoldMultiplier = 5f;

        [Header("完美通关奖励")]
        [Tooltip("无伤通关经验加成")]
        [Range(1f, 2f)]
        public float NoDamageExpBonus = 1.5f;

        [Tooltip("全歼敌人金币加成")]
        [Range(1f, 2f)]
        public float FullClearGoldBonus = 1.2f;

        // ========================================
        // 计算方法
        // ========================================

        /// <summary>
        /// 计算等级修正系数
        /// </summary>
        public float CalculateLevelModifier(int playerLevel, int enemyLevel)
        {
            int levelDiff = playerLevel - enemyLevel;

            if (levelDiff > 0 && ApplyLevelPenalty)
            {
                // 玩家等级更高，减少经验
                float penalty = 1f - (levelDiff * LevelPenaltyPerLevel);
                return Mathf.Max(penalty, MinExpRatio);
            }
            else if (levelDiff < 0 && ApplyLevelBonus)
            {
                // 敌人等级更高，增加经验
                float bonus = 1f + (Mathf.Abs(levelDiff) * LevelBonusPerLevel);
                return Mathf.Min(bonus, MaxExpRatio);
            }

            return 1f;
        }

        /// <summary>
        /// 获取敌人类型倍率
        /// </summary>
        public float GetEnemyTypeMultiplier(bool isElite, bool isBoss, bool isExp)
        {
            if (isBoss)
            {
                return isExp ? BossExpMultiplier : BossGoldMultiplier;
            }
            if (isElite)
            {
                return isExp ? EliteExpMultiplier : EliteGoldMultiplier;
            }
            return 1f;
        }
    }

    /// <summary>
    /// 战斗结算结果
    /// </summary>
    public class BattleRewardResult
    {
        /// <summary>总经验</summary>
        public int TotalExp;

        /// <summary>总金币</summary>
        public int TotalGold;

        /// <summary>总 JP</summary>
        public int TotalJP;

        /// <summary>掉落物品列表</summary>
        public List<ItemDropResult> DroppedItems = new List<ItemDropResult>();

        /// <summary>每个参战者的经验分配</summary>
        public Dictionary<string, int> ExpPerMember = new Dictionary<string, int>();

        /// <summary>每个参战者的 JP 分配</summary>
        public Dictionary<string, int> JPPerMember = new Dictionary<string, int>();

        /// <summary>是否有角色升级</summary>
        public List<LevelUpInfo> LevelUps = new List<LevelUpInfo>();

        /// <summary>是否有职业升级</summary>
        public List<JobLevelUpInfo> JobLevelUps = new List<JobLevelUpInfo>();

        /// <summary>是否无伤通关</summary>
        public bool NoDamageClear;

        /// <summary>是否全歼</summary>
        public bool FullClear;
    }

    /// <summary>
    /// 物品掉落结果
    /// </summary>
    public struct ItemDropResult
    {
        public string ItemId;
        public int Amount;
        public string SourceEnemyId;
    }

    /// <summary>
    /// 升级信息
    /// </summary>
    public struct LevelUpInfo
    {
        public string MemberId;
        public int OldLevel;
        public int NewLevel;
    }

    /// <summary>
    /// 职业升级信息
    /// </summary>
    public struct JobLevelUpInfo
    {
        public string MemberId;
        public string JobType;
        public int OldLevel;
        public int NewLevel;
    }
}
