using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.StatSystem;

namespace ZeroEngine.BuffSystem
{
    /// <summary>
    /// Buff 事件参数 (v1.2.0+)
    /// </summary>
    public struct BuffEventArgs
    {
        public BuffHandler Buff;
        public BuffEventType EventType;
        public int OldStacks;
        public int NewStacks;
        public int StackDelta => NewStacks - OldStacks;

        public BuffEventArgs(BuffHandler buff, BuffEventType eventType, int oldStacks, int newStacks)
        {
            Buff = buff;
            EventType = eventType;
            OldStacks = oldStacks;
            NewStacks = newStacks;
        }
    }

    /// <summary>
    /// 管理实体上的所有 Buff
    /// </summary>
    public class BuffReceiver : MonoBehaviour
    {
        [SerializeField] private StatController _statController;

        private Dictionary<string, BuffHandler> _activeBuffs = new Dictionary<string, BuffHandler>();
        private List<BuffHandler> _tickList = new List<BuffHandler>();

        /// <summary>
        /// 当前激活的 Buff 列表
        /// </summary>
        public IReadOnlyDictionary<string, BuffHandler> ActiveBuffs => _activeBuffs;

        // v1.2.0+ 事件系统
        /// <summary>
        /// 任意 Buff 状态变化时触发
        /// </summary>
        public event Action<BuffEventArgs> OnBuffChanged;

        /// <summary>
        /// 新 Buff 施加时触发
        /// </summary>
        public event Action<BuffHandler> OnBuffApplied;

        /// <summary>
        /// Buff 被移除时触发（包括过期）
        /// </summary>
        public event Action<BuffHandler, BuffEventType> OnBuffRemoved;

        /// <summary>
        /// 添加 Buff
        /// </summary>
        public BuffHandler AddBuff(BuffData data, int stacks = 1)
        {
            if (data == null) return null;

            bool isNewBuff = !_activeBuffs.TryGetValue(data.BuffId, out var handler);

            if (isNewBuff)
            {
                handler = new BuffHandler(data, _statController);
                handler.OnExpired += OnBuffExpiredInternal;
                _activeBuffs.Add(data.BuffId, handler);
            }

            int oldStacks = handler.CurrentStacks;

            // 根据 StackMode 处理
            switch (data.StackMode)
            {
                case BuffStackMode.Stack:
                    handler.AddStacks(stacks);
                    break;

                case BuffStackMode.Refresh:
                    if (isNewBuff)
                    {
                        handler.AddStacks(stacks);
                    }
                    else
                    {
                        handler.RefreshDuration();
                    }
                    break;

                case BuffStackMode.Replace:
                    if (!isNewBuff)
                    {
                        handler.ClearModifiers();
                        handler.ResetStacks();
                    }
                    handler.AddStacks(stacks);
                    break;
            }

            int newStacks = handler.CurrentStacks;

            // 触发事件
            if (isNewBuff)
            {
                OnBuffApplied?.Invoke(handler);
                OnBuffChanged?.Invoke(new BuffEventArgs(handler, BuffEventType.Applied, 0, newStacks));
            }
            else if (data.StackMode == BuffStackMode.Refresh)
            {
                OnBuffChanged?.Invoke(new BuffEventArgs(handler, BuffEventType.Refreshed, oldStacks, newStacks));
            }
            else if (newStacks > oldStacks)
            {
                OnBuffChanged?.Invoke(new BuffEventArgs(handler, BuffEventType.Stacked, oldStacks, newStacks));
            }

            return handler;
        }

        /// <summary>
        /// 移除 Buff 层数
        /// </summary>
        public void RemoveBuff(string buffId, int stacks = 1)
        {
            if (_activeBuffs.TryGetValue(buffId, out var handler))
            {
                int oldStacks = handler.CurrentStacks;
                handler.RemoveStacks(stacks);
                int newStacks = handler.CurrentStacks;

                if (handler.IsExpired)
                {
                    RemoveBuffInternal(handler, BuffEventType.Removed);
                }
                else if (newStacks < oldStacks)
                {
                    OnBuffChanged?.Invoke(new BuffEventArgs(handler, BuffEventType.Unstacked, oldStacks, newStacks));
                }
            }
        }

        /// <summary>
        /// 移除所有 Buff 层数
        /// </summary>
        public void RemoveBuffCompletely(string buffId)
        {
            if (_activeBuffs.TryGetValue(buffId, out var handler))
            {
                int oldStacks = handler.CurrentStacks;
                handler.ForceExpire();
                RemoveBuffInternal(handler, BuffEventType.Removed);
            }
        }

        /// <summary>
        /// 移除所有 Buff
        /// </summary>
        public void RemoveAllBuffs()
        {
            _tickList.Clear();
            _tickList.AddRange(_activeBuffs.Values);

            foreach (var handler in _tickList)
            {
                handler.ForceExpire();
                RemoveBuffInternal(handler, BuffEventType.Removed);
            }
        }

        /// <summary>
        /// 检查是否拥有指定 Buff
        /// </summary>
        public bool HasBuff(string buffId) => _activeBuffs.ContainsKey(buffId);

        /// <summary>
        /// 获取指定 Buff
        /// </summary>
        public BuffHandler GetBuff(string buffId)
        {
            _activeBuffs.TryGetValue(buffId, out var handler);
            return handler;
        }

        /// <summary>
        /// 获取指定 Buff 的层数
        /// </summary>
        public int GetBuffStacks(string buffId)
        {
            if (_activeBuffs.TryGetValue(buffId, out var handler))
            {
                return handler.CurrentStacks;
            }
            return 0;
        }

        private void Update()
        {
            if (_activeBuffs.Count == 0) return;

            _tickList.Clear();
            _tickList.AddRange(_activeBuffs.Values);

            for (int i = 0; i < _tickList.Count; i++)
            {
                _tickList[i].Tick(Time.deltaTime);
            }
        }

        private void OnBuffExpiredInternal(BuffHandler handler)
        {
            RemoveBuffInternal(handler, BuffEventType.Expired);
        }

        private void RemoveBuffInternal(BuffHandler handler, BuffEventType reason)
        {
            if (_activeBuffs.ContainsKey(handler.Data.BuffId))
            {
                _activeBuffs.Remove(handler.Data.BuffId);
            }
            handler.OnExpired -= OnBuffExpiredInternal;

            OnBuffRemoved?.Invoke(handler, reason);
            OnBuffChanged?.Invoke(new BuffEventArgs(handler, reason, handler.CurrentStacks, 0));
        }

        private void Awake()
        {
            if (_statController == null) _statController = GetComponent<StatController>();
        }
    }
}
