using System;
using UnityEngine;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>
    /// 技能来源接口 — 由拥有技能系统的实体实现
    /// </summary>
    public interface IAbilitySource
    {
        Transform Transform { get; }
    }

    /// <summary>
    /// 技能目标接口 — 由可被技能作用的实体实现
    /// </summary>
    public interface IAbilityTarget
    {
        Transform Transform { get; }
    }

    /// <summary>
    /// 技能施放状态
    /// </summary>
    public enum AbilityCastState
    {
        Idle,
        Casting,      // 前摇（可打断）
        Executing,    // 效果释放中
        Recovering    // 后摇
    }

    /// <summary>
    /// 技能运行时实例 — 包含等级和冷却追踪
    /// </summary>
    [Serializable]
    public class AbilityInstance
    {
        public AbilityDataSO Data;
        public int Level = 1;
        public float CooldownRemaining;

        public bool IsOnCooldown => CooldownRemaining > 0;
        public float Cooldown => Data.GetCooldown(Level);
        public float EffectMultiplier => Data.GetEffectMultiplier(Level);

        public AbilityInstance(AbilityDataSO data, int level = 1)
        {
            Data = data;
            Level = Mathf.Clamp(level, 1, data.MaxLevel);
            CooldownRemaining = 0;
        }

        public bool TryLevelUp()
        {
            if (Level < Data.MaxLevel)
            {
                Level++;
                return true;
            }
            return false;
        }

        public void StartCooldown()
        {
            CooldownRemaining = Cooldown;
        }

        public void UpdateCooldown(float deltaTime)
        {
            if (CooldownRemaining > 0)
            {
                CooldownRemaining = Mathf.Max(0, CooldownRemaining - deltaTime);
            }
        }

        public void ResetCooldown()
        {
            CooldownRemaining = 0;
        }
    }
}
