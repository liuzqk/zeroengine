// ============================================================================
// JobType.cs
// 职业类型定义
// 创建于: 2026-01-07
// ============================================================================

using System;

namespace ZeroEngine.Character.Job
{
    /// <summary>
    /// 职业类型枚举
    /// 参考: 八方旅人风格 - 8 种基础职业 + 4 种高级职业
    /// </summary>
    public enum JobType
    {
        /// <summary>无职业</summary>
        None = 0,

        // ===== 物理系 =====

        /// <summary>剑士 - 平衡型近战, 剑技/盾反</summary>
        Warrior = 1,

        /// <summary>盗贼 - 高速型, 偷窃/暗器</summary>
        Thief = 2,

        /// <summary>猎人 - 远程物理, 弓箭/陷阱/捕获</summary>
        Hunter = 3,

        /// <summary>武僧 - 格斗型, 拳脚/气功</summary>
        Monk = 4,

        // ===== 魔法系 =====

        /// <summary>学者 - 攻击魔法, 属性弱点分析</summary>
        Scholar = 5,

        /// <summary>神官 - 治疗/光魔法, 净化</summary>
        Cleric = 6,

        /// <summary>舞者 - 辅助/Buff, 魅惑</summary>
        Dancer = 7,

        /// <summary>商人 - 金钱技能, 雇佣/收购</summary>
        Merchant = 8,

        // ===== 高级职业 (解锁条件更高) =====

        /// <summary>魔剑士 - 物魔双修</summary>
        RuneBlade = 100,

        /// <summary>星术师 - 强力魔法</summary>
        Starseer = 101,

        /// <summary>武器大师 - 全武器精通</summary>
        Warmaster = 102,

        /// <summary>发明家 - 道具强化</summary>
        Inventor = 103,

        // ===== 自定义职业起始 ID =====

        /// <summary>自定义职业起始 ID</summary>
        Custom = 1000
    }

    /// <summary>
    /// 职业分类
    /// </summary>
    public enum JobCategory
    {
        /// <summary>基础职业 (初始可选)</summary>
        Basic,

        /// <summary>高级职业 (需解锁)</summary>
        Advanced,

        /// <summary>隐藏职业 (特殊条件)</summary>
        Secret,

        /// <summary>自定义职业</summary>
        Custom
    }

    /// <summary>
    /// 职业槽位类型
    /// </summary>
    public enum JobSlotType
    {
        /// <summary>主职业 (不可更换)</summary>
        Primary,

        /// <summary>副职业 (可自由切换)</summary>
        Secondary,

        /// <summary>额外职业槽 (特殊系统)</summary>
        Extra
    }

    /// <summary>
    /// 技能学习状态
    /// </summary>
    public enum SkillLearnStatus
    {
        /// <summary>未解锁 (职业等级不足)</summary>
        Locked,

        /// <summary>可学习 (满足条件但未学)</summary>
        Available,

        /// <summary>已学习</summary>
        Learned,

        /// <summary>已精通 (永久保留)</summary>
        Mastered
    }

    /// <summary>
    /// 武器类型 (职业可用武器限制)
    /// </summary>
    [Flags]
    public enum WeaponCategory
    {
        None = 0,

        // 近战
        Sword = 1 << 0,      // 剑
        Dagger = 1 << 1,     // 匕首
        Axe = 1 << 2,        // 斧
        Spear = 1 << 3,      // 枪/矛
        Staff = 1 << 4,      // 杖
        Fist = 1 << 5,       // 拳套

        // 远程
        Bow = 1 << 6,        // 弓
        Crossbow = 1 << 7,   // 弩
        Gun = 1 << 8,        // 枪械
        Thrown = 1 << 9,     // 投掷武器

        // 特殊
        Shield = 1 << 10,    // 盾牌
        Instrument = 1 << 11, // 乐器
        Book = 1 << 12,      // 书籍/魔导器

        // 组合
        AllMelee = Sword | Dagger | Axe | Spear | Staff | Fist,
        AllRanged = Bow | Crossbow | Gun | Thrown,
        All = AllMelee | AllRanged | Shield | Instrument | Book
    }
}
