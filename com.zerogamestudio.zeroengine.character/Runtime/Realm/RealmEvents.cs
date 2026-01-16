// ============================================================================
// RealmEvents.cs
// 境界事件系统
// 创建于: 2026-01-09
// ============================================================================

using System;

namespace ZeroEngine.Character.Realm
{
    /// <summary>
    /// 境界事件常量
    /// </summary>
    public static class RealmEvents
    {
        /// <summary>境界突破</summary>
        public const string OnRealmBreakthrough = "Realm.Breakthrough";

        /// <summary>境界跌落</summary>
        public const string OnRealmRegression = "Realm.Regression";

        /// <summary>修为变化</summary>
        public const string OnCultivationChanged = "Realm.CultivationChanged";

        /// <summary>突破尝试</summary>
        public const string OnBreakthroughAttempt = "Realm.BreakthroughAttempt";

        /// <summary>走火入魔</summary>
        public const string OnDeviationStarted = "Realm.DeviationStarted";

        /// <summary>走火入魔恢复</summary>
        public const string OnDeviationEnded = "Realm.DeviationEnded";

        /// <summary>遇到瓶颈</summary>
        public const string OnBottleneck = "Realm.Bottleneck";
    }

    /// <summary>
    /// 境界突破事件参数
    /// </summary>
    public struct RealmBreakthroughEventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId;

        /// <summary>旧境界</summary>
        public RealmType OldRealm;

        /// <summary>新境界</summary>
        public RealmType NewRealm;

        /// <summary>突破结果</summary>
        public BreakthroughResult Result;

        /// <summary>尝试次数</summary>
        public int Attempts;
    }

    /// <summary>
    /// 修为变化事件参数
    /// </summary>
    public struct CultivationChangedEventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId;

        /// <summary>旧修为</summary>
        public int OldValue;

        /// <summary>新修为</summary>
        public int NewValue;

        /// <summary>变化量</summary>
        public int Delta => NewValue - OldValue;

        /// <summary>修为来源</summary>
        public CultivationSource Source;
    }

    /// <summary>
    /// 修为来源
    /// </summary>
    public enum CultivationSource
    {
        /// <summary>打坐修炼</summary>
        Meditation,

        /// <summary>战斗</summary>
        Combat,

        /// <summary>使用丹药</summary>
        Pill,

        /// <summary>任务奖励</summary>
        Quest,

        /// <summary>顿悟</summary>
        Enlightenment,

        /// <summary>传功</summary>
        Transfer,

        /// <summary>消耗 (负值)</summary>
        Consumption,

        /// <summary>其他</summary>
        Other
    }

    /// <summary>
    /// 突破尝试事件参数
    /// </summary>
    public struct BreakthroughAttemptEventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId;

        /// <summary>当前境界</summary>
        public RealmType CurrentRealm;

        /// <summary>目标境界</summary>
        public RealmType TargetRealm;

        /// <summary>成功率</summary>
        public int SuccessChance;

        /// <summary>结果</summary>
        public BreakthroughResult Result;
    }

    /// <summary>
    /// 走火入魔事件参数
    /// </summary>
    public struct DeviationEventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId;

        /// <summary>当前境界</summary>
        public RealmType CurrentRealm;

        /// <summary>持续时间 (秒)</summary>
        public float Duration;

        /// <summary>原因</summary>
        public DeviationCause Cause;
    }

    /// <summary>
    /// 走火入魔原因
    /// </summary>
    public enum DeviationCause
    {
        /// <summary>突破失败</summary>
        BreakthroughFailed,

        /// <summary>修炼冲突武学</summary>
        ConflictingArts,

        /// <summary>强行突破</summary>
        ForcedBreakthrough,

        /// <summary>外部攻击</summary>
        ExternalAttack,

        /// <summary>其他</summary>
        Other
    }
}
