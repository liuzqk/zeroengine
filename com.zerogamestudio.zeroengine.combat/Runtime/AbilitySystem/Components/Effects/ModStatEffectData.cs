using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>属性修改方式</summary>
    public enum StatModType
    {
        Additive,
        Multiplicative,
        Override
    }

    /// <summary>修改属性效果 — 修改攻击/防御等数值</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("修改属性")]
    [Sirenix.OdinInspector.GUIColor(0.8f, 0.8f, 1f)]
#endif
    public class ModStatEffectData : EffectComponentData
    {
        public string StatName = "Attack";
        public float Value = 10f;
        public StatModType ModType = StatModType.Additive;

        /// <summary>持续时间，0=永久</summary>
        public float Duration = 0f;
    }
}
