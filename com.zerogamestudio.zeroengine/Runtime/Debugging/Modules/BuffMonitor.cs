using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.BuffSystem;
using ZeroEngine.Performance.Collections;

namespace ZeroEngine.Debugging
{
    /// <summary>
    /// Buff 系统监控模块
    /// </summary>
    public class BuffMonitor : IDebugModule
    {
        public const string MODULE_NAME = "BuffSystem";

        private readonly List<BuffReceiver> _trackedReceivers = new List<BuffReceiver>();
        private readonly List<BuffDebugData> _buffData = new List<BuffDebugData>();
        private readonly List<string> _eventLog = new List<string>();
        private const int MAX_LOG = 50;

        private bool _isEnabled = true;

        public string ModuleName => MODULE_NAME;
        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }

        /// <summary>Buff 事件</summary>
        public event Action<BuffDebugData, string> OnBuffEvent;

        /// <summary>
        /// 追踪 BuffReceiver
        /// </summary>
        public void TrackReceiver(BuffReceiver receiver)
        {
            if (receiver == null || _trackedReceivers.Contains(receiver)) return;

            _trackedReceivers.Add(receiver);

            // 订阅事件
            receiver.OnBuffApplied += OnBuffAppliedHandler;
            receiver.OnBuffRemoved += OnBuffRemovedHandler;
            receiver.OnBuffChanged += OnBuffChangedHandler;
        }

        /// <summary>
        /// 取消追踪 BuffReceiver
        /// </summary>
        public void UntrackReceiver(BuffReceiver receiver)
        {
            if (receiver == null) return;

            if (_trackedReceivers.Remove(receiver))
            {
                receiver.OnBuffApplied -= OnBuffAppliedHandler;
                receiver.OnBuffRemoved -= OnBuffRemovedHandler;
                receiver.OnBuffChanged -= OnBuffChangedHandler;
            }
        }

        private void OnBuffAppliedHandler(BuffHandler handler)
        {
            var data = CreateDebugData(handler);
            AddLog($"[+] {data.BuffName} applied (x{data.CurrentStacks})");
            OnBuffEvent?.Invoke(data, "Applied");
        }

        private void OnBuffRemovedHandler(BuffHandler handler, BuffEventType reason)
        {
            var data = CreateDebugData(handler);
            AddLog($"[-] {data.BuffName} removed ({reason})");
            OnBuffEvent?.Invoke(data, reason.ToString());
        }

        private void OnBuffChangedHandler(BuffEventArgs args)
        {
            var data = CreateDebugData(args.Buff);
            string change = args.EventType switch
            {
                BuffEventType.Stacked => $"stacked {args.OldStacks} → {args.NewStacks}",
                BuffEventType.Unstacked => $"unstacked {args.OldStacks} → {args.NewStacks}",
                BuffEventType.Refreshed => "refreshed",
                _ => args.EventType.ToString()
            };
            AddLog($"[*] {data.BuffName} {change}");
        }

        private BuffDebugData CreateDebugData(BuffHandler handler)
        {
            return new BuffDebugData
            {
                BuffId = handler.Data.BuffId,
                BuffName = handler.Data.name ?? handler.Data.BuffId,
                CurrentStacks = handler.CurrentStacks,
                MaxStacks = handler.Data.MaxStacks,
                RemainingTime = handler.RemainingTime,
                Duration = handler.Data.Duration,
                IsExpired = handler.IsExpired,
                Modifiers = new List<string>() // 可扩展
            };
        }

        private void AddLog(string message)
        {
            if (_eventLog.Count >= MAX_LOG)
            {
                _eventLog.RemoveAt(0);
            }
            _eventLog.Add($"[{Time.unscaledTime:F2}] {message}");
        }

        public void Update()
        {
            _buffData.Clear();

            foreach (var receiver in _trackedReceivers)
            {
                if (receiver == null) continue;

                foreach (var kvp in receiver.ActiveBuffs)
                {
                    _buffData.Add(CreateDebugData(kvp.Value));
                }
            }
        }

        public string GetSummary()
        {
            int totalBuffs = 0;
            int expiringBuffs = 0;

            foreach (var data in _buffData)
            {
                totalBuffs++;
                if (data.Duration > 0 && data.RemainingTime < 3f)
                    expiringBuffs++;
            }

            return $"Receivers: {_trackedReceivers.Count}, Active Buffs: {totalBuffs}, Expiring: {expiringBuffs}";
        }

        public IEnumerable<DebugEntry> GetEntries()
        {
            foreach (var data in _buffData)
            {
                var type = data.IsExpired ? DebugEntryType.Error :
                          data.RemainingTime < 3f ? DebugEntryType.Warning :
                          DebugEntryType.Info;

                yield return new DebugEntry(data.BuffName, data.ToString(), type);
            }
        }

        /// <summary>
        /// 获取 Buff 数据列表
        /// </summary>
        public IReadOnlyList<BuffDebugData> GetBuffData() => _buffData;

        /// <summary>
        /// 获取事件日志
        /// </summary>
        public IReadOnlyList<string> GetEventLog() => _eventLog;

        public void Clear()
        {
            _buffData.Clear();
            _eventLog.Clear();
        }
    }
}
