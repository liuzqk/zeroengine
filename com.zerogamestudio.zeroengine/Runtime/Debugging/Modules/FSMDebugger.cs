using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.FSM;

namespace ZeroEngine.Debugging
{
    /// <summary>
    /// FSM 状态机调试模块
    /// </summary>
    public class FSMDebugger : IDebugModule
    {
        public const string MODULE_NAME = "FSM";
        private const int MAX_HISTORY = 50;

        private readonly List<TrackedFSM> _trackedFSMs = new List<TrackedFSM>();
        private readonly List<FSMTransitionRecord> _transitionHistory = new List<FSMTransitionRecord>();

        private bool _isEnabled = true;

        public string ModuleName => MODULE_NAME;
        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }

        /// <summary>状态转换事件</summary>
        public event Action<FSMTransitionRecord> OnStateTransition;

        private struct TrackedFSM
        {
            public StateMachine Machine;
            public string LastState;
            public string Name;
        }

        /// <summary>
        /// 追踪状态机
        /// </summary>
        public void TrackFSM(StateMachine machine, string name = null)
        {
            if (machine == null) return;

            // 检查是否已追踪
            for (int i = 0; i < _trackedFSMs.Count; i++)
            {
                if (_trackedFSMs[i].Machine == machine) return;
            }

            _trackedFSMs.Add(new TrackedFSM
            {
                Machine = machine,
                LastState = machine.CurrentNode,
                Name = name ?? $"FSM_{_trackedFSMs.Count}"
            });
        }

        /// <summary>
        /// 取消追踪状态机
        /// </summary>
        public void UntrackFSM(StateMachine machine)
        {
            for (int i = _trackedFSMs.Count - 1; i >= 0; i--)
            {
                if (_trackedFSMs[i].Machine == machine)
                {
                    _trackedFSMs.RemoveAt(i);
                    break;
                }
            }
        }

        public void Update()
        {
            for (int i = 0; i < _trackedFSMs.Count; i++)
            {
                var tracked = _trackedFSMs[i];
                if (tracked.Machine == null) continue;

                string currentState = tracked.Machine.CurrentNode;

                // 检测状态变化
                if (currentState != tracked.LastState)
                {
                    var record = new FSMTransitionRecord
                    {
                        FromState = tracked.LastState ?? "(none)",
                        ToState = currentState ?? "(none)",
                        Timestamp = Time.unscaledTime,
                        Trigger = tracked.Name
                    };

                    AddTransitionRecord(record);
                    OnStateTransition?.Invoke(record);

                    // 更新 LastState
                    tracked.LastState = currentState;
                    _trackedFSMs[i] = tracked;
                }
            }
        }

        private void AddTransitionRecord(FSMTransitionRecord record)
        {
            if (_transitionHistory.Count >= MAX_HISTORY)
            {
                _transitionHistory.RemoveAt(0);
            }
            _transitionHistory.Add(record);
        }

        public string GetSummary()
        {
            int activeCount = 0;
            foreach (var tracked in _trackedFSMs)
            {
                if (!string.IsNullOrEmpty(tracked.Machine?.CurrentNode))
                    activeCount++;
            }

            return $"FSMs: {_trackedFSMs.Count}, Active: {activeCount}, Transitions: {_transitionHistory.Count}";
        }

        public IEnumerable<DebugEntry> GetEntries()
        {
            // 当前状态
            foreach (var tracked in _trackedFSMs)
            {
                if (tracked.Machine == null) continue;

                string state = tracked.Machine.CurrentNode ?? "(none)";
                string prev = tracked.Machine.PreviousNode ?? "(none)";

                yield return DebugEntry.Info(tracked.Name, $"Current: {GetShortName(state)} (prev: {GetShortName(prev)})");
            }

            // 最近的转换历史（倒序显示最新的 5 条）
            int count = Math.Min(5, _transitionHistory.Count);
            for (int i = _transitionHistory.Count - 1; i >= _transitionHistory.Count - count; i--)
            {
                var record = _transitionHistory[i];
                yield return DebugEntry.Warning("Transition", record.ToString());
            }
        }

        private string GetShortName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "(none)";
            int lastDot = fullName.LastIndexOf('.');
            return lastDot >= 0 ? fullName.Substring(lastDot + 1) : fullName;
        }

        /// <summary>
        /// 获取转换历史
        /// </summary>
        public IReadOnlyList<FSMTransitionRecord> GetTransitionHistory() => _transitionHistory;

        /// <summary>
        /// 获取指定 FSM 的当前状态
        /// </summary>
        public string GetCurrentState(StateMachine machine)
        {
            foreach (var tracked in _trackedFSMs)
            {
                if (tracked.Machine == machine)
                    return tracked.Machine.CurrentNode;
            }
            return null;
        }

        public void Clear()
        {
            _transitionHistory.Clear();
        }
    }
}
