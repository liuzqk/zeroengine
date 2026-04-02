using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>生命值变化触发 — 受伤/治疗/血量阈值触发</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("血量变化触发")]
    [Sirenix.OdinInspector.GUIColor(1f, 0.4f, 0.4f)]
#endif
    public class OnHealthChangeTriggerData : TriggerComponentData
    {
        public bool TriggerOnDamage = true;
        public bool TriggerOnHeal = false;

        /// <summary>血量阈值百分比 (0-1)，-1表示不限</summary>
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.PropertyRange(-1f, 1f)]
#else
        [UnityEngine.Range(-1f, 1f)]
#endif
        public float ThresholdPercent = -1f;

        public ComparisonType ThresholdComparison = ComparisonType.LessThan;
    }
}
