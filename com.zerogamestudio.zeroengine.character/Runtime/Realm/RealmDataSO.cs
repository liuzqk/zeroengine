// ============================================================================
// RealmDataSO.cs
// 境界数据配置 (ScriptableObject)
// 创建于: 2026-01-09
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Character.Realm
{
    /// <summary>
    /// 境界数据配置
    /// 定义境界的属性加成、突破条件等
    /// </summary>
    [CreateAssetMenu(fileName = "New Realm", menuName = "ZeroEngine/Character/Realm/Realm Data")]
    public class RealmDataSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("境界类型")]
        public RealmType realmType = RealmType.None;

        [Tooltip("境界名称")]
        public string realmName;

        [Tooltip("境界描述")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("境界图标")]
        public Sprite icon;

        [Header("属性加成")]
        [Tooltip("基础属性倍率 (相对于初始值)")]
        public RealmStatMultiplier statMultiplier;

        [Header("修炼配置")]
        [Tooltip("突破所需修为")]
        public int cultivationRequired = 1000;

        [Tooltip("修炼速度倍率")]
        [Range(0.1f, 5f)]
        public float cultivationSpeedMultiplier = 1f;

        [Tooltip("每日自然恢复修为")]
        public int dailyCultivationRegen = 0;

        [Header("突破配置")]
        [Tooltip("基础突破成功率 (0-100)")]
        [Range(0, 100)]
        public int baseBreakthroughChance = 50;

        [Tooltip("突破失败是否可能跌境")]
        public bool canRegress = false;

        [Tooltip("跌境概率 (突破失败时)")]
        [Range(0, 100)]
        public int regressionChance = 10;

        [Tooltip("突破失败是否可能走火入魔")]
        public bool canDeviate = false;

        [Tooltip("走火入魔概率 (突破失败时)")]
        [Range(0, 100)]
        public int deviationChance = 5;

        [Header("突破条件")]
        [Tooltip("突破条件列表")]
        public List<BreakthroughRequirement> requirements = new List<BreakthroughRequirement>();

        [Header("解锁内容")]
        [Tooltip("解锁的能力/特性")]
        public List<string> unlockedAbilities = new List<string>();

        [Tooltip("解锁的武学类型")]
        public List<MartialArts.MartialArtType> unlockedMartialArtTypes = new List<MartialArts.MartialArtType>();

        /// <summary>
        /// 计算实际突破成功率
        /// </summary>
        /// <param name="bonusChance">额外成功率加成</param>
        public int CalculateBreakthroughChance(int bonusChance = 0)
        {
            return Mathf.Clamp(baseBreakthroughChance + bonusChance, 0, 100);
        }

        /// <summary>
        /// 检查是否满足突破条件
        /// </summary>
        public bool CheckRequirements(int currentCultivation, Func<BreakthroughRequirement, bool> customCheck = null)
        {
            // 检查修为
            if (currentCultivation < cultivationRequired)
                return false;

            // 检查其他条件
            foreach (var req in requirements)
            {
                if (customCheck != null && !customCheck(req))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 境界属性倍率
    /// </summary>
    [Serializable]
    public class RealmStatMultiplier
    {
        [Tooltip("生命值倍率")]
        [Range(1f, 10f)]
        public float healthMultiplier = 1f;

        [Tooltip("内力/真气倍率")]
        [Range(1f, 10f)]
        public float energyMultiplier = 1f;

        [Tooltip("攻击力倍率")]
        [Range(1f, 10f)]
        public float attackMultiplier = 1f;

        [Tooltip("防御力倍率")]
        [Range(1f, 10f)]
        public float defenseMultiplier = 1f;

        [Tooltip("速度倍率")]
        [Range(1f, 5f)]
        public float speedMultiplier = 1f;

        [Tooltip("暴击率加成 (固定值)")]
        [Range(0f, 50f)]
        public float critRateBonus = 0f;

        [Tooltip("暴击伤害加成 (百分比)")]
        [Range(0f, 200f)]
        public float critDamageBonus = 0f;
    }

    /// <summary>
    /// 突破条件
    /// </summary>
    [Serializable]
    public class BreakthroughRequirement
    {
        [Tooltip("条件类型")]
        public BreakthroughRequirementType type;

        [Tooltip("条件描述")]
        public string description;

        [Tooltip("数值要求")]
        public int valueRequired;

        [Tooltip("字符串要求 (物品 ID/武学 ID 等)")]
        public string stringRequired;
    }

    /// <summary>
    /// 突破条件类型
    /// </summary>
    public enum BreakthroughRequirementType
    {
        /// <summary>修为要求</summary>
        Cultivation,

        /// <summary>物品消耗</summary>
        Item,

        /// <summary>武学要求 (需要某武学达到某层)</summary>
        MartialArt,

        /// <summary>门派职位要求</summary>
        SectRank,

        /// <summary>任务完成</summary>
        Quest,

        /// <summary>特殊条件</summary>
        Special
    }
}
