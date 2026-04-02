using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>范围伤害效果 — AOE伤害，P6自走棋核心需求</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("范围伤害")]
    [Sirenix.OdinInspector.GUIColor(1f, 0.4f, 0.2f)]
#endif
    public class AreaDamageEffectData : EffectComponentData
    {
        public float BaseDamage = 10f;
        public float ScalingMultiplier = 1f;

        /// <summary>影响范围（格子/单位距离）</summary>
        public int Radius = 2;

        public DamageType DamageType = DamageType.Magical;

        /// <summary>是否包含施放者自身</summary>
        public bool IncludeSelf = false;
    }
}
