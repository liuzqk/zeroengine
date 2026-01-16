// ============================================================================
// SectDataSO.cs
// 门派数据配置 (ScriptableObject)
// 创建于: 2026-01-09
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Character.Sect
{
    /// <summary>
    /// 门派数据配置
    /// 定义门派的基础信息、武学列表、入门条件等
    /// </summary>
    [CreateAssetMenu(fileName = "New Sect", menuName = "ZeroEngine/Character/Sect/Sect Data")]
    public class SectDataSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("门派类型 ID")]
        public SectType sectType = SectType.None;

        [Tooltip("门派名称")]
        public string sectName;

        [Tooltip("门派描述")]
        [TextArea(3, 6)]
        public string description;

        [Tooltip("门派分类")]
        public SectCategory category = SectCategory.Neutral;

        [Tooltip("门派图标")]
        public Sprite icon;

        [Header("门派特性")]
        [Tooltip("门派属性加成")]
        public SectStatBonus statBonus;

        [Tooltip("门派特殊能力")]
        public List<string> specialAbilities = new List<string>();

        [Header("武学配置")]
        [Tooltip("门派武学列表 (按等级分类)")]
        public List<SectMartialArtEntry> martialArts = new List<SectMartialArtEntry>();

        [Header("入门条件")]
        [Tooltip("是否需要特殊条件才能加入")]
        public bool requiresUnlock = false;

        [Tooltip("入门条件描述")]
        [TextArea(2, 4)]
        public string unlockConditionDesc;

        [Tooltip("最低境界要求")]
        public int minRealmLevel = 0;

        [Tooltip("声望要求 (正数为好感, 负数为恶名)")]
        public int reputationRequired = 0;

        [Header("晋升配置")]
        [Tooltip("各职位晋升所需贡献度")]
        public List<RankRequirement> rankRequirements = new List<RankRequirement>();

        [Header("门派关系")]
        [Tooltip("友好门派")]
        public List<SectType> friendlySects = new List<SectType>();

        [Tooltip("敌对门派")]
        public List<SectType> hostileSects = new List<SectType>();

        /// <summary>
        /// 获取指定职位的晋升要求
        /// </summary>
        public RankRequirement GetRankRequirement(SectRank rank)
        {
            return rankRequirements.Find(r => r.rank == rank);
        }

        /// <summary>
        /// 获取指定访问等级的武学列表
        /// </summary>
        public List<SectMartialArtEntry> GetMartialArtsByAccess(SectMartialArtAccess access)
        {
            return martialArts.FindAll(m => m.accessLevel == access);
        }

        /// <summary>
        /// 检查是否可以学习指定武学
        /// </summary>
        public bool CanLearnMartialArt(string martialArtId, SectRank currentRank, int contribution)
        {
            var entry = martialArts.Find(m => m.martialArtId == martialArtId);
            if (entry == null) return false;

            // 检查职位要求
            if ((int)currentRank < (int)entry.requiredRank) return false;

            // 检查贡献度要求
            if (contribution < entry.contributionCost) return false;

            return true;
        }
    }

    /// <summary>
    /// 门派属性加成
    /// </summary>
    [Serializable]
    public class SectStatBonus
    {
        [Tooltip("生命值加成 (百分比)")]
        [Range(-50f, 100f)]
        public float healthPercent = 0f;

        [Tooltip("内力/真气加成 (百分比)")]
        [Range(-50f, 100f)]
        public float energyPercent = 0f;

        [Tooltip("攻击力加成 (百分比)")]
        [Range(-50f, 100f)]
        public float attackPercent = 0f;

        [Tooltip("防御力加成 (百分比)")]
        [Range(-50f, 100f)]
        public float defensePercent = 0f;

        [Tooltip("速度加成 (百分比)")]
        [Range(-50f, 100f)]
        public float speedPercent = 0f;

        [Tooltip("暴击率加成 (固定值)")]
        [Range(-20f, 50f)]
        public float critRateFlat = 0f;

        [Tooltip("内功伤害加成 (百分比)")]
        [Range(-50f, 100f)]
        public float innerDamagePercent = 0f;

        [Tooltip("外功伤害加成 (百分比)")]
        [Range(-50f, 100f)]
        public float outerDamagePercent = 0f;
    }

    /// <summary>
    /// 门派武学条目
    /// </summary>
    [Serializable]
    public class SectMartialArtEntry
    {
        [Tooltip("武学 ID (引用 MartialArtDataSO)")]
        public string martialArtId;

        [Tooltip("武学名称 (显示用)")]
        public string displayName;

        [Tooltip("访问等级")]
        public SectMartialArtAccess accessLevel = SectMartialArtAccess.Basic;

        [Tooltip("学习所需职位")]
        public SectRank requiredRank = SectRank.OuterDisciple;

        [Tooltip("学习消耗贡献度")]
        public int contributionCost = 100;

        [Tooltip("是否为镇派之宝")]
        public bool isSignature = false;
    }

    /// <summary>
    /// 职位晋升要求
    /// </summary>
    [Serializable]
    public class RankRequirement
    {
        [Tooltip("目标职位")]
        public SectRank rank;

        [Tooltip("所需贡献度")]
        public int contributionRequired;

        [Tooltip("所需境界等级")]
        public int realmLevelRequired;

        [Tooltip("所需完成任务数")]
        public int questsRequired;

        [Tooltip("额外条件描述")]
        public string additionalCondition;
    }
}
