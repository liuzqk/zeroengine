// ============================================================================
// JobSkillSO.cs
// 职业技能定义 (ScriptableObject)
// 创建于: 2026-01-07
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Character.Job
{
    /// <summary>
    /// 职业技能配置
    /// 定义职业可学习的技能及其解锁条件
    /// </summary>
    [CreateAssetMenu(fileName = "JobSkill", menuName = "ZeroEngine/Job/Job Skill")]
    public class JobSkillSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("技能唯一 ID")]
        public string SkillId;

        [Tooltip("技能显示名称")]
        public string DisplayName;

        [Tooltip("技能描述")]
        [TextArea(2, 4)]
        public string Description;

        [Tooltip("技能图标")]
        public Sprite Icon;

        [Header("学习条件")]
        [Tooltip("所需职业等级")]
        [Range(1, 99)]
        public int RequiredJobLevel = 1;

        [Tooltip("学习消耗 JP (Job Points)")]
        [Min(0)]
        public int JPCost = 100;

        [Tooltip("前置技能 ID 列表")]
        public List<string> PrerequisiteSkillIds = new List<string>();

        [Header("技能类型")]
        [Tooltip("技能分类")]
        public JobSkillCategory Category = JobSkillCategory.Active;

        [Tooltip("是否为职业专属 (换职业后不可使用)")]
        public bool IsJobExclusive = false;

        [Tooltip("是否可精通 (永久保留即使换职业)")]
        public bool CanMaster = true;

        [Tooltip("精通所需 SP")]
        [Min(0)]
        public int MasterySPCost = 500;

        [Header("技能数据")]
        [Tooltip("关联的技能数据 ID (对接 AbilitySystem)")]
        public string AbilityDataId;

        [Tooltip("技能属性修正")]
        public List<SkillStatModifier> StatModifiers = new List<SkillStatModifier>();

        [Header("解锁条件 (可选)")]
        [Tooltip("额外解锁条件")]
        [SerializeReference]
        public List<IJobSkillCondition> UnlockConditions = new List<IJobSkillCondition>();

        /// <summary>
        /// 检查是否满足学习条件
        /// </summary>
        /// <param name="jobLevel">当前职业等级</param>
        /// <param name="currentJP">当前 JP</param>
        /// <param name="learnedSkills">已学习技能</param>
        /// <returns>是否可学习</returns>
        public bool CanLearn(int jobLevel, int currentJP, HashSet<string> learnedSkills)
        {
            // 等级检查
            if (jobLevel < RequiredJobLevel)
                return false;

            // JP 检查
            if (currentJP < JPCost)
                return false;

            // 前置技能检查
            foreach (var prereq in PrerequisiteSkillIds)
            {
                if (!learnedSkills.Contains(prereq))
                    return false;
            }

            // 额外条件检查
            foreach (var condition in UnlockConditions)
            {
                if (condition != null && !condition.IsMet())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 检查是否满足精通条件
        /// </summary>
        public bool CanMasterSkill(int currentSP)
        {
            return CanMaster && currentSP >= MasterySPCost;
        }
    }

    /// <summary>
    /// 技能分类
    /// </summary>
    public enum JobSkillCategory
    {
        /// <summary>主动技能 (战斗中使用)</summary>
        Active,

        /// <summary>被动技能 (自动生效)</summary>
        Passive,

        /// <summary>支援技能 (装备后生效)</summary>
        Support,

        /// <summary>EX 技能 (终极技能)</summary>
        Ultimate,

        /// <summary>特殊技能 (探索/交互)</summary>
        Special
    }

    /// <summary>
    /// 技能属性修正
    /// </summary>
    [Serializable]
    public class SkillStatModifier
    {
        [Tooltip("属性名称")]
        public string StatName;

        [Tooltip("修正类型")]
        public SkillModifierType ModifierType = SkillModifierType.Flat;

        [Tooltip("修正值")]
        public float Value;
    }

    /// <summary>
    /// 修正类型
    /// </summary>
    public enum SkillModifierType
    {
        /// <summary>固定值</summary>
        Flat,

        /// <summary>百分比加成</summary>
        Percent,

        /// <summary>百分比乘算</summary>
        Multiplier
    }

    /// <summary>
    /// 技能解锁条件接口
    /// </summary>
    public interface IJobSkillCondition
    {
        /// <summary>条件描述</summary>
        string Description { get; }

        /// <summary>检查条件是否满足</summary>
        bool IsMet();
    }

    /// <summary>
    /// 任务完成条件
    /// </summary>
    [Serializable]
    public class QuestCompleteCondition : IJobSkillCondition
    {
        [Tooltip("任务 ID")]
        public string QuestId;

        public string Description => $"完成任务: {QuestId}";

        public bool IsMet()
        {
            // 对接 QuestManager
            // return QuestManager.Instance?.IsQuestCompleted(QuestId) ?? false;
            return true; // 默认通过
        }
    }

    /// <summary>
    /// 好感度等级条件
    /// </summary>
    [Serializable]
    public class RelationshipCondition : IJobSkillCondition
    {
        [Tooltip("NPC ID")]
        public string NpcId;

        [Tooltip("所需好感度等级")]
        public int RequiredLevel = 3;

        public string Description => $"与 {NpcId} 好感度达到 {RequiredLevel}";

        public bool IsMet()
        {
            // 对接 RelationshipManager
            // return RelationshipManager.Instance?.GetLevel(NpcId) >= RequiredLevel;
            return true;
        }
    }

    /// <summary>
    /// 击败敌人条件
    /// </summary>
    [Serializable]
    public class DefeatEnemyCondition : IJobSkillCondition
    {
        [Tooltip("敌人 ID (空则计算所有)")]
        public string EnemyId;

        [Tooltip("击败数量")]
        public int RequiredCount = 1;

        public string Description => string.IsNullOrEmpty(EnemyId)
            ? $"击败 {RequiredCount} 个敌人"
            : $"击败 {EnemyId} {RequiredCount} 次";

        public bool IsMet()
        {
            // 对接统计系统
            return true;
        }
    }
}
