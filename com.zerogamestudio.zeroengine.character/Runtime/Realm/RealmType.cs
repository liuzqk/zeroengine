// ============================================================================
// RealmType.cs
// 境界类型与相关枚举定义
// 创建于: 2026-01-09
// ============================================================================

using System;

namespace ZeroEngine.Character.Realm
{
    /// <summary>
    /// 境界类型枚举
    /// 仙剑风格 - 简单分级，不过度仙侠化
    /// </summary>
    public enum RealmType
    {
        /// <summary>无境界</summary>
        None = 0,

        // ===== 凡人境 (武侠阶段) =====

        /// <summary>初入江湖 - 刚学武的新手</summary>
        Mortal_Beginner = 1,

        /// <summary>小有所成 - 有一定武学基础</summary>
        Mortal_Intermediate = 2,

        /// <summary>一流高手 - 江湖上有名号</summary>
        Mortal_Advanced = 3,

        /// <summary>绝顶高手 - 武林顶尖</summary>
        Mortal_Peak = 4,

        // ===== 仙人境 (仙侠阶段，简化) =====

        /// <summary>踏入仙途 - 感应天地灵气</summary>
        Immortal_Entry = 10,

        /// <summary>筑基 - 构建修仙根基</summary>
        Immortal_Foundation = 11,

        /// <summary>结丹 - 凝聚金丹</summary>
        Immortal_Core = 12,

        /// <summary>元婴 - 最高境界 (不继续往上)</summary>
        Immortal_Nascent = 13
    }

    /// <summary>
    /// 境界分类
    /// </summary>
    public enum RealmCategory
    {
        /// <summary>凡人境</summary>
        Mortal,

        /// <summary>仙人境</summary>
        Immortal
    }

    /// <summary>
    /// 境界突破状态
    /// </summary>
    public enum BreakthroughStatus
    {
        /// <summary>正常修炼中</summary>
        Normal,

        /// <summary>可以尝试突破</summary>
        Ready,

        /// <summary>突破中</summary>
        InProgress,

        /// <summary>遇到瓶颈</summary>
        Bottleneck,

        /// <summary>走火入魔</summary>
        Deviation
    }

    /// <summary>
    /// 突破结果
    /// </summary>
    public enum BreakthroughResult
    {
        /// <summary>成功</summary>
        Success,

        /// <summary>失败 (无副作用)</summary>
        Failed,

        /// <summary>失败 (境界跌落)</summary>
        FailedWithRegression,

        /// <summary>失败 (走火入魔)</summary>
        FailedWithDeviation,

        /// <summary>大成功 (额外奖励)</summary>
        CriticalSuccess
    }

    /// <summary>
    /// 境界辅助方法
    /// </summary>
    public static class RealmHelper
    {
        /// <summary>
        /// 获取境界分类
        /// </summary>
        public static RealmCategory GetCategory(RealmType realm)
        {
            return (int)realm >= 10 ? RealmCategory.Immortal : RealmCategory.Mortal;
        }

        /// <summary>
        /// 获取境界等级 (用于数值计算)
        /// </summary>
        public static int GetRealmLevel(RealmType realm)
        {
            return realm switch
            {
                RealmType.Mortal_Beginner => 1,
                RealmType.Mortal_Intermediate => 2,
                RealmType.Mortal_Advanced => 3,
                RealmType.Mortal_Peak => 4,
                RealmType.Immortal_Entry => 5,
                RealmType.Immortal_Foundation => 6,
                RealmType.Immortal_Core => 7,
                RealmType.Immortal_Nascent => 8,
                _ => 0
            };
        }

        /// <summary>
        /// 获取下一个境界
        /// </summary>
        public static RealmType GetNextRealm(RealmType current)
        {
            return current switch
            {
                RealmType.None => RealmType.Mortal_Beginner,
                RealmType.Mortal_Beginner => RealmType.Mortal_Intermediate,
                RealmType.Mortal_Intermediate => RealmType.Mortal_Advanced,
                RealmType.Mortal_Advanced => RealmType.Mortal_Peak,
                RealmType.Mortal_Peak => RealmType.Immortal_Entry,
                RealmType.Immortal_Entry => RealmType.Immortal_Foundation,
                RealmType.Immortal_Foundation => RealmType.Immortal_Core,
                RealmType.Immortal_Core => RealmType.Immortal_Nascent,
                _ => current // 已是最高境界
            };
        }

        /// <summary>
        /// 获取上一个境界
        /// </summary>
        public static RealmType GetPreviousRealm(RealmType current)
        {
            return current switch
            {
                RealmType.Mortal_Intermediate => RealmType.Mortal_Beginner,
                RealmType.Mortal_Advanced => RealmType.Mortal_Intermediate,
                RealmType.Mortal_Peak => RealmType.Mortal_Advanced,
                RealmType.Immortal_Entry => RealmType.Mortal_Peak,
                RealmType.Immortal_Foundation => RealmType.Immortal_Entry,
                RealmType.Immortal_Core => RealmType.Immortal_Foundation,
                RealmType.Immortal_Nascent => RealmType.Immortal_Core,
                _ => current // 已是最低境界
            };
        }

        /// <summary>
        /// 检查是否为最高境界
        /// </summary>
        public static bool IsMaxRealm(RealmType realm)
        {
            return realm == RealmType.Immortal_Nascent;
        }

        /// <summary>
        /// 获取境界中文名称
        /// </summary>
        public static string GetRealmName(RealmType realm)
        {
            return realm switch
            {
                RealmType.None => "无",
                RealmType.Mortal_Beginner => "初入江湖",
                RealmType.Mortal_Intermediate => "小有所成",
                RealmType.Mortal_Advanced => "一流高手",
                RealmType.Mortal_Peak => "绝顶高手",
                RealmType.Immortal_Entry => "踏入仙途",
                RealmType.Immortal_Foundation => "筑基",
                RealmType.Immortal_Core => "结丹",
                RealmType.Immortal_Nascent => "元婴",
                _ => "未知"
            };
        }

        /// <summary>
        /// 获取境界描述
        /// </summary>
        public static string GetRealmDescription(RealmType realm)
        {
            return realm switch
            {
                RealmType.Mortal_Beginner => "刚踏入武林，一切从头开始",
                RealmType.Mortal_Intermediate => "武学小成，在江湖上已有一席之地",
                RealmType.Mortal_Advanced => "武功高强，名震一方",
                RealmType.Mortal_Peak => "武林绝顶，天下少有敌手",
                RealmType.Immortal_Entry => "感应天地灵气，踏上仙途",
                RealmType.Immortal_Foundation => "筑就修仙根基，脱胎换骨",
                RealmType.Immortal_Core => "凝聚金丹，寿元大增",
                RealmType.Immortal_Nascent => "元婴出窍，已近仙人",
                _ => ""
            };
        }
    }
}
