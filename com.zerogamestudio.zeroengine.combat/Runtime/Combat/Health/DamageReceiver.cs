using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 伤害接收器 - 处理伤害接收和分发的组件
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DamageReceiver : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private HealthComponent _healthComponent;
        [SerializeField] private bool _autoFindHealth = true;
        [SerializeField] private float _damageMultiplier = 1f;

        [Header("部位伤害")]
        [SerializeField] private string _hitZoneName = "Body";
        [SerializeField] private float _hitZoneMultiplier = 1f;

        [Header("状态")]
        [SerializeField] private bool _isActive = true;

        /// <summary>关联的战斗单位</summary>
        private ICombatant _combatant;

        /// <summary>伤害修正器列表</summary>
        private readonly List<IDamageReceiverModifier> _modifiers = new();

        /// <summary>生命值组件</summary>
        public HealthComponent Health => _healthComponent;

        /// <summary>是否激活</summary>
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        /// <summary>部位名称</summary>
        public string HitZoneName => _hitZoneName;

        /// <summary>部位伤害倍率</summary>
        public float HitZoneMultiplier => _hitZoneMultiplier;

        /// <summary>受到伤害事件</summary>
        public event Action<DamageReceiverHitEventArgs> OnHit;

        protected virtual void Awake()
        {
            if (_autoFindHealth && _healthComponent == null)
            {
                _healthComponent = GetComponentInParent<HealthComponent>();
            }

            _combatant = GetComponentInParent<ICombatant>();
        }

        /// <summary>
        /// 设置生命值组件
        /// </summary>
        public void SetHealthComponent(HealthComponent health)
        {
            _healthComponent = health;
        }

        /// <summary>
        /// 接收伤害
        /// </summary>
        /// <param name="damage">伤害数据</param>
        /// <param name="hitPoint">命中点</param>
        /// <param name="hitNormal">命中法线</param>
        /// <returns>伤害结果</returns>
        public DamageResult ReceiveDamage(DamageData damage, Vector3 hitPoint = default, Vector3 hitNormal = default)
        {
            if (!_isActive || _healthComponent == null)
            {
                return new DamageResult(damage, 0f);
            }

            // 应用伤害倍率
            float modifiedDamage = damage.BaseDamage * _damageMultiplier * _hitZoneMultiplier;

            // 应用修正器
            foreach (var modifier in _modifiers)
            {
                modifiedDamage = modifier.ModifyIncomingDamage(modifiedDamage, damage, this);
            }

            // 创建修改后的伤害数据
            var modifiedDamageData = damage.WithBaseDamage(modifiedDamage);

            // 传递给生命值组件
            var result = _healthComponent.TakeDamage(modifiedDamageData);

            // 触发命中事件
            var hitArgs = new DamageReceiverHitEventArgs(
                this,
                damage,
                result,
                hitPoint,
                hitNormal
            );
            OnHit?.Invoke(hitArgs);

            return result;
        }

        /// <summary>
        /// 注册伤害修正器
        /// </summary>
        public void RegisterModifier(IDamageReceiverModifier modifier)
        {
            if (modifier != null && !_modifiers.Contains(modifier))
            {
                _modifiers.Add(modifier);
            }
        }

        /// <summary>
        /// 注销伤害修正器
        /// </summary>
        public void UnregisterModifier(IDamageReceiverModifier modifier)
        {
            _modifiers.Remove(modifier);
        }

        /// <summary>
        /// 设置伤害倍率
        /// </summary>
        public void SetDamageMultiplier(float multiplier)
        {
            _damageMultiplier = multiplier;
        }

        /// <summary>
        /// 设置部位伤害倍率
        /// </summary>
        public void SetHitZoneMultiplier(float multiplier)
        {
            _hitZoneMultiplier = multiplier;
        }
    }

    /// <summary>
    /// 伤害接收器修正器接口
    /// </summary>
    public interface IDamageReceiverModifier
    {
        /// <summary>
        /// 修改接收的伤害
        /// </summary>
        float ModifyIncomingDamage(float damage, DamageData originalDamage, DamageReceiver receiver);
    }

    /// <summary>
    /// 伤害接收器命中事件参数
    /// </summary>
    public readonly struct DamageReceiverHitEventArgs
    {
        /// <summary>伤害接收器</summary>
        public readonly DamageReceiver Receiver;

        /// <summary>原始伤害数据</summary>
        public readonly DamageData OriginalDamage;

        /// <summary>伤害结果</summary>
        public readonly DamageResult Result;

        /// <summary>命中点</summary>
        public readonly Vector3 HitPoint;

        /// <summary>命中法线</summary>
        public readonly Vector3 HitNormal;

        /// <summary>时间戳</summary>
        public readonly float Timestamp;

        public DamageReceiverHitEventArgs(
            DamageReceiver receiver,
            DamageData originalDamage,
            DamageResult result,
            Vector3 hitPoint,
            Vector3 hitNormal)
        {
            Receiver = receiver;
            OriginalDamage = originalDamage;
            Result = result;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// 部位伤害配置
    /// </summary>
    [Serializable]
    public class HitZoneConfig
    {
        /// <summary>部位名称</summary>
        public string ZoneName = "Body";

        /// <summary>伤害倍率</summary>
        public float DamageMultiplier = 1f;

        /// <summary>是否为弱点</summary>
        public bool IsWeakPoint = false;

        /// <summary>是否为致命点</summary>
        public bool IsCriticalZone = false;

        /// <summary>额外暴击率</summary>
        public float BonusCritChance = 0f;
    }
}
