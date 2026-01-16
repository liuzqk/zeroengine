using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 战斗上下文 - 管理单场战斗的状态和参与者
    /// </summary>
    public class CombatContext
    {
        /// <summary>上下文ID</summary>
        public string ContextId { get; }

        /// <summary>战斗状态</summary>
        public CombatState State { get; private set; } = CombatState.None;

        /// <summary>战斗开始时间</summary>
        public float StartTime { get; private set; }

        /// <summary>战斗结束时间</summary>
        public float EndTime { get; private set; }

        /// <summary>战斗持续时间</summary>
        public float Duration => State == CombatState.Active ? Time.time - StartTime : EndTime - StartTime;

        /// <summary>参与战斗的单位</summary>
        private readonly HashSet<ICombatant> _participants = new();

        /// <summary>战斗统计</summary>
        private readonly Dictionary<string, CombatStatistics> _statistics = new();

        /// <summary>只读参与者集合</summary>
        public IReadOnlyCollection<ICombatant> Participants => _participants;

        /// <summary>自定义数据存储</summary>
        private readonly Dictionary<string, object> _customData = new();

        /// <summary>状态变更事件</summary>
        public event Action<CombatState, CombatState> OnStateChanged;

        /// <summary>参与者加入事件</summary>
        public event Action<ICombatant> OnParticipantJoined;

        /// <summary>参与者离开事件</summary>
        public event Action<ICombatant> OnParticipantLeft;

        public CombatContext(string contextId = null)
        {
            ContextId = contextId ?? Guid.NewGuid().ToString();
        }

        #region 状态管理

        /// <summary>
        /// 开始战斗
        /// </summary>
        internal void Start()
        {
            if (State != CombatState.None) return;

            SetState(CombatState.Active);
            StartTime = Time.time;

            // 通知所有参与者进入战斗
            foreach (var participant in _participants)
            {
                participant.OnEnterCombat();
            }
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        internal void End()
        {
            if (State != CombatState.Active) return;

            SetState(CombatState.Ended);
            EndTime = Time.time;

            // 通知所有参与者退出战斗
            foreach (var participant in _participants)
            {
                participant.OnExitCombat();
            }
        }

        private void SetState(CombatState newState)
        {
            if (State == newState) return;

            var oldState = State;
            State = newState;
            OnStateChanged?.Invoke(oldState, newState);
        }

        #endregion

        #region 参与者管理

        /// <summary>
        /// 添加参与者
        /// </summary>
        public void AddParticipant(ICombatant combatant)
        {
            if (combatant == null) return;

            if (_participants.Add(combatant))
            {
                // 初始化统计数据
                if (!_statistics.ContainsKey(combatant.CombatantId))
                {
                    _statistics[combatant.CombatantId] = new CombatStatistics();
                }

                // 如果战斗已开始，通知进入战斗
                if (State == CombatState.Active)
                {
                    combatant.OnEnterCombat();
                }

                OnParticipantJoined?.Invoke(combatant);
            }
        }

        /// <summary>
        /// 移除参与者
        /// </summary>
        public void RemoveParticipant(ICombatant combatant)
        {
            if (combatant == null) return;

            if (_participants.Remove(combatant))
            {
                // 如果战斗进行中，通知退出战斗
                if (State == CombatState.Active)
                {
                    combatant.OnExitCombat();
                }

                OnParticipantLeft?.Invoke(combatant);
            }
        }

        /// <summary>
        /// 检查是否包含参与者
        /// </summary>
        public bool HasParticipant(ICombatant combatant)
        {
            return _participants.Contains(combatant);
        }

        /// <summary>
        /// 获取指定队伍的参与者
        /// </summary>
        public IEnumerable<ICombatant> GetParticipantsByTeam(int teamId)
        {
            foreach (var participant in _participants)
            {
                if (participant.TeamId == teamId)
                {
                    yield return participant;
                }
            }
        }

        /// <summary>
        /// 获取存活的参与者
        /// </summary>
        public IEnumerable<ICombatant> GetAliveParticipants()
        {
            foreach (var participant in _participants)
            {
                if (participant.IsAlive)
                {
                    yield return participant;
                }
            }
        }

        #endregion

        #region 统计数据

        /// <summary>
        /// 获取战斗统计
        /// </summary>
        public CombatStatistics GetStatistics(ICombatant combatant)
        {
            if (combatant == null) return null;
            return _statistics.GetValueOrDefault(combatant.CombatantId);
        }

        /// <summary>
        /// 获取战斗统计
        /// </summary>
        public CombatStatistics GetStatistics(string combatantId)
        {
            return _statistics.GetValueOrDefault(combatantId);
        }

        /// <summary>
        /// 记录造成伤害
        /// </summary>
        public void RecordDamageDealt(ICombatant source, float damage, bool isCritical)
        {
            if (source == null) return;

            var stats = GetStatistics(source);
            if (stats != null)
            {
                stats.TotalDamageDealt += damage;
                stats.HitCount++;
                if (isCritical) stats.CriticalHitCount++;
            }
        }

        /// <summary>
        /// 记录受到伤害
        /// </summary>
        public void RecordDamageTaken(ICombatant target, float damage)
        {
            if (target == null) return;

            var stats = GetStatistics(target);
            if (stats != null)
            {
                stats.TotalDamageTaken += damage;
            }
        }

        /// <summary>
        /// 记录治疗
        /// </summary>
        public void RecordHealing(ICombatant source, float amount)
        {
            if (source == null) return;

            var stats = GetStatistics(source);
            if (stats != null)
            {
                stats.TotalHealing += amount;
            }
        }

        /// <summary>
        /// 记录击杀
        /// </summary>
        public void RecordKill(ICombatant killer)
        {
            if (killer == null) return;

            var stats = GetStatistics(killer);
            if (stats != null)
            {
                stats.KillCount++;
            }
        }

        /// <summary>
        /// 记录死亡
        /// </summary>
        public void RecordDeath(ICombatant victim)
        {
            if (victim == null) return;

            var stats = GetStatistics(victim);
            if (stats != null)
            {
                stats.DeathCount++;
            }
        }

        #endregion

        #region 自定义数据

        /// <summary>
        /// 设置自定义数据
        /// </summary>
        public void SetData<T>(string key, T value)
        {
            _customData[key] = value;
        }

        /// <summary>
        /// 获取自定义数据
        /// </summary>
        public T GetData<T>(string key, T defaultValue = default)
        {
            if (_customData.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 检查是否存在自定义数据
        /// </summary>
        public bool HasData(string key)
        {
            return _customData.ContainsKey(key);
        }

        /// <summary>
        /// 移除自定义数据
        /// </summary>
        public bool RemoveData(string key)
        {
            return _customData.Remove(key);
        }

        #endregion
    }

    /// <summary>
    /// 战斗状态
    /// </summary>
    public enum CombatState
    {
        /// <summary>未开始</summary>
        None,
        /// <summary>进行中</summary>
        Active,
        /// <summary>已结束</summary>
        Ended
    }

    /// <summary>
    /// 战斗统计数据
    /// </summary>
    public class CombatStatistics
    {
        /// <summary>造成的总伤害</summary>
        public float TotalDamageDealt;

        /// <summary>受到的总伤害</summary>
        public float TotalDamageTaken;

        /// <summary>总治疗量</summary>
        public float TotalHealing;

        /// <summary>击杀数</summary>
        public int KillCount;

        /// <summary>死亡数</summary>
        public int DeathCount;

        /// <summary>命中次数</summary>
        public int HitCount;

        /// <summary>暴击次数</summary>
        public int CriticalHitCount;

        /// <summary>暴击率</summary>
        public float CriticalRate => HitCount > 0 ? (float)CriticalHitCount / HitCount : 0f;

        /// <summary>DPS (每秒伤害)</summary>
        public float GetDPS(float duration)
        {
            return duration > 0 ? TotalDamageDealt / duration : 0f;
        }

        /// <summary>重置统计</summary>
        public void Reset()
        {
            TotalDamageDealt = 0f;
            TotalDamageTaken = 0f;
            TotalHealing = 0f;
            KillCount = 0;
            DeathCount = 0;
            HitCount = 0;
            CriticalHitCount = 0;
        }
    }
}
