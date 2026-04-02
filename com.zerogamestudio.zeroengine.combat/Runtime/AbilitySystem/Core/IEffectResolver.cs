namespace ZeroEngine.AbilitySystem
{
    /// <summary>
    /// 效果解析器 — 各项目实现自己的效果落地逻辑
    /// POB: 对接 Core, DamageComponent, BuffManager
    /// P6:  对接 CombatManager, ThreatTable, UnitData
    /// LLS: 对接 ItemHandler, PlayerStats
    /// </summary>
    public interface IEffectResolver
    {
        /// <summary>
        /// 解析并执行单个效果
        /// </summary>
        /// <returns>是否成功执行</returns>
        bool ResolveEffect(EffectComponentData effect, IAbilityContext context);

        /// <summary>
        /// 检查条件是否满足
        /// </summary>
        bool CheckCondition(ConditionComponentData condition, IAbilityContext context);
    }

    /// <summary>
    /// 默认效果解析器 — 只做 Debug.Log，用于测试和开发
    /// </summary>
    public class DebugEffectResolver : IEffectResolver
    {
        public bool ResolveEffect(EffectComponentData effect, IAbilityContext context)
        {
            UnityEngine.Debug.Log(
                $"[AbilitySystem] Effect: {effect.GetType().Name} " +
                $"on {context.Target?.Transform?.name ?? "self"} " +
                $"x{context.EffectMultiplier:F2}");
            return true;
        }

        public bool CheckCondition(ConditionComponentData condition, IAbilityContext context)
        {
            return true;
        }
    }
}
