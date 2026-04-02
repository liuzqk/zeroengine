using System;
using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace ZeroEngine.AbilitySystem
{
    // ================ Interface & Base ================

    public abstract class ComponentData { }

    /// <summary>
    /// 运行时能力组件接口
    /// </summary>
    public interface IAbilityComponent
    {
        void Initialize(ComponentData data, object source, AbilityDataSO abilityData);
    }

    // ================ Data Base Classes ================

    [Serializable]
    public abstract class TriggerComponentData : ComponentData
    {
#if ODIN_INSPECTOR
        [FoldoutGroup("Settings", Expanded = true)]
        [LabelWidth(150)]
#endif
        public bool TriggerMultipleTimes;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings"), ShowIf(nameof(TriggerMultipleTimes))]
        [ProgressBar(1, 10, ColorGetter = "GetBarColor")]
#endif
        public int TriggerTimes = 1;

#if ODIN_INSPECTOR
        private Color GetBarColor(int value) => Color.Lerp(Color.red, Color.green, value / 10f);
#endif
    }

    [Serializable]
    public abstract class ConditionComponentData : ComponentData { }

    [Serializable]
    public abstract class EffectComponentData : ComponentData
    {
#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        public AbilityTargetType TargetType;
    }

    // ================ Enums ================

    public enum AbilityTargetType
    {
        Self,
        Target,
        All
    }

    public enum DamageType
    {
        Physical,
        Magical,
        True
    }

    public enum ResourceType
    {
        Health,
        Mana,
        Stamina,
        Rage,
        Energy
    }

    public enum ComparisonType
    {
        GreaterThan,
        LessThan,
        GreaterThanOrEqualTo,
        LessThanOrEqualTo,
        EqualTo
    }

    public enum LogicMode
    {
        And,
        Or
    }
}
