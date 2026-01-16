using System;

namespace ZeroEngine.RPG.Systems
{
    /// <summary>
    /// 弱点类型 - 八方旅人风格
    /// 使用 Flags 支持多重弱点
    /// </summary>
    [Flags]
    public enum WeaknessType
    {
        /// <summary>无弱点</summary>
        None = 0,

        // 物理类型 (1-8)
        /// <summary>剑</summary>
        Sword = 1 << 0,
        /// <summary>枪</summary>
        Spear = 1 << 1,
        /// <summary>短剑/匕首</summary>
        Dagger = 1 << 2,
        /// <summary>斧</summary>
        Axe = 1 << 3,
        /// <summary>弓</summary>
        Bow = 1 << 4,
        /// <summary>杖</summary>
        Staff = 1 << 5,

        // 魔法类型 (64-512)
        /// <summary>火</summary>
        Fire = 1 << 6,
        /// <summary>冰</summary>
        Ice = 1 << 7,
        /// <summary>雷</summary>
        Lightning = 1 << 8,
        /// <summary>风</summary>
        Wind = 1 << 9,
        /// <summary>光</summary>
        Light = 1 << 10,
        /// <summary>暗</summary>
        Dark = 1 << 11,

        // 分类掩码
        /// <summary>所有物理弱点</summary>
        AllPhysical = Sword | Spear | Dagger | Axe | Bow | Staff,
        /// <summary>所有魔法弱点</summary>
        AllMagical = Fire | Ice | Lightning | Wind | Light | Dark,
        /// <summary>所有弱点</summary>
        All = AllPhysical | AllMagical
    }

    /// <summary>
    /// 弱点类型扩展方法
    /// </summary>
    public static class WeaknessTypeExtensions
    {
        /// <summary>
        /// 检查是否为物理类型弱点
        /// </summary>
        public static bool IsPhysical(this WeaknessType type)
        {
            return (type & WeaknessType.AllPhysical) != 0;
        }

        /// <summary>
        /// 检查是否为魔法类型弱点
        /// </summary>
        public static bool IsMagical(this WeaknessType type)
        {
            return (type & WeaknessType.AllMagical) != 0;
        }

        /// <summary>
        /// 检查是否包含指定弱点
        /// </summary>
        public static bool HasWeakness(this WeaknessType self, WeaknessType target)
        {
            return (self & target) != 0;
        }

        /// <summary>
        /// 获取弱点的显示名称
        /// </summary>
        public static string GetDisplayName(this WeaknessType type)
        {
            return type switch
            {
                WeaknessType.Sword => "剑",
                WeaknessType.Spear => "枪",
                WeaknessType.Dagger => "短剑",
                WeaknessType.Axe => "斧",
                WeaknessType.Bow => "弓",
                WeaknessType.Staff => "杖",
                WeaknessType.Fire => "火",
                WeaknessType.Ice => "冰",
                WeaknessType.Lightning => "雷",
                WeaknessType.Wind => "风",
                WeaknessType.Light => "光",
                WeaknessType.Dark => "暗",
                _ => type.ToString()
            };
        }

        /// <summary>
        /// 获取弱点的图标名称 (用于 UI)
        /// </summary>
        public static string GetIconName(this WeaknessType type)
        {
            return type switch
            {
                WeaknessType.Sword => "icon_weakness_sword",
                WeaknessType.Spear => "icon_weakness_spear",
                WeaknessType.Dagger => "icon_weakness_dagger",
                WeaknessType.Axe => "icon_weakness_axe",
                WeaknessType.Bow => "icon_weakness_bow",
                WeaknessType.Staff => "icon_weakness_staff",
                WeaknessType.Fire => "icon_weakness_fire",
                WeaknessType.Ice => "icon_weakness_ice",
                WeaknessType.Lightning => "icon_weakness_lightning",
                WeaknessType.Wind => "icon_weakness_wind",
                WeaknessType.Light => "icon_weakness_light",
                WeaknessType.Dark => "icon_weakness_dark",
                _ => "icon_weakness_unknown"
            };
        }
    }
}
