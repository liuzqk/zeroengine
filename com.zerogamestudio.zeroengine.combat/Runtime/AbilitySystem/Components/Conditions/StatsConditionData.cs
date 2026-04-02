using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>属性检查条件 — 通用stat数值比较</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("属性检查")]
#endif
    public class StatsConditionData : ConditionComponentData
    {
        public string StatName;
        public float Value;
        public ComparisonType Comparison = ComparisonType.GreaterThan;
    }
}
