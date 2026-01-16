using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.StatSystem
{
    /// <summary>
    /// 标准 RPG 属性类型枚举
    /// </summary>
    public enum StatType
    {
        None,

        // === 基础资源 ===
        MaxHP,
        MaxMP,

        // === 基础属性 ===
        Attack,
        Defense,
        MagicAttack,
        MagicDefense,
        Speed,
        Luck,

        // === 战斗属性 ===
        CritRate,
        CritDamage,
        HitRate,
        DodgeRate,
        BlockRate,
        CounterRate,
        LifeSteal,
        DamageReduction,

        // === 移动属性 ===
        MoveSpeed,
        JumpForce,

        // === 自定义扩展 ===
        Custom1,
        Custom2,
        Custom3,
        Custom4,
        Custom5
    }

    public interface IStatProvider
    {
        float GetStatValue(StatType type);
    }
}
