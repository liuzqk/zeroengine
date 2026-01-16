using System;
using UnityEngine;
using ZeroEngine.Combat;

namespace ZeroEngine.RPG.TurnBased
{
    /// <summary>
    /// 回合制战斗单位基类 - 提供 ITurnBasedCombatant 的默认实现
    /// </summary>
    public abstract class TurnBasedCombatantBase : MonoBehaviour, ITurnBasedCombatant
    {
        #region 序列化字段

        [Header("基本信息")]
        [SerializeField] protected string _combatantId;
        [SerializeField] protected string _displayName;
        [SerializeField] protected int _teamId;
        [SerializeField] protected bool _isPlayerControlled;

        [Header("属性")]
        [SerializeField] protected int _speed = 100;

        #endregion

        #region ICombatant 实现

        public virtual string CombatantId => string.IsNullOrEmpty(_combatantId) ? gameObject.name : _combatantId;
        public virtual string DisplayName => string.IsNullOrEmpty(_displayName) ? gameObject.name : _displayName;
        public virtual int TeamId => _teamId;
        public abstract bool IsAlive { get; }
        public virtual bool IsTargetable => IsAlive;
        public GameObject GameObject => gameObject;
        public Transform Transform => transform;

        public virtual Vector3 GetCombatPosition() => transform.position;

        public abstract DamageResult TakeDamage(DamageData damage);
        public abstract float ReceiveHeal(float amount, ICombatant source = null);

        public virtual void OnEnterCombat()
        {
            Debug.Log($"[{DisplayName}] 进入战斗");
        }

        public virtual void OnExitCombat()
        {
            Debug.Log($"[{DisplayName}] 离开战斗");
        }

        #endregion

        #region ITurnBasedCombatant 实现

        public virtual int Speed => _speed;

        /// <summary>
        /// 是否可以行动 (可被子类重写以添加眩晕、破盾等检查)
        /// </summary>
        public virtual bool CanAct => IsAlive;

        /// <summary>
        /// 本回合是否已行动
        /// </summary>
        public bool HasActed { get; set; }

        /// <summary>
        /// 是否为玩家控制
        /// </summary>
        public virtual bool IsPlayerControlled => _isPlayerControlled;

        /// <summary>
        /// 回合开始时调用
        /// </summary>
        public virtual void OnTurnStart()
        {
            // 子类可重写以实现 BP 恢复、Buff 结算等
        }

        /// <summary>
        /// 回合结束时调用
        /// </summary>
        public virtual void OnTurnEnd()
        {
            // 子类可重写以实现状态衰减等
        }

        /// <summary>
        /// 重置回合状态
        /// </summary>
        public virtual void ResetTurnState()
        {
            HasActed = false;
        }

        #endregion

        #region 编辑器辅助

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(_combatantId))
            {
                _combatantId = gameObject.name;
            }
            if (string.IsNullOrEmpty(_displayName))
            {
                _displayName = gameObject.name;
            }
        }

        #endregion
    }
}
