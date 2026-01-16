using System;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 生命值接口
    /// </summary>
    public interface IHealth
    {
        /// <summary>当前生命值</summary>
        float CurrentHealth { get; }

        /// <summary>最大生命值</summary>
        float MaxHealth { get; }

        /// <summary>生命值百分比 (0-1)</summary>
        float HealthPercent { get; }

        /// <summary>是否存活</summary>
        bool IsAlive { get; }

        /// <summary>是否满血</summary>
        bool IsFullHealth { get; }

        /// <summary>是否无敌</summary>
        bool IsInvulnerable { get; set; }

        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <param name="damage">伤害数据</param>
        /// <returns>伤害结果</returns>
        DamageResult TakeDamage(DamageData damage);

        /// <summary>
        /// 接受治疗
        /// </summary>
        /// <param name="amount">治疗量</param>
        /// <param name="source">治疗来源</param>
        /// <returns>实际治疗量</returns>
        float Heal(float amount, ICombatant source = null);

        /// <summary>
        /// 设置生命值
        /// </summary>
        void SetHealth(float health);

        /// <summary>
        /// 设置最大生命值
        /// </summary>
        void SetMaxHealth(float maxHealth, bool healToFull = false);

        /// <summary>
        /// 恢复满血
        /// </summary>
        void RestoreToFull();

        /// <summary>生命值变化事件</summary>
        event Action<HealthChangeEventArgs> OnHealthChanged;

        /// <summary>受到伤害事件</summary>
        event Action<DamageEventArgs> OnDamageTaken;

        /// <summary>治疗事件</summary>
        event Action<HealEventArgs> OnHealed;

        /// <summary>死亡事件</summary>
        event Action<DeathEventArgs> OnDeath;

        /// <summary>复活事件</summary>
        event Action OnRevived;
    }

    /// <summary>
    /// 生命值变化事件参数
    /// </summary>
    public readonly struct HealthChangeEventArgs
    {
        /// <summary>变化前生命值</summary>
        public readonly float PreviousHealth;

        /// <summary>变化后生命值</summary>
        public readonly float CurrentHealth;

        /// <summary>最大生命值</summary>
        public readonly float MaxHealth;

        /// <summary>变化量（正为治疗，负为伤害）</summary>
        public readonly float Delta;

        /// <summary>变化类型</summary>
        public readonly HealthChangeType ChangeType;

        public HealthChangeEventArgs(float previousHealth, float currentHealth, float maxHealth, HealthChangeType changeType)
        {
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            Delta = currentHealth - previousHealth;
            ChangeType = changeType;
        }
    }

    /// <summary>
    /// 死亡事件参数
    /// </summary>
    public readonly struct DeathEventArgs
    {
        /// <summary>死亡者</summary>
        public readonly ICombatant Victim;

        /// <summary>击杀者</summary>
        public readonly ICombatant Killer;

        /// <summary>致死伤害</summary>
        public readonly DamageResult FatalDamage;

        /// <summary>死亡时间</summary>
        public readonly float Timestamp;

        public DeathEventArgs(ICombatant victim, ICombatant killer, DamageResult fatalDamage)
        {
            Victim = victim;
            Killer = killer;
            FatalDamage = fatalDamage;
            Timestamp = UnityEngine.Time.time;
        }
    }

    /// <summary>
    /// 生命值变化类型
    /// </summary>
    public enum HealthChangeType
    {
        /// <summary>伤害</summary>
        Damage,
        /// <summary>治疗</summary>
        Heal,
        /// <summary>直接设置</summary>
        Set,
        /// <summary>最大生命值变化</summary>
        MaxHealthChanged,
        /// <summary>复活</summary>
        Revive
    }
}
