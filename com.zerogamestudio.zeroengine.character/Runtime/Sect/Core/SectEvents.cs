// ============================================================================
// SectEvents.cs
// 门派事件系统
// 创建于: 2026-01-09
// ============================================================================

using System;

namespace ZeroEngine.Character.Sect
{
    /// <summary>
    /// 门派事件常量
    /// 用于 EventManager 的事件名称
    /// </summary>
    public static class SectEvents
    {
        /// <summary>加入门派</summary>
        public const string OnSectJoined = "Sect.Joined";

        /// <summary>离开门派</summary>
        public const string OnSectLeft = "Sect.Left";

        /// <summary>被逐出门派</summary>
        public const string OnSectExpelled = "Sect.Expelled";

        /// <summary>职位晋升</summary>
        public const string OnRankPromoted = "Sect.RankPromoted";

        /// <summary>职位降级</summary>
        public const string OnRankDemoted = "Sect.RankDemoted";

        /// <summary>贡献度变化</summary>
        public const string OnContributionChanged = "Sect.ContributionChanged";

        /// <summary>声望变化</summary>
        public const string OnReputationChanged = "Sect.ReputationChanged";

        /// <summary>学习门派武学</summary>
        public const string OnSectMartialArtLearned = "Sect.MartialArtLearned";

        /// <summary>门派关系变化</summary>
        public const string OnSectRelationChanged = "Sect.RelationChanged";

        /// <summary>门派任务完成</summary>
        public const string OnSectQuestCompleted = "Sect.QuestCompleted";
    }

    /// <summary>
    /// 加入门派事件参数
    /// </summary>
    public struct SectJoinedEventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId;

        /// <summary>门派类型</summary>
        public SectType SectType;

        /// <summary>初始职位</summary>
        public SectRank InitialRank;

        /// <summary>是否为重新加入</summary>
        public bool IsRejoin;
    }

    /// <summary>
    /// 离开门派事件参数
    /// </summary>
    public struct SectLeftEventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId;

        /// <summary>门派类型</summary>
        public SectType SectType;

        /// <summary>离开原因</summary>
        public SectLeaveReason Reason;

        /// <summary>详细说明</summary>
        public string Details;
    }

    /// <summary>
    /// 离开门派原因
    /// </summary>
    public enum SectLeaveReason
    {
        /// <summary>主动退出</summary>
        Voluntary,

        /// <summary>被逐出</summary>
        Expelled,

        /// <summary>叛出</summary>
        Betrayed,

        /// <summary>门派解散</summary>
        SectDisbanded,

        /// <summary>转投他派</summary>
        SwitchedSect
    }

    /// <summary>
    /// 职位变化事件参数
    /// </summary>
    public struct SectRankChangedEventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId;

        /// <summary>门派类型</summary>
        public SectType SectType;

        /// <summary>旧职位</summary>
        public SectRank OldRank;

        /// <summary>新职位</summary>
        public SectRank NewRank;

        /// <summary>是否为晋升</summary>
        public bool IsPromotion => (int)NewRank > (int)OldRank;
    }

    /// <summary>
    /// 贡献度变化事件参数
    /// </summary>
    public struct SectContributionChangedEventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId;

        /// <summary>门派类型</summary>
        public SectType SectType;

        /// <summary>旧贡献度</summary>
        public int OldValue;

        /// <summary>新贡献度</summary>
        public int NewValue;

        /// <summary>变化量</summary>
        public int Delta => NewValue - OldValue;

        /// <summary>变化原因</summary>
        public ContributionChangeReason Reason;
    }

    /// <summary>
    /// 贡献度变化原因
    /// </summary>
    public enum ContributionChangeReason
    {
        /// <summary>完成任务</summary>
        QuestCompleted,

        /// <summary>门派活动</summary>
        SectActivity,

        /// <summary>捐献物品</summary>
        Donation,

        /// <summary>学习武学消耗</summary>
        LearnMartialArt,

        /// <summary>兑换物品消耗</summary>
        Exchange,

        /// <summary>惩罚扣除</summary>
        Punishment,

        /// <summary>其他</summary>
        Other
    }

    /// <summary>
    /// 门派武学学习事件参数
    /// </summary>
    public struct SectMartialArtLearnedEventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId;

        /// <summary>门派类型</summary>
        public SectType SectType;

        /// <summary>武学 ID</summary>
        public string MartialArtId;

        /// <summary>消耗的贡献度</summary>
        public int ContributionCost;
    }

    /// <summary>
    /// 门派关系变化事件参数
    /// </summary>
    public struct SectRelationChangedEventArgs
    {
        /// <summary>门派 A</summary>
        public SectType SectA;

        /// <summary>门派 B</summary>
        public SectType SectB;

        /// <summary>旧关系</summary>
        public SectRelationType OldRelation;

        /// <summary>新关系</summary>
        public SectRelationType NewRelation;
    }
}
