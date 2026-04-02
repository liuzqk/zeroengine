using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>死亡触发 — 自身/目标/友军死亡时触发</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("死亡触发")]
    [Sirenix.OdinInspector.GUIColor(0.6f, 0.6f, 0.6f)]
#endif
    public class OnDeathTriggerData : TriggerComponentData
    {
        public bool TriggerOnSelfDeath = true;
        public bool TriggerOnTargetDeath = false;
        public bool TriggerOnAllyDeath = false;
    }
}
