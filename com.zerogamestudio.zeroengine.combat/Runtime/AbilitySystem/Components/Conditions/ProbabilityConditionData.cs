using System;
using UnityEngine;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>概率条件 — 随机判定是否通过</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("概率条件")]
#endif
    public class ProbabilityConditionData : ConditionComponentData
    {
        [Range(0f, 1f)]
        public float Probability = 0.5f;
    }
}
