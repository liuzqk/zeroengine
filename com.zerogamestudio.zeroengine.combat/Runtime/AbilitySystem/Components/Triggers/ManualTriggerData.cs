using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>手动触发 — 由外部代码显式调用</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("手动触发")]
    [Sirenix.OdinInspector.GUIColor(0.7f, 1f, 0.7f)]
#endif
    public class ManualTriggerData : TriggerComponentData
    {
        public string ButtonName = "Fire";
    }
}
