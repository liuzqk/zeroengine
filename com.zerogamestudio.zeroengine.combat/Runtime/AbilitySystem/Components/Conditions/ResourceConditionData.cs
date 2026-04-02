using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>资源条件 — 检查并消耗资源（MP/怒气/能量等）</summary>
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("资源条件")]
#endif
    public class ResourceConditionData : ConditionComponentData
    {
        public ResourceType Resource = ResourceType.Mana;
        public float RequiredAmount = 10;
        public bool ConsumeOnUse = true;
    }
}
