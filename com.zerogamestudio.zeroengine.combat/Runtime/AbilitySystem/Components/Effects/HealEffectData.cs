using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>治疗效果</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("治疗")]
    [Sirenix.OdinInspector.GUIColor(0.5f, 1f, 0.5f)]
#endif
    public class HealEffectData : EffectComponentData
    {
        public float HealAmount = 10f;
        public DamageScaling Scaling = DamageScaling.Flat;
        public float ScalingMultiplier = 1f;
    }
}
