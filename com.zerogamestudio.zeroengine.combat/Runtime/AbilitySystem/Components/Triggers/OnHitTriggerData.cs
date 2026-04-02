using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>命中触发 — 造成/受到伤害时触发</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("命中触发")]
    [Sirenix.OdinInspector.GUIColor(1f, 0.7f, 0.2f)]
#endif
    public class OnHitTriggerData : TriggerComponentData
    {
        public bool TriggerOnDealingDamage = true;
        public bool TriggerOnTakingDamage = false;
    }
}
