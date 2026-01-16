// ============================================================================
// SectType.cs
// 门派类型与相关枚举定义
// 创建于: 2026-01-09
// ============================================================================

using System;

namespace ZeroEngine.Character.Sect
{
    /// <summary>
    /// 门派类型枚举
    /// 武侠风格 - 支持 15+ 门派 (含隐藏门派)
    /// </summary>
    public enum SectType
    {
        /// <summary>无门派 (散修/江湖人士)</summary>
        None = 0,

        // ===== 正道门派 =====

        /// <summary>少林 - 佛门正宗, 刚猛拳法/禅定内功</summary>
        Shaolin = 1,

        /// <summary>武当 - 道家正宗, 太极剑法/内丹术</summary>
        Wudang = 2,

        /// <summary>峨眉 - 佛道兼修, 剑法/暗器</summary>
        Emei = 3,

        /// <summary>华山 - 剑宗气宗, 剑法精妙</summary>
        Huashan = 4,

        /// <summary>昆仑 - 西域大派, 刀剑双绝</summary>
        Kunlun = 5,

        /// <summary>崆峒 - 七伤拳法, 刚猛霸道</summary>
        Kongtong = 6,

        // ===== 中立门派 =====

        /// <summary>丐帮 - 天下第一大帮, 降龙掌/打狗棒</summary>
        BeggarsGang = 10,

        /// <summary>唐门 - 暗器毒药, 机关术</summary>
        TangClan = 11,

        /// <summary>逍遥派 - 逍遥自在, 奇门武学</summary>
        Xiaoyao = 12,

        /// <summary>灵鹫宫 - 神秘势力, 生死符</summary>
        Lingjiu = 13,

        // ===== 邪道门派 =====

        /// <summary>魔教/日月神教 - 邪派之首</summary>
        SunMoon = 20,

        /// <summary>星宿派 - 毒功/媚术</summary>
        Xingxiu = 21,

        /// <summary>血刀门 - 血腥刀法</summary>
        BloodSaber = 22,

        // ===== 隐藏门派 =====

        /// <summary>天山 - 隐世门派, 天山折梅手</summary>
        Tianshan = 100,

        /// <summary>大理段氏 - 皇族武学, 六脉神剑</summary>
        Dali = 101,

        /// <summary>古墓派 - 隐世不出, 玉女心经</summary>
        AncientTomb = 102,

        // ===== 仙侠门派 (后期解锁) =====

        /// <summary>蜀山 - 御剑飞行, 仙剑术</summary>
        Shushan = 200,

        /// <summary>青云门 - 仙道正宗</summary>
        Qingyun = 201,

        /// <summary>鬼王宗 - 魔道大派</summary>
        GhostKing = 202,

        // ===== 自定义门派起始 ID =====

        /// <summary>自定义门派起始 ID</summary>
        Custom = 1000
    }

    /// <summary>
    /// 门派分类
    /// </summary>
    public enum SectCategory
    {
        /// <summary>正道门派</summary>
        Orthodox,

        /// <summary>中立门派</summary>
        Neutral,

        /// <summary>邪道门派</summary>
        Unorthodox,

        /// <summary>隐藏门派 (特殊条件解锁)</summary>
        Hidden,

        /// <summary>仙侠门派 (后期解锁)</summary>
        Immortal,

        /// <summary>自定义门派</summary>
        Custom
    }

    /// <summary>
    /// 门派职位/身份
    /// </summary>
    public enum SectRank
    {
        /// <summary>非门派成员</summary>
        None = 0,

        /// <summary>记名弟子 (最低级)</summary>
        Initiate = 1,

        /// <summary>外门弟子</summary>
        OuterDisciple = 2,

        /// <summary>内门弟子</summary>
        InnerDisciple = 3,

        /// <summary>核心弟子/真传弟子</summary>
        CoreDisciple = 4,

        /// <summary>长老</summary>
        Elder = 5,

        /// <summary>掌门/宗主</summary>
        Patriarch = 6
    }

    /// <summary>
    /// 门派贡献度等级
    /// </summary>
    public enum ContributionLevel
    {
        /// <summary>无贡献</summary>
        None = 0,

        /// <summary>普通</summary>
        Normal = 1,

        /// <summary>良好</summary>
        Good = 2,

        /// <summary>优秀</summary>
        Excellent = 3,

        /// <summary>杰出</summary>
        Outstanding = 4,

        /// <summary>传奇</summary>
        Legendary = 5
    }

    /// <summary>
    /// 门派武学类型限制
    /// </summary>
    [Flags]
    public enum SectMartialArtAccess
    {
        None = 0,

        /// <summary>基础武学 (所有弟子可学)</summary>
        Basic = 1 << 0,

        /// <summary>进阶武学 (内门弟子以上)</summary>
        Advanced = 1 << 1,

        /// <summary>核心武学 (核心弟子以上)</summary>
        Core = 1 << 2,

        /// <summary>镇派绝学 (长老以上/特殊传承)</summary>
        Ultimate = 1 << 3,

        /// <summary>禁忌武学 (掌门专属/禁术)</summary>
        Forbidden = 1 << 4,

        /// <summary>全部权限</summary>
        All = Basic | Advanced | Core | Ultimate | Forbidden
    }
}
