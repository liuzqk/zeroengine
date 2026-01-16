using UnityEngine;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 伤害数据结构 - 描述一次伤害的所有信息
    /// </summary>
    [System.Serializable]
    public struct DamageData
    {
        /// <summary>基础伤害值</summary>
        public float BaseDamage;

        /// <summary>伤害类型</summary>
        public DamageType DamageType;

        /// <summary>伤害来源类型</summary>
        public DamageSourceType SourceType;

        /// <summary>伤害标记</summary>
        public DamageFlags Flags;

        /// <summary>伤害来源（可为null）</summary>
        public ICombatant Source;

        /// <summary>关联的技能ID（可选）</summary>
        public string AbilityId;

        /// <summary>暴击率（0-1）</summary>
        public float CritChance;

        /// <summary>暴击倍率</summary>
        public float CritMultiplier;

        /// <summary>命中位置（可选）</summary>
        public Vector3 HitPoint;

        /// <summary>命中方向（可选）</summary>
        public Vector3 HitDirection;

        /// <summary>额外数据（用于扩展）</summary>
        public object CustomData;

        /// <summary>
        /// 创建基础物理伤害
        /// </summary>
        public static DamageData Physical(float damage, ICombatant source = null)
        {
            return new DamageData
            {
                BaseDamage = damage,
                DamageType = DamageType.Physical,
                SourceType = DamageSourceType.Attack,
                Flags = DamageFlags.None,
                Source = source,
                CritChance = 0f,
                CritMultiplier = 2f
            };
        }

        /// <summary>
        /// 创建基础魔法伤害
        /// </summary>
        public static DamageData Magical(float damage, DamageType element, ICombatant source = null)
        {
            return new DamageData
            {
                BaseDamage = damage,
                DamageType = DamageType.Magical | element,
                SourceType = DamageSourceType.Ability,
                Flags = DamageFlags.None,
                Source = source,
                CritChance = 0f,
                CritMultiplier = 2f
            };
        }

        /// <summary>
        /// 创建真实伤害（无视防御）
        /// </summary>
        public static DamageData True(float damage, ICombatant source = null)
        {
            return new DamageData
            {
                BaseDamage = damage,
                DamageType = DamageType.True,
                SourceType = DamageSourceType.Ability,
                Flags = DamageFlags.IgnoreArmor,
                Source = source,
                CritChance = 0f,
                CritMultiplier = 1f
            };
        }

        /// <summary>
        /// 创建DoT伤害
        /// </summary>
        public static DamageData DoT(float damage, DamageType damageType, ICombatant source = null)
        {
            return new DamageData
            {
                BaseDamage = damage,
                DamageType = damageType,
                SourceType = DamageSourceType.StatusEffect,
                Flags = DamageFlags.NoOnHitEffects,
                Source = source,
                CritChance = 0f,
                CritMultiplier = 1f
            };
        }

        /// <summary>
        /// 创建环境伤害
        /// </summary>
        public static DamageData Environment(float damage, DamageType damageType = DamageType.True)
        {
            return new DamageData
            {
                BaseDamage = damage,
                DamageType = damageType,
                SourceType = DamageSourceType.Environment,
                Flags = DamageFlags.IgnoreArmor | DamageFlags.IgnoreDodge,
                Source = null,
                CritChance = 0f,
                CritMultiplier = 1f
            };
        }

        /// <summary>
        /// 创建修改后基础伤害的副本
        /// </summary>
        public DamageData WithBaseDamage(float newBaseDamage)
        {
            return new DamageData
            {
                BaseDamage = newBaseDamage,
                DamageType = this.DamageType,
                SourceType = this.SourceType,
                Flags = this.Flags,
                Source = this.Source,
                AbilityId = this.AbilityId,
                CritChance = this.CritChance,
                CritMultiplier = this.CritMultiplier,
                HitPoint = this.HitPoint,
                HitDirection = this.HitDirection,
                CustomData = this.CustomData
            };
        }

        /// <summary>
        /// 设置暴击参数
        /// </summary>
        public DamageData WithCrit(float chance, float multiplier = 2f)
        {
            CritChance = Mathf.Clamp01(chance);
            CritMultiplier = multiplier;
            return this;
        }

        /// <summary>
        /// 添加伤害标记
        /// </summary>
        public DamageData WithFlags(DamageFlags flags)
        {
            Flags |= flags;
            return this;
        }

        /// <summary>
        /// 设置命中信息
        /// </summary>
        public DamageData WithHitInfo(Vector3 point, Vector3 direction)
        {
            HitPoint = point;
            HitDirection = direction;
            return this;
        }

        /// <summary>
        /// 检查是否有指定标记
        /// </summary>
        public bool HasFlag(DamageFlags flag)
        {
            return (Flags & flag) != 0;
        }

        /// <summary>
        /// 检查是否有指定伤害类型
        /// </summary>
        public bool HasDamageType(DamageType type)
        {
            return (DamageType & type) != 0;
        }
    }
}
