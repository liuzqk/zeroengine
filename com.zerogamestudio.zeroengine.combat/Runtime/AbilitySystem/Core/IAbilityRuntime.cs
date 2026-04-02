namespace ZeroEngine.AbilitySystem
{
    /// <summary>
    /// 技能执行策略接口
    /// 实时模式: 由 MonoBehaviour.Update 驱动，支持前摇/后摇协程
    /// Tick模式: 由外部 Tick() 驱动，支持离散步进
    /// </summary>
    public interface IAbilityRuntime
    {
        AbilityCastState CurrentState { get; }
        float CastProgress { get; }

        /// <summary>尝试施放</summary>
        bool TryCast(AbilityInstance ability, IAbilityContext context);

        /// <summary>尝试打断</summary>
        bool TryInterrupt(string reason = "Interrupted");

        /// <summary>
        /// 每帧/每tick更新
        /// deltaTime: 实时模式=Time.deltaTime, Tick模式=tickInterval
        /// </summary>
        void Update(float deltaTime);

        /// <summary>强制重置状态</summary>
        void Reset();
    }
}
