using System;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>
    /// 技能状态变更事件 — readonly struct 避免GC
    /// </summary>
    public readonly struct AbilityStateChangedArgs
    {
        public readonly AbilityInstance Ability;
        public readonly AbilityCastState PreviousState;
        public readonly AbilityCastState NewState;
        public readonly IAbilityContext Context;

        public AbilityStateChangedArgs(AbilityInstance ability,
            AbilityCastState prev, AbilityCastState next, IAbilityContext ctx)
        {
            Ability = ability;
            PreviousState = prev;
            NewState = next;
            Context = ctx;
        }
    }

    /// <summary>
    /// 技能被打断事件
    /// </summary>
    public readonly struct AbilityInterruptedArgs
    {
        public readonly AbilityInstance Ability;
        public readonly string Reason;

        public AbilityInterruptedArgs(AbilityInstance ability, string reason)
        {
            Ability = ability;
            Reason = reason;
        }
    }

    /// <summary>
    /// 技能效果执行完成事件
    /// </summary>
    public readonly struct AbilityExecutedArgs
    {
        public readonly AbilityInstance Ability;
        public readonly IAbilityContext Context;
        public readonly int EffectsApplied;

        public AbilityExecutedArgs(AbilityInstance ability, IAbilityContext ctx, int count)
        {
            Ability = ability;
            Context = ctx;
            EffectsApplied = count;
        }
    }
}
