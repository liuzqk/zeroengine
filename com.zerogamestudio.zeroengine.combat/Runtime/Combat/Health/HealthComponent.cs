using System;
using UnityEngine;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 生命值组件 - 实现 IHealth 接口的 MonoBehaviour
    /// </summary>
    public class HealthComponent : MonoBehaviour, IHealth
    {
        [Header("生命值配置")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth = 100f;
        [SerializeField] private bool _startAtFullHealth = true;

        [Header("状态")]
        [SerializeField] private bool _isInvulnerable;
        [SerializeField] private bool _isAlive = true;

        [Header("伤害处理")]
        [SerializeField] private bool _allowOverkill = false;
        [SerializeField] private float _minDamage = 0f;

        /// <summary>关联的战斗单位</summary>
        private ICombatant _combatant;

        #region IHealth 实现

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float HealthPercent => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
        public bool IsAlive => _isAlive;
        public bool IsFullHealth => Mathf.Approximately(_currentHealth, _maxHealth);

        public bool IsInvulnerable
        {
            get => _isInvulnerable;
            set => _isInvulnerable = value;
        }

        public event Action<HealthChangeEventArgs> OnHealthChanged;
        public event Action<DamageEventArgs> OnDamageTaken;
        public event Action<HealEventArgs> OnHealed;
        public event Action<DeathEventArgs> OnDeath;
        public event Action OnRevived;

        #endregion

        protected virtual void Awake()
        {
            _combatant = GetComponent<ICombatant>();

            if (_startAtFullHealth)
            {
                _currentHealth = _maxHealth;
            }

            _isAlive = _currentHealth > 0;
        }

        /// <summary>
        /// 设置关联的战斗单位
        /// </summary>
        public void SetCombatant(ICombatant combatant)
        {
            _combatant = combatant;
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public virtual DamageResult TakeDamage(DamageData damage)
        {
            if (!_isAlive)
            {
                return new DamageResult(damage, 0f);
            }

            // 无敌检查
            if (_isInvulnerable && !damage.HasFlag(DamageFlags.IgnoreInvulnerable))
            {
                return DamageResult.Immune(damage);
            }

            float previousHealth = _currentHealth;
            float actualDamage = Mathf.Max(damage.BaseDamage, _minDamage);

            // 计算实际扣除的生命值
            float healthDelta = _allowOverkill ? actualDamage : Mathf.Min(actualDamage, _currentHealth);
            _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);

            bool isKill = previousHealth > 0 && _currentHealth <= 0;

            var result = new DamageResult(
                damage,
                healthDelta,
                isKill: isKill
            );

            // 触发事件
            var changeArgs = new HealthChangeEventArgs(previousHealth, _currentHealth, _maxHealth, HealthChangeType.Damage);
            OnHealthChanged?.Invoke(changeArgs);

            var damageArgs = new DamageEventArgs(damage.Source, _combatant, result);
            OnDamageTaken?.Invoke(damageArgs);

            // 死亡处理
            if (isKill)
            {
                HandleDeath(damage.Source, result);
            }

            return result;
        }

        /// <summary>
        /// 接受治疗
        /// </summary>
        public virtual float Heal(float amount, ICombatant source = null)
        {
            if (!_isAlive || amount <= 0)
            {
                return 0f;
            }

            float previousHealth = _currentHealth;
            float actualHeal = Mathf.Min(amount, _maxHealth - _currentHealth);

            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            actualHeal = _currentHealth - previousHealth;

            if (actualHeal > 0)
            {
                var changeArgs = new HealthChangeEventArgs(previousHealth, _currentHealth, _maxHealth, HealthChangeType.Heal);
                OnHealthChanged?.Invoke(changeArgs);

                var healArgs = new HealEventArgs(source, _combatant, amount, actualHeal);
                OnHealed?.Invoke(healArgs);
            }

            return actualHeal;
        }

        /// <summary>
        /// 设置生命值
        /// </summary>
        public virtual void SetHealth(float health)
        {
            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(health, 0, _maxHealth);

            if (!Mathf.Approximately(previousHealth, _currentHealth))
            {
                bool wasAlive = _isAlive;
                _isAlive = _currentHealth > 0;

                var changeArgs = new HealthChangeEventArgs(previousHealth, _currentHealth, _maxHealth, HealthChangeType.Set);
                OnHealthChanged?.Invoke(changeArgs);

                // 设置为 0 时触发死亡
                if (wasAlive && !_isAlive)
                {
                    var fakeResult = new DamageResult(default, previousHealth, isKill: true);
                    HandleDeath(null, fakeResult);
                }
                // 从 0 恢复时触发复活
                else if (!wasAlive && _isAlive)
                {
                    OnRevived?.Invoke();
                }
            }
        }

        /// <summary>
        /// 设置最大生命值
        /// </summary>
        public virtual void SetMaxHealth(float maxHealth, bool healToFull = false)
        {
            float previousMax = _maxHealth;
            _maxHealth = Mathf.Max(1f, maxHealth);

            float previousHealth = _currentHealth;

            if (healToFull)
            {
                _currentHealth = _maxHealth;
            }
            else
            {
                // 保持百分比
                float percent = previousMax > 0 ? previousHealth / previousMax : 1f;
                _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            }

            if (!Mathf.Approximately(previousHealth, _currentHealth))
            {
                var changeArgs = new HealthChangeEventArgs(previousHealth, _currentHealth, _maxHealth, HealthChangeType.MaxHealthChanged);
                OnHealthChanged?.Invoke(changeArgs);
            }
        }

        /// <summary>
        /// 恢复满血
        /// </summary>
        public virtual void RestoreToFull()
        {
            float previousHealth = _currentHealth;
            _currentHealth = _maxHealth;

            if (!Mathf.Approximately(previousHealth, _currentHealth))
            {
                var changeArgs = new HealthChangeEventArgs(previousHealth, _currentHealth, _maxHealth, HealthChangeType.Heal);
                OnHealthChanged?.Invoke(changeArgs);
            }
        }

        /// <summary>
        /// 复活
        /// </summary>
        public virtual void Revive(float healthPercent = 1f)
        {
            if (_isAlive) return;

            float previousHealth = _currentHealth;
            _currentHealth = _maxHealth * Mathf.Clamp01(healthPercent);
            _isAlive = true;

            var changeArgs = new HealthChangeEventArgs(previousHealth, _currentHealth, _maxHealth, HealthChangeType.Revive);
            OnHealthChanged?.Invoke(changeArgs);

            OnRevived?.Invoke();
        }

        /// <summary>
        /// 处理死亡
        /// </summary>
        protected virtual void HandleDeath(ICombatant killer, DamageResult fatalDamage)
        {
            _isAlive = false;

            var deathArgs = new DeathEventArgs(_combatant, killer, fatalDamage);
            OnDeath?.Invoke(deathArgs);
        }

        /// <summary>
        /// 应用百分比伤害
        /// </summary>
        public DamageResult TakePercentDamage(float percent, ICombatant source = null, DamageType damageType = DamageType.True)
        {
            float damage = _maxHealth * Mathf.Clamp01(percent);
            var damageData = new DamageData
            {
                BaseDamage = damage,
                DamageType = damageType,
                Source = source,
                SourceType = DamageSourceType.Effect
            };
            return TakeDamage(damageData);
        }

        /// <summary>
        /// 应用百分比治疗
        /// </summary>
        public float HealPercent(float percent, ICombatant source = null)
        {
            float amount = _maxHealth * Mathf.Clamp01(percent);
            return Heal(amount, source);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maxHealth = Mathf.Max(1f, _maxHealth);
            _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
        }
#endif
    }
}
