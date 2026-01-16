// ============================================================================
// JobEvents.cs
// 职业系统事件定义
// 创建于: 2026-01-07
// ============================================================================

using System;

namespace ZeroEngine.Character.Job
{
    /// <summary>
    /// 职业变更事件参数
    /// </summary>
    public class JobChangedEventArgs : EventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId { get; set; }

        /// <summary>槽位类型</summary>
        public JobSlotType SlotType { get; set; }

        /// <summary>旧职业</summary>
        public JobInstance OldJob { get; set; }

        /// <summary>新职业</summary>
        public JobInstance NewJob { get; set; }

        /// <summary>旧职业类型</summary>
        public JobType OldJobType => OldJob?.JobType ?? JobType.None;

        /// <summary>新职业类型</summary>
        public JobType NewJobType => NewJob?.JobType ?? JobType.None;
    }

    /// <summary>
    /// 职业升级事件参数
    /// </summary>
    public class JobLevelUpEventArgs : EventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId { get; set; }

        /// <summary>职业实例</summary>
        public JobInstance JobInstance { get; set; }

        /// <summary>旧等级</summary>
        public int OldLevel { get; set; }

        /// <summary>新等级</summary>
        public int NewLevel { get; set; }

        /// <summary>升级数</summary>
        public int LevelsGained => NewLevel - OldLevel;

        /// <summary>职业类型</summary>
        public JobType JobType => JobInstance?.JobType ?? JobType.None;
    }

    /// <summary>
    /// 技能学习事件参数
    /// </summary>
    public class SkillLearnedEventArgs : EventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId { get; set; }

        /// <summary>职业实例</summary>
        public JobInstance JobInstance { get; set; }

        /// <summary>学习的技能</summary>
        public JobSkillSO Skill { get; set; }

        /// <summary>技能 ID</summary>
        public string SkillId => Skill?.SkillId;

        /// <summary>职业类型</summary>
        public JobType JobType => JobInstance?.JobType ?? JobType.None;
    }

    /// <summary>
    /// 技能精通事件参数
    /// </summary>
    public class SkillMasteredEventArgs : EventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId { get; set; }

        /// <summary>职业实例</summary>
        public JobInstance JobInstance { get; set; }

        /// <summary>精通的技能</summary>
        public JobSkillSO Skill { get; set; }

        /// <summary>技能 ID</summary>
        public string SkillId => Skill?.SkillId;

        /// <summary>职业类型</summary>
        public JobType JobType => JobInstance?.JobType ?? JobType.None;
    }

    /// <summary>
    /// 职业解锁事件参数
    /// </summary>
    public class JobUnlockedEventArgs : EventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId { get; set; }

        /// <summary>解锁的职业类型</summary>
        public JobType JobType { get; set; }

        /// <summary>职业数据</summary>
        public JobDataSO JobData { get; set; }
    }

    /// <summary>
    /// 支援技能变更事件参数
    /// </summary>
    public class SupportSkillChangedEventArgs : EventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId { get; set; }

        /// <summary>槽位索引</summary>
        public int SlotIndex { get; set; }

        /// <summary>旧技能 ID</summary>
        public string OldSkillId { get; set; }

        /// <summary>新技能 ID</summary>
        public string NewSkillId { get; set; }
    }

    /// <summary>
    /// JP 获得事件参数
    /// </summary>
    public class JPGainedEventArgs : EventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId { get; set; }

        /// <summary>职业类型</summary>
        public JobType JobType { get; set; }

        /// <summary>获得的 JP</summary>
        public int Amount { get; set; }

        /// <summary>当前总 JP</summary>
        public int CurrentJP { get; set; }

        /// <summary>来源 (如: "battle", "quest", "item")</summary>
        public string Source { get; set; }
    }

    /// <summary>
    /// 被动解锁事件参数
    /// </summary>
    public class PassiveUnlockedEventArgs : EventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId { get; set; }

        /// <summary>职业类型</summary>
        public JobType JobType { get; set; }

        /// <summary>解锁的被动</summary>
        public JobPassiveSO Passive { get; set; }
    }
}
