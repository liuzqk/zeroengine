using UnityEngine;

namespace ZeroEngine.AbilitySystem
{
    /// <summary>
    /// 技能施放上下文 — 统一描述"谁对谁施放了什么"
    /// 实时和Tick模式共用
    /// </summary>
    public interface IAbilityContext
    {
        /// <summary>施放者</summary>
        IAbilitySource Caster { get; }

        /// <summary>目标（可为null=自身/无目标）</summary>
        IAbilityTarget Target { get; }

        /// <summary>技能实例</summary>
        AbilityInstance Ability { get; }

        /// <summary>效果倍率（含等级加成等）</summary>
        float EffectMultiplier { get; }

        /// <summary>施放位置</summary>
        Vector3 CastPosition { get; }

        /// <summary>
        /// 自定义数据槽 — 项目特定数据通过此传递
        /// 例如 P6 传入 ThreatTable 引用，POB 传入 Core 引用
        /// </summary>
        object UserData { get; set; }
    }

    /// <summary>
    /// 默认实现
    /// </summary>
    public class AbilityContext : IAbilityContext
    {
        public IAbilitySource Caster { get; set; }
        public IAbilityTarget Target { get; set; }
        public AbilityInstance Ability { get; set; }
        public float EffectMultiplier { get; set; } = 1f;
        public Vector3 CastPosition { get; set; }
        public object UserData { get; set; }

        private static readonly AbilityContext _shared = new();

        /// <summary>复用实例，减少GC（单线程场景安全）</summary>
        public static AbilityContext Get(IAbilitySource caster, IAbilityTarget target,
            AbilityInstance ability)
        {
            _shared.Caster = caster;
            _shared.Target = target;
            _shared.Ability = ability;
            _shared.EffectMultiplier = ability?.EffectMultiplier ?? 1f;
            _shared.CastPosition = caster?.Transform?.position ?? Vector3.zero;
            _shared.UserData = null;
            return _shared;
        }
    }
}
