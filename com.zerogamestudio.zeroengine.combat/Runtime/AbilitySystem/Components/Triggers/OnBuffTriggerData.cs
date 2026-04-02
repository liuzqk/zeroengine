using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>Buff变化触发 — Buff添加/移除时触发</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("Buff变化触发")]
    [Sirenix.OdinInspector.GUIColor(0.5f, 0.8f, 1f)]
#endif
    public class OnBuffTriggerData : TriggerComponentData
    {
        public string BuffName;
        public bool TriggerOnAdd = true;
        public bool TriggerOnRemove = false;
    }
}
