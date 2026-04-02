using System;
using UnityEngine;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>血量检查条件 — 自身或目标血量百分比</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("血量检查")]
#endif
    public class HealthCheckConditionData : ConditionComponentData
    {
        [Range(0f, 1f)]
        public float HealthPercent = 0.3f;

        public ComparisonType Comparison = ComparisonType.LessThan;

        /// <summary>true=检查施放者自身，false=检查目标</summary>
        public bool CheckSelf = true;
    }
}
