using System;
using UnityEngine;

namespace ZeroEngine.TalentTree
{
    /// <summary>
    /// Buff 效果
    /// 天赋解锁时添加永久 Buff
    /// </summary>
    [Serializable]
    public class BuffEffect : TalentEffect
    {
        [Header("Buff 配置")]
        [Tooltip("Buff ID（需要在 BuffSystem 中定义）")]
        public string BuffId;

        [Tooltip("Buff 层数（每级增加）")]
        public int StacksPerLevel = 1;

        [Tooltip("基础层数")]
        public int BaseStacks = 1;

        // 用于追踪是否已应用
        private bool _isApplied;
        private GameObject _cachedOwner;
        private int _appliedStacks;

        public override void Apply(GameObject owner, int level)
        {
            if (owner == null || string.IsNullOrEmpty(BuffId)) return;

            // 如果已经应用，先移除
            if (_isApplied)
            {
                Remove(owner);
            }

            // 计算层数
            int stacks = BaseStacks + StacksPerLevel * level;

            // 通过事件系统或直接引用应用 Buff
            // 这里使用解耦方式，通过接口或事件
            var buffProvider = owner.GetComponent<IBuffProvider>();
            if (buffProvider != null)
            {
                buffProvider.ApplyBuff(BuffId, stacks, this);
                _isApplied = true;
                _cachedOwner = owner;
                _appliedStacks = stacks;
            }
            else
            {
                Debug.LogWarning($"[TalentTree] BuffEffect: Owner {owner.name} 没有 IBuffProvider");
            }
        }

        public override void Remove(GameObject owner)
        {
            if (!_isApplied || _cachedOwner == null) return;

            var buffProvider = _cachedOwner.GetComponent<IBuffProvider>();
            buffProvider?.RemoveBuff(BuffId, this);

            _isApplied = false;
            _cachedOwner = null;
            _appliedStacks = 0;
        }

        public override string GetDescription(int level)
        {
            int stacks = BaseStacks + StacksPerLevel * level;
            return $"获得 {BuffId} Buff (x{stacks})";
        }
    }

    /// <summary>
    /// Buff 提供者接口（解耦用）
    /// 项目层实现此接口以连接 BuffSystem
    /// </summary>
    public interface IBuffProvider
    {
        void ApplyBuff(string buffId, int stacks, object source);
        void RemoveBuff(string buffId, object source);
    }
}
