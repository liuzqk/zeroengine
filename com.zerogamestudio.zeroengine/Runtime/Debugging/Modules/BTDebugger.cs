using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.BehaviorTree;
using ZeroEngine.Performance.Collections;

namespace ZeroEngine.Debugging
{
    /// <summary>
    /// BehaviorTree 调试模块
    /// </summary>
    public class BTDebugger : IDebugModule
    {
        public const string MODULE_NAME = "BehaviorTree";
        private const int MAX_HISTORY = 100;

        private readonly List<BehaviorTree.BehaviorTree> _trackedTrees = new List<BehaviorTree.BehaviorTree>();
        private readonly List<BTNodeDebugData> _nodeData = new List<BTNodeDebugData>();
        private readonly Queue<string> _executionHistory = new Queue<string>();

        private bool _isEnabled = true;

        public string ModuleName => MODULE_NAME;
        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }

        /// <summary>当前追踪的行为树列表</summary>
        public IReadOnlyList<BehaviorTree.BehaviorTree> TrackedTrees => _trackedTrees;

        /// <summary>节点执行事件</summary>
        public event Action<BTNodeDebugData> OnNodeExecuted;

        /// <summary>
        /// 追踪行为树
        /// </summary>
        public void TrackTree(BehaviorTree.BehaviorTree tree)
        {
            if (tree != null && !_trackedTrees.Contains(tree))
            {
                _trackedTrees.Add(tree);
            }
        }

        /// <summary>
        /// 取消追踪行为树
        /// </summary>
        public void UntrackTree(BehaviorTree.BehaviorTree tree)
        {
            _trackedTrees.Remove(tree);
        }

        public void Update()
        {
            _nodeData.Clear();

            foreach (var tree in _trackedTrees)
            {
                if (tree == null || tree.Root == null) continue;

                CollectNodeData(tree.Root, 0);
            }
        }

        private void CollectNodeData(IBTNode node, int depth)
        {
            var data = new BTNodeDebugData
            {
                NodeName = node.Name ?? node.GetType().Name,
                NodeType = GetNodeTypeName(node),
                State = node.CurrentState,
                Depth = depth,
                IsActive = node.CurrentState == NodeState.Running,
                LastExecutionTime = Time.unscaledTime
            };

            _nodeData.Add(data);
            OnNodeExecuted?.Invoke(data);

            // 记录执行历史
            if (data.IsActive)
            {
                AddHistory($"[{Time.unscaledTime:F2}] {data.NodeName} → Running");
            }

            // 递归收集子节点
            if (node is BTComposite composite)
            {
                foreach (var child in composite.Children)
                {
                    CollectNodeData(child, depth + 1);
                }
            }
            else if (node is BTDecorator decorator && decorator.Child != null)
            {
                CollectNodeData(decorator.Child, depth + 1);
            }
        }

        private string GetNodeTypeName(IBTNode node)
        {
            if (node is BTComposite) return "Composite";
            if (node is BTDecorator) return "Decorator";
            if (node is BTLeaf) return "Leaf";
            return "Node";
        }

        private void AddHistory(string entry)
        {
            if (_executionHistory.Count >= MAX_HISTORY)
            {
                _executionHistory.Dequeue();
            }
            _executionHistory.Enqueue(entry);
        }

        public string GetSummary()
        {
            int activeCount = 0;
            int runningCount = 0;

            foreach (var data in _nodeData)
            {
                if (data.IsActive) runningCount++;
                activeCount++;
            }

            return $"Trees: {_trackedTrees.Count}, Nodes: {activeCount}, Running: {runningCount}";
        }

        public IEnumerable<DebugEntry> GetEntries()
        {
            foreach (var data in _nodeData)
            {
                var type = data.State switch
                {
                    NodeState.Running => DebugEntryType.Warning,
                    NodeState.Success => DebugEntryType.Success,
                    NodeState.Failure => DebugEntryType.Error,
                    _ => DebugEntryType.Info
                };

                yield return new DebugEntry(data.NodeName, data.ToString(), type);
            }
        }

        /// <summary>
        /// 获取节点数据列表
        /// </summary>
        public IReadOnlyList<BTNodeDebugData> GetNodeData() => _nodeData;

        /// <summary>
        /// 获取执行历史
        /// </summary>
        public IEnumerable<string> GetHistory() => _executionHistory;

        public void Clear()
        {
            _nodeData.Clear();
            _executionHistory.Clear();
        }
    }
}
