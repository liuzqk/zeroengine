using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>冷却条件 — 额外冷却检查（在 AbilityInstance.IsOnCooldown 之外）</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("冷却条件")]
#endif
    public class CooldownConditionData : ConditionComponentData
    {
        public float CooldownSeconds = 1.0f;
    }
}
