using System;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 伤害类型枚举
    /// </summary>
    [Flags]
    public enum DamageType
    {
        /// <summary>无类型</summary>
        None = 0,

        /// <summary>物理伤害</summary>
        Physical = 1 << 0,

        /// <summary>魔法伤害</summary>
        Magical = 1 << 1,

        /// <summary>真实伤害（无视防御）</summary>
        True = 1 << 2,

        /// <summary>火焰伤害</summary>
        Fire = 1 << 3,

        /// <summary>冰霜伤害</summary>
        Ice = 1 << 4,

        /// <summary>雷电伤害</summary>
        Lightning = 1 << 5,

        /// <summary>毒素伤害</summary>
        Poison = 1 << 6,

        /// <summary>神圣伤害</summary>
        Holy = 1 << 7,

        /// <summary>暗影伤害</summary>
        Shadow = 1 << 8,

        /// <summary>自然伤害</summary>
        Nature = 1 << 9,

        /// <summary>混沌伤害</summary>
        Chaos = 1 << 10,

        // 组合类型
        /// <summary>所有元素伤害</summary>
        AllElemental = Fire | Ice | Lightning | Poison | Holy | Shadow | Nature,

        /// <summary>所有伤害类型</summary>
        All = ~None
    }

    /// <summary>
    /// 伤害来源类型
    /// </summary>
    public enum DamageSourceType
    {
        /// <summary>未知来源</summary>
        Unknown,

        /// <summary>普通攻击</summary>
        Attack,

        /// <summary>技能</summary>
        Ability,

        /// <summary>Buff/Debuff (DoT)</summary>
        StatusEffect,

        /// <summary>环境伤害</summary>
        Environment,

        /// <summary>反弹伤害</summary>
        Reflect,

        /// <summary>处决/斩杀</summary>
        Execute,

        /// <summary>自伤</summary>
        SelfInflicted,

        /// <summary>效果（别名，同StatusEffect）</summary>
        Effect = StatusEffect
    }

    /// <summary>
    /// 伤害标记（用于特殊处理）
    /// </summary>
    [Flags]
    public enum DamageFlags
    {
        /// <summary>无特殊标记</summary>
        None = 0,

        /// <summary>暴击</summary>
        Critical = 1 << 0,

        /// <summary>无视护甲</summary>
        IgnoreArmor = 1 << 1,

        /// <summary>无视闪避</summary>
        IgnoreDodge = 1 << 2,

        /// <summary>无视格挡</summary>
        IgnoreBlock = 1 << 3,

        /// <summary>无法被反弹</summary>
        CannotReflect = 1 << 4,

        /// <summary>无法被吸收</summary>
        CannotAbsorb = 1 << 5,

        /// <summary>无法触发击中效果</summary>
        NoOnHitEffects = 1 << 6,

        /// <summary>生命偷取</summary>
        Lifesteal = 1 << 7,

        /// <summary>溅射伤害</summary>
        Splash = 1 << 8,

        /// <summary>穿透伤害</summary>
        Piercing = 1 << 9,

        /// <summary>无视无敌</summary>
        IgnoreInvulnerable = 1 << 10
    }
}
