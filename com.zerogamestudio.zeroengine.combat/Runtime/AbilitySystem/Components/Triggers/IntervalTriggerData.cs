using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>定时触发 — 每隔 Interval 秒/tick 触发一次</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("定时触发")]
    [Sirenix.OdinInspector.GUIColor(0.7f, 1f, 1f)]
#endif
    public class IntervalTriggerData : TriggerComponentData
    {
        public float Interval = 1.0f;
        public bool StartImmediately = true;
    }
}
