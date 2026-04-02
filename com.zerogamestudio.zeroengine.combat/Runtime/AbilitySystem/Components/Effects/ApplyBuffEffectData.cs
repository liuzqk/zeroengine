using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>施加Buff效果 — 对接 BuffSystem</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("施加Buff")]
    [Sirenix.OdinInspector.GUIColor(1f, 0.6f, 0.8f)]
#endif
    public class ApplyBuffEffectData : EffectComponentData
    {
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.InlineEditor]
#endif
        public ZeroEngine.BuffSystem.BuffData BuffToApply;

        /// <summary>持续时间覆盖，-1=使用BuffData默认值</summary>
        public float DurationOverride = -1f;

        /// <summary>叠加层数</summary>
        public int StackCount = 1;
    }
}
