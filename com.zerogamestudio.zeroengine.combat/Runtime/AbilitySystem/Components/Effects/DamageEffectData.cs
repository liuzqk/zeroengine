using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>伤害缩放方式</summary>
    public enum DamageScaling
    {
        /// <summary>固定数值</summary>
        Flat,
        /// <summary>攻击力百分比</summary>
        AttackPercent,
        /// <summary>最大生命值百分比</summary>
        MaxHealthPercent
    }

    /// <summary>造成伤害效果</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("造成伤害")]
    [Sirenix.OdinInspector.GUIColor(1f, 0.5f, 0.5f)]
#endif
    public class DamageEffectData : EffectComponentData
    {
        public float BaseDamage = 10f;
        public DamageScaling Scaling = DamageScaling.Flat;
        public float ScalingMultiplier = 1f;
        public DamageType DamageType = DamageType.Physical;
    }
}
