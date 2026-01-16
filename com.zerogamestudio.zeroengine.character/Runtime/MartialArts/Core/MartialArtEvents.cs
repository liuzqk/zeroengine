// ============================================================================
// MartialArtEvents.cs
// 武学事件系统
// 创建于: 2026-01-09
// ============================================================================

using System;

namespace ZeroEngine.Character.MartialArts
{
    /// <summary>
    /// 武学事件常量
    /// </summary>
    public static class MartialArtEvents
    {
        /// <summary>学习武学</summary>
        public const string OnMartialArtLearned = "MartialArt.Learned";

        /// <summary>武学升级</summary>
        public const string OnMartialArtLevelUp = "MartialArt.LevelUp";

        /// <summary>武学圆满</summary>
        public const string OnMartialArtMastered = "MartialArt.Mastered";

        /// <summary>武学突破</summary>
        public const string OnMartialArtTranscended = "MartialArt.Transcended";

        /// <summary>招式解锁</summary>
        public const string OnSkillUnlocked = "MartialArt.SkillUnlocked";

        /// <summary>装备武学</summary>
        public const string OnMartialArtEquipped = "MartialArt.Equipped";

        /// <summary>卸下武学</summary>
        public const string OnMartialArtUnequipped = "MartialArt.Unequipped";

        /// <summary>修炼经验获得</summary>
        public const string OnCultivationExpGained = "MartialArt.ExpGained";
    }

    /// <summary>
    /// 学习武学事件参数
    /// </summary>
    public struct MartialArtLearnedEventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId;

        /// <summary>武学 ID</summary>
        public string ArtId;

        /// <summary>武学名称</summary>
        public string ArtName;

        /// <summary>武学类型</summary>
        public MartialArtType ArtType;

        /// <summary>武学品级</summary>
        public MartialArtGrade Grade;
    }

    /// <summary>
    /// 武学升级事件参数
    /// </summary>
    public struct MartialArtLevelUpEventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId;

        /// <summary>武学 ID</summary>
        public string ArtId;

        /// <summary>武学名称</summary>
        public string ArtName;

        /// <summary>旧层数</summary>
        public int OldLevel;

        /// <summary>新层数</summary>
        public int NewLevel;

        /// <summary>是否圆满</summary>
        public bool IsMastered;
    }

    /// <summary>
    /// 招式解锁事件参数
    /// </summary>
    public struct SkillUnlockedEventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId;

        /// <summary>武学 ID</summary>
        public string ArtId;

        /// <summary>招式 ID</summary>
        public string SkillId;

        /// <summary>招式名称</summary>
        public string SkillName;

        /// <summary>是否为绝招</summary>
        public bool IsUltimate;
    }

    /// <summary>
    /// 装备武学事件参数
    /// </summary>
    public struct MartialArtEquippedEventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId;

        /// <summary>武学 ID</summary>
        public string ArtId;

        /// <summary>槽位类型</summary>
        public MartialArtSlotType Slot;

        /// <summary>之前装备的武学 ID (可为空)</summary>
        public string PreviousArtId;
    }

    /// <summary>
    /// 修炼经验获得事件参数
    /// </summary>
    public struct CultivationExpGainedEventArgs
    {
        /// <summary>角色 ID</summary>
        public string CharacterId;

        /// <summary>武学 ID</summary>
        public string ArtId;

        /// <summary>获得经验</summary>
        public int ExpGained;

        /// <summary>当前经验</summary>
        public int CurrentExp;

        /// <summary>当前层数</summary>
        public int CurrentLevel;

        /// <summary>经验来源</summary>
        public CultivationExpSource Source;
    }

    /// <summary>
    /// 修炼经验来源
    /// </summary>
    public enum CultivationExpSource
    {
        /// <summary>战斗</summary>
        Combat,

        /// <summary>打坐修炼</summary>
        Meditation,

        /// <summary>使用秘籍</summary>
        Manual,

        /// <summary>师傅传功</summary>
        MasterTeaching,

        /// <summary>顿悟</summary>
        Enlightenment,

        /// <summary>其他</summary>
        Other
    }
}
