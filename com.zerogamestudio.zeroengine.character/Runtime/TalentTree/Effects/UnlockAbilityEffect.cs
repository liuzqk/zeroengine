using System;
using UnityEngine;

namespace ZeroEngine.TalentTree
{
    /// <summary>
    /// 解锁技能效果
    /// 天赋解锁时解锁或强化技能
    /// </summary>
    [Serializable]
    public class UnlockAbilityEffect : TalentEffect
    {
        [Header("技能配置")]
        [Tooltip("技能 ID")]
        public string AbilityId;

        [Tooltip("是否解锁（false 表示仅强化）")]
        public bool Unlock = true;

        [Tooltip("技能等级增加（每天赋等级）")]
        public int AbilityLevelPerTalentLevel = 1;

        private bool _isApplied;
        private GameObject _cachedOwner;

        public override void Apply(GameObject owner, int level)
        {
            if (owner == null || string.IsNullOrEmpty(AbilityId)) return;

            if (_isApplied)
            {
                Remove(owner);
            }

            var abilityProvider = owner.GetComponent<IAbilityProvider>();
            if (abilityProvider != null)
            {
                if (Unlock)
                {
                    abilityProvider.UnlockAbility(AbilityId);
                }

                int abilityLevel = AbilityLevelPerTalentLevel * level;
                if (abilityLevel > 0)
                {
                    abilityProvider.SetAbilityLevel(AbilityId, abilityLevel);
                }

                _isApplied = true;
                _cachedOwner = owner;
            }
            else
            {
                Debug.LogWarning($"[TalentTree] UnlockAbilityEffect: Owner {owner.name} 没有 IAbilityProvider");
            }
        }

        public override void Remove(GameObject owner)
        {
            if (!_isApplied || _cachedOwner == null) return;

            var abilityProvider = _cachedOwner.GetComponent<IAbilityProvider>();
            if (abilityProvider != null && Unlock)
            {
                abilityProvider.LockAbility(AbilityId);
            }

            _isApplied = false;
            _cachedOwner = null;
        }

        public override string GetDescription(int level)
        {
            if (Unlock)
            {
                return $"解锁技能: {AbilityId}";
            }
            else
            {
                int abilityLevel = AbilityLevelPerTalentLevel * level;
                return $"强化 {AbilityId} (+{abilityLevel}级)";
            }
        }
    }

    /// <summary>
    /// 技能提供者接口（解耦用）
    /// 项目层实现此接口以连接 AbilitySystem
    /// </summary>
    public interface IAbilityProvider
    {
        void UnlockAbility(string abilityId);
        void LockAbility(string abilityId);
        void SetAbilityLevel(string abilityId, int level);
    }
}
