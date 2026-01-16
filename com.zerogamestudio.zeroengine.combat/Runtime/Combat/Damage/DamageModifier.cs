using System;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 伤害修正器 - 用于修改伤害计算
    /// </summary>
    [Serializable]
    public struct DamageModifier
    {
        /// <summary>修正类型</summary>
        public DamageModifierType ModifierType;

        /// <summary>目标伤害类型（仅对特定类型生效）</summary>
        public DamageType TargetDamageType;

        /// <summary>修正值</summary>
        public float Value;

        /// <summary>修正操作</summary>
        public ModifierOperation Operation;

        /// <summary>来源ID（用于识别和移除）</summary>
        public string SourceId;

        /// <summary>
        /// 创建伤害增幅修正器（百分比）
        /// </summary>
        public static DamageModifier DamageBonus(float percentage, string sourceId = null)
        {
            return new DamageModifier
            {
                ModifierType = DamageModifierType.DamageDealt,
                TargetDamageType = DamageType.All,
                Value = percentage,
                Operation = ModifierOperation.PercentAdd,
                SourceId = sourceId
            };
        }

        /// <summary>
        /// 创建伤害减免修正器（百分比）
        /// </summary>
        public static DamageModifier DamageReduction(float percentage, string sourceId = null)
        {
            return new DamageModifier
            {
                ModifierType = DamageModifierType.DamageTaken,
                TargetDamageType = DamageType.All,
                Value = -percentage,
                Operation = ModifierOperation.PercentAdd,
                SourceId = sourceId
            };
        }

        /// <summary>
        /// 创建元素抗性修正器
        /// </summary>
        public static DamageModifier ElementalResistance(DamageType element, float percentage, string sourceId = null)
        {
            return new DamageModifier
            {
                ModifierType = DamageModifierType.DamageTaken,
                TargetDamageType = element,
                Value = -percentage,
                Operation = ModifierOperation.PercentAdd,
                SourceId = sourceId
            };
        }

        /// <summary>
        /// 创建暴击率修正器
        /// </summary>
        public static DamageModifier CritChance(float flatBonus, string sourceId = null)
        {
            return new DamageModifier
            {
                ModifierType = DamageModifierType.CritChance,
                TargetDamageType = DamageType.All,
                Value = flatBonus,
                Operation = ModifierOperation.FlatAdd,
                SourceId = sourceId
            };
        }

        /// <summary>
        /// 创建暴击伤害修正器
        /// </summary>
        public static DamageModifier CritDamage(float percentage, string sourceId = null)
        {
            return new DamageModifier
            {
                ModifierType = DamageModifierType.CritDamage,
                TargetDamageType = DamageType.All,
                Value = percentage,
                Operation = ModifierOperation.PercentAdd,
                SourceId = sourceId
            };
        }

        /// <summary>
        /// 创建护甲穿透修正器
        /// </summary>
        public static DamageModifier ArmorPenetration(float percentage, string sourceId = null)
        {
            return new DamageModifier
            {
                ModifierType = DamageModifierType.ArmorPenetration,
                TargetDamageType = DamageType.Physical,
                Value = percentage,
                Operation = ModifierOperation.PercentAdd,
                SourceId = sourceId
            };
        }

        /// <summary>
        /// 应用修正器到值
        /// </summary>
        public float Apply(float baseValue)
        {
            return Operation switch
            {
                ModifierOperation.FlatAdd => baseValue + Value,
                ModifierOperation.PercentAdd => baseValue * (1f + Value),
                ModifierOperation.PercentMult => baseValue * Value,
                ModifierOperation.Override => Value,
                _ => baseValue
            };
        }
    }

    /// <summary>
    /// 伤害修正器类型
    /// </summary>
    public enum DamageModifierType
    {
        /// <summary>造成伤害</summary>
        DamageDealt,
        /// <summary>受到伤害</summary>
        DamageTaken,
        /// <summary>暴击率</summary>
        CritChance,
        /// <summary>暴击伤害</summary>
        CritDamage,
        /// <summary>护甲穿透</summary>
        ArmorPenetration,
        /// <summary>魔法穿透</summary>
        MagicPenetration,
        /// <summary>生命偷取</summary>
        Lifesteal,
        /// <summary>伤害吸收</summary>
        DamageAbsorb
    }

    /// <summary>
    /// 修正器操作类型
    /// </summary>
    public enum ModifierOperation
    {
        /// <summary>固定值加法</summary>
        FlatAdd,
        /// <summary>百分比加法 (base * (1 + value))</summary>
        PercentAdd,
        /// <summary>百分比乘法 (base * value)</summary>
        PercentMult,
        /// <summary>覆盖</summary>
        Override
    }
}
