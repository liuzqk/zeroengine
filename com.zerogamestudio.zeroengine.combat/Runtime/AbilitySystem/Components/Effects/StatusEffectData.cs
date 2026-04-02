using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>状态效果类型</summary>
    public enum StatusModType
    {
        Buff,
        Debuff
    }

    /// <summary>状态效果 — 通用Buff/Debuff（不依赖BuffSystem，轻量级）</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("状态效果")]
    [Sirenix.OdinInspector.GUIColor(0.9f, 0.8f, 0.3f)]
#endif
    public class StatusEffectData : EffectComponentData
    {
        public string EffectName;
        public StatusModType ModType = StatusModType.Buff;

        /// <summary>影响的属性名</summary>
        public string StatToModify = "Attack";

        /// <summary>修改值（正数=增益，负数=减益。ModType 仅用于分类显示）</summary>
        public float Value = 10f;

        /// <summary>持续 tick 数</summary>
        public int DurationTicks = 5;
    }
}
