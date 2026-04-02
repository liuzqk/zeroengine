using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>持续伤害(DoT)效果</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("持续伤害(DoT)")]
    [Sirenix.OdinInspector.GUIColor(0.7f, 0.3f, 0.7f)]
#endif
    public class DotEffectData : EffectComponentData
    {
        public float DamagePerTick = 5f;
        public int DurationTicks = 4;
        public DamageType DamageType = DamageType.Magical;
        public string EffectName = "灼烧";
    }
}
