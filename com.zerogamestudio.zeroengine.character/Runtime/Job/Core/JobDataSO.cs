// ============================================================================
// JobDataSO.cs
// 职业数据定义 (ScriptableObject)
// 创建于: 2026-01-07
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Character.Job
{
    /// <summary>
    /// 职业配置数据
    /// 定义职业的所有属性、技能、限制
    /// </summary>
    [CreateAssetMenu(fileName = "JobData", menuName = "ZeroEngine/Job/Job Data")]
    public class JobDataSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("职业类型")]
        public JobType JobType = JobType.None;

        [Tooltip("职业分类")]
        public JobCategory Category = JobCategory.Basic;

        [Tooltip("职业显示名称")]
        public string DisplayName;

        [Tooltip("职业描述")]
        [TextArea(2, 5)]
        public string Description;

        [Tooltip("职业图标")]
        public Sprite Icon;

        [Tooltip("职业立绘")]
        public Sprite Portrait;

        [Header("属性成长")]
        [Tooltip("基础属性加成 (装备此职业时)")]
        public JobStatBonus BaseStatBonus = new JobStatBonus();

        [Tooltip("每级属性成长")]
        public JobStatBonus LevelUpBonus = new JobStatBonus();

        [Tooltip("最大职业等级")]
        [Range(1, 99)]
        public int MaxJobLevel = 99;

        [Header("武器与技能")]
        [Tooltip("可用武器类型")]
        public WeaponCategory AllowedWeapons = WeaponCategory.None;

        [Tooltip("职业技能列表")]
        public List<JobSkillSO> Skills = new List<JobSkillSO>();

        [Tooltip("职业专属被动 (换职业后失效)")]
        public List<JobPassiveSO> ExclusivePassives = new List<JobPassiveSO>();

        [Header("解锁条件")]
        [Tooltip("是否初始可用")]
        public bool IsUnlockedByDefault = true;

        [Tooltip("解锁条件 (IsUnlockedByDefault=false 时生效)")]
        [SerializeReference]
        public List<IJobUnlockCondition> UnlockConditions = new List<IJobUnlockCondition>();

        [Header("特殊能力")]
        [Tooltip("职业独有指令 (如商人的'收购'、盗贼的'偷窃')")]
        public string UniqueCommandId;

        [Tooltip("独有指令描述")]
        public string UniqueCommandDescription;

        [Header("视觉效果")]
        [Tooltip("职业颜色主题")]
        public Color ThemeColor = Color.white;

        [Tooltip("转职动画 ID")]
        public string JobChangeAnimationId;

        // ===== 运行时 API =====

        /// <summary>
        /// 获取指定等级的属性加成
        /// </summary>
        public JobStatBonus GetStatBonusAtLevel(int level)
        {
            var bonus = new JobStatBonus();

            // 基础加成
            bonus.HP = BaseStatBonus.HP + LevelUpBonus.HP * (level - 1);
            bonus.MP = BaseStatBonus.MP + LevelUpBonus.MP * (level - 1);
            bonus.Attack = BaseStatBonus.Attack + LevelUpBonus.Attack * (level - 1);
            bonus.Defense = BaseStatBonus.Defense + LevelUpBonus.Defense * (level - 1);
            bonus.MagicAttack = BaseStatBonus.MagicAttack + LevelUpBonus.MagicAttack * (level - 1);
            bonus.MagicDefense = BaseStatBonus.MagicDefense + LevelUpBonus.MagicDefense * (level - 1);
            bonus.Speed = BaseStatBonus.Speed + LevelUpBonus.Speed * (level - 1);
            bonus.Luck = BaseStatBonus.Luck + LevelUpBonus.Luck * (level - 1);

            return bonus;
        }

        /// <summary>
        /// 获取指定等级可学习的技能
        /// </summary>
        public List<JobSkillSO> GetSkillsAtLevel(int level)
        {
            var result = new List<JobSkillSO>();
            foreach (var skill in Skills)
            {
                if (skill != null && skill.RequiredJobLevel <= level)
                {
                    result.Add(skill);
                }
            }
            return result;
        }

        /// <summary>
        /// 检查是否可以使用指定武器
        /// </summary>
        public bool CanUseWeapon(WeaponCategory weapon)
        {
            return (AllowedWeapons & weapon) != 0;
        }

        /// <summary>
        /// 计算升到指定等级所需的总 JP
        /// </summary>
        public int GetRequiredJPForLevel(int targetLevel)
        {
            // 默认公式: 100 * level * (level + 1) / 2
            // Level 1->2: 100, Level 2->3: 200, ...
            int total = 0;
            for (int i = 1; i < targetLevel; i++)
            {
                total += GetJPForNextLevel(i);
            }
            return total;
        }

        /// <summary>
        /// 获取从当前等级升级所需 JP
        /// </summary>
        public int GetJPForNextLevel(int currentLevel)
        {
            // 可覆盖的公式
            return 100 * currentLevel;
        }

        /// <summary>
        /// 检查解锁条件
        /// </summary>
        public bool CheckUnlockConditions()
        {
            if (IsUnlockedByDefault)
                return true;

            foreach (var condition in UnlockConditions)
            {
                if (condition != null && !condition.IsMet())
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 职业属性加成
    /// </summary>
    [Serializable]
    public class JobStatBonus
    {
        [Header("基础属性")]
        public int HP;
        public int MP;
        public int Attack;
        public int Defense;
        public int MagicAttack;
        public int MagicDefense;
        public int Speed;
        public int Luck;

        [Header("特殊属性")]
        [Range(0f, 1f)]
        public float CriticalRate;

        [Range(0f, 1f)]
        public float EvasionRate;

        [Range(0f, 1f)]
        public float AccuracyRate;

        /// <summary>
        /// 转换为属性修正列表 (对接 StatSystem)
        /// </summary>
        public List<(string statName, float value)> ToStatModifiers()
        {
            var modifiers = new List<(string, float)>();

            if (HP != 0) modifiers.Add(("HP", HP));
            if (MP != 0) modifiers.Add(("MP", MP));
            if (Attack != 0) modifiers.Add(("Attack", Attack));
            if (Defense != 0) modifiers.Add(("Defense", Defense));
            if (MagicAttack != 0) modifiers.Add(("MagicAttack", MagicAttack));
            if (MagicDefense != 0) modifiers.Add(("MagicDefense", MagicDefense));
            if (Speed != 0) modifiers.Add(("Speed", Speed));
            if (Luck != 0) modifiers.Add(("Luck", Luck));
            if (CriticalRate != 0) modifiers.Add(("CriticalRate", CriticalRate));
            if (EvasionRate != 0) modifiers.Add(("EvasionRate", EvasionRate));
            if (AccuracyRate != 0) modifiers.Add(("AccuracyRate", AccuracyRate));

            return modifiers;
        }
    }

    /// <summary>
    /// 职业被动配置 (简化版)
    /// </summary>
    [CreateAssetMenu(fileName = "JobPassive", menuName = "ZeroEngine/Job/Job Passive")]
    public class JobPassiveSO : ScriptableObject
    {
        [Tooltip("被动 ID")]
        public string PassiveId;

        [Tooltip("被动名称")]
        public string DisplayName;

        [Tooltip("被动描述")]
        [TextArea(2, 4)]
        public string Description;

        [Tooltip("被动图标")]
        public Sprite Icon;

        [Tooltip("解锁所需职业等级")]
        [Range(1, 99)]
        public int RequiredJobLevel = 1;

        [Tooltip("被动效果 (对接 BuffSystem)")]
        public string BuffId;

        [Tooltip("属性修正")]
        public List<SkillStatModifier> StatModifiers = new List<SkillStatModifier>();
    }

    // ===== 职业解锁条件 =====

    /// <summary>
    /// 职业解锁条件接口
    /// </summary>
    public interface IJobUnlockCondition
    {
        /// <summary>条件描述</summary>
        string Description { get; }

        /// <summary>检查是否满足</summary>
        bool IsMet();
    }

    /// <summary>
    /// 角色等级条件
    /// </summary>
    [Serializable]
    public class CharacterLevelCondition : IJobUnlockCondition
    {
        [Tooltip("所需角色等级")]
        public int RequiredLevel = 1;

        public string Description => $"角色等级达到 {RequiredLevel}";

        public bool IsMet()
        {
            // 对接角色等级系统
            return true;
        }
    }

    /// <summary>
    /// 职业等级条件 (需要其他职业达到指定等级)
    /// </summary>
    [Serializable]
    public class JobLevelCondition : IJobUnlockCondition
    {
        [Tooltip("所需职业")]
        public JobType RequiredJob;

        [Tooltip("所需职业等级")]
        public int RequiredLevel = 10;

        public string Description => $"{RequiredJob} 等级达到 {RequiredLevel}";

        public bool IsMet()
        {
            // 对接 JobManager
            // return JobManager.Instance?.GetJobLevel(RequiredJob) >= RequiredLevel;
            return true;
        }
    }

    /// <summary>
    /// 多职业条件 (需要多个职业达到指定等级)
    /// </summary>
    [Serializable]
    public class MultiJobCondition : IJobUnlockCondition
    {
        [Tooltip("条件列表")]
        public List<JobLevelRequirement> Requirements = new List<JobLevelRequirement>();

        public string Description
        {
            get
            {
                var parts = new List<string>();
                foreach (var req in Requirements)
                {
                    parts.Add($"{req.Job} Lv.{req.Level}");
                }
                return string.Join(", ", parts);
            }
        }

        public bool IsMet()
        {
            foreach (var req in Requirements)
            {
                // if (JobManager.Instance?.GetJobLevel(req.Job) < req.Level)
                //     return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 职业等级需求
    /// </summary>
    [Serializable]
    public class JobLevelRequirement
    {
        public JobType Job;
        public int Level = 10;
    }

    /// <summary>
    /// 道具持有条件
    /// </summary>
    [Serializable]
    public class ItemRequiredCondition : IJobUnlockCondition
    {
        [Tooltip("所需道具 ID")]
        public string ItemId;

        [Tooltip("是否消耗道具")]
        public bool ConsumeItem = true;

        public string Description => $"持有道具: {ItemId}";

        public bool IsMet()
        {
            // 对接 InventoryManager
            // return InventoryManager.Instance?.HasItem(ItemId) ?? false;
            return true;
        }
    }

    /// <summary>
    /// 祭坛解锁条件 (八方旅人高级职业解锁方式)
    /// </summary>
    [Serializable]
    public class ShrineUnlockCondition : IJobUnlockCondition
    {
        [Tooltip("祭坛 ID")]
        public string ShrineId;

        [Tooltip("需要击败的守护者 ID")]
        public string GuardianId;

        public string Description => $"在 {ShrineId} 击败 {GuardianId}";

        public bool IsMet()
        {
            // 对接统计/事件系统
            return true;
        }
    }
}
