// ============================================================================
// MartialArtType.cs
// 武学类型与相关枚举定义
// 创建于: 2026-01-09
// ============================================================================

using System;

namespace ZeroEngine.Character.MartialArts
{
    /// <summary>
    /// 武学类型
    /// </summary>
    public enum MartialArtType
    {
        /// <summary>无</summary>
        None = 0,

        /// <summary>内功 - 修炼内力/真气, 提升属性</summary>
        InnerArt = 1,

        /// <summary>外功 - 拳脚功夫, 硬功</summary>
        OuterArt = 2,

        /// <summary>轻功 - 身法, 移动能力</summary>
        Lightness = 3,

        /// <summary>剑法</summary>
        Sword = 10,

        /// <summary>刀法</summary>
        Saber = 11,

        /// <summary>枪法/矛法</summary>
        Spear = 12,

        /// <summary>棍法</summary>
        Staff = 13,

        /// <summary>拳法</summary>
        Fist = 14,

        /// <summary>掌法</summary>
        Palm = 15,

        /// <summary>指法</summary>
        Finger = 16,

        /// <summary>腿法</summary>
        Leg = 17,

        /// <summary>暗器</summary>
        HiddenWeapon = 20,

        /// <summary>毒术</summary>
        Poison = 21,

        /// <summary>医术</summary>
        Medicine = 22,

        /// <summary>阵法</summary>
        Formation = 23,

        /// <summary>绝学 - 顶级武学</summary>
        Ultimate = 100,

        /// <summary>禁术 - 有副作用的强力武学</summary>
        Forbidden = 101
    }

    /// <summary>
    /// 武学品级
    /// </summary>
    public enum MartialArtGrade
    {
        /// <summary>下品 - 江湖流传</summary>
        Low = 1,

        /// <summary>中品 - 门派基础</summary>
        Medium = 2,

        /// <summary>上品 - 门派核心</summary>
        High = 3,

        /// <summary>极品 - 镇派之宝</summary>
        Supreme = 4,

        /// <summary>绝世 - 天下无双</summary>
        Legendary = 5
    }

    /// <summary>
    /// 武学修炼状态
    /// </summary>
    public enum MartialArtStatus
    {
        /// <summary>未学习</summary>
        NotLearned = 0,

        /// <summary>入门 (1-3 层)</summary>
        Beginner = 1,

        /// <summary>小成 (4-6 层)</summary>
        Intermediate = 2,

        /// <summary>大成 (7-9 层)</summary>
        Advanced = 3,

        /// <summary>圆满 (10 层)</summary>
        Mastered = 4,

        /// <summary>登峰造极 (突破上限)</summary>
        Transcendent = 5
    }

    /// <summary>
    /// 武学槽位类型
    /// </summary>
    public enum MartialArtSlotType
    {
        /// <summary>主修内功 (只能装备一个)</summary>
        PrimaryInner = 0,

        /// <summary>辅修内功</summary>
        SecondaryInner = 1,

        /// <summary>主修外功/武技</summary>
        PrimaryOuter = 2,

        /// <summary>辅修外功/武技</summary>
        SecondaryOuter = 3,

        /// <summary>轻功</summary>
        Lightness = 4,

        /// <summary>绝学槽位</summary>
        Ultimate = 5
    }

    /// <summary>
    /// 武学学习条件类型
    /// </summary>
    [Flags]
    public enum LearnRequirementType
    {
        None = 0,

        /// <summary>境界要求</summary>
        Realm = 1 << 0,

        /// <summary>属性要求</summary>
        Stat = 1 << 1,

        /// <summary>前置武学</summary>
        PrerequisiteArt = 1 << 2,

        /// <summary>门派要求</summary>
        Sect = 1 << 3,

        /// <summary>物品消耗</summary>
        Item = 1 << 4,

        /// <summary>特殊条件</summary>
        Special = 1 << 5
    }
}
