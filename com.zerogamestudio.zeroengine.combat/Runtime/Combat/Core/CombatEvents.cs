using UnityEngine;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 战斗事件常量
    /// </summary>
    public static class CombatEvents
    {
        public const string OnCombatStart = "Combat.Start";
        public const string OnCombatEnd = "Combat.End";
        public const string OnDamageDealt = "Combat.DamageDealt";
        public const string OnDamageTaken = "Combat.DamageTaken";
        public const string OnHeal = "Combat.Heal";
        public const string OnKill = "Combat.Kill";
        public const string OnDeath = "Combat.Death";
        public const string OnCombatantEnter = "Combat.CombatantEnter";
        public const string OnCombatantExit = "Combat.CombatantExit";
        public const string OnTargetChanged = "Combat.TargetChanged";
    }

    /// <summary>
    /// 伤害事件参数
    /// </summary>
    public readonly struct DamageEventArgs
    {
        /// <summary>伤害来源</summary>
        public readonly ICombatant Source;

        /// <summary>伤害目标</summary>
        public readonly ICombatant Target;

        /// <summary>伤害结果</summary>
        public readonly DamageResult Result;

        /// <summary>时间戳</summary>
        public readonly float Timestamp;

        public DamageEventArgs(ICombatant source, ICombatant target, DamageResult result)
        {
            Source = source;
            Target = target;
            Result = result;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// 治疗事件参数
    /// </summary>
    public readonly struct HealEventArgs
    {
        /// <summary>治疗来源</summary>
        public readonly ICombatant Source;

        /// <summary>治疗目标</summary>
        public readonly ICombatant Target;

        /// <summary>治疗量</summary>
        public readonly float Amount;

        /// <summary>实际治疗量</summary>
        public readonly float ActualAmount;

        /// <summary>是否过量治疗</summary>
        public readonly bool IsOverheal;

        /// <summary>时间戳</summary>
        public readonly float Timestamp;

        public HealEventArgs(ICombatant source, ICombatant target, float amount, float actualAmount)
        {
            Source = source;
            Target = target;
            Amount = amount;
            ActualAmount = actualAmount;
            IsOverheal = actualAmount < amount;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// 击杀事件参数
    /// </summary>
    public readonly struct KillEventArgs
    {
        /// <summary>击杀者</summary>
        public readonly ICombatant Killer;

        /// <summary>被击杀者</summary>
        public readonly ICombatant Victim;

        /// <summary>致死伤害</summary>
        public readonly DamageResult FatalDamage;

        /// <summary>时间戳</summary>
        public readonly float Timestamp;

        public KillEventArgs(ICombatant killer, ICombatant victim, DamageResult fatalDamage)
        {
            Killer = killer;
            Victim = victim;
            FatalDamage = fatalDamage;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// 战斗状态变更事件参数
    /// </summary>
    public readonly struct CombatStateEventArgs
    {
        /// <summary>战斗单位</summary>
        public readonly ICombatant Combatant;

        /// <summary>是否进入战斗</summary>
        public readonly bool IsEntering;

        /// <summary>时间戳</summary>
        public readonly float Timestamp;

        public CombatStateEventArgs(ICombatant combatant, bool isEntering)
        {
            Combatant = combatant;
            IsEntering = isEntering;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// 目标变更事件参数
    /// </summary>
    public readonly struct TargetChangedEventArgs
    {
        /// <summary>选择者</summary>
        public readonly ICombatant Selector;

        /// <summary>旧目标</summary>
        public readonly ICombatant OldTarget;

        /// <summary>新目标</summary>
        public readonly ICombatant NewTarget;

        /// <summary>时间戳</summary>
        public readonly float Timestamp;

        public TargetChangedEventArgs(ICombatant selector, ICombatant oldTarget, ICombatant newTarget)
        {
            Selector = selector;
            OldTarget = oldTarget;
            NewTarget = newTarget;
            Timestamp = Time.time;
        }
    }
}
