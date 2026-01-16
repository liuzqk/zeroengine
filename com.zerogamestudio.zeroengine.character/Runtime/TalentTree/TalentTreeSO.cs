using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.TalentTree
{
    /// <summary>
    /// 天赋树节点连接
    /// </summary>
    [Serializable]
    public class TalentConnection
    {
        public string FromNodeId;
        public string ToNodeId;
    }

    /// <summary>
    /// 天赋树定义
    /// </summary>
    [CreateAssetMenu(fileName = "NewTalentTree", menuName = "ZeroEngine/TalentTree/Talent Tree")]
    public class TalentTreeSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("天赋树唯一 ID")]
        public string TreeId;

        [Tooltip("显示名称")]
        public string DisplayName;

        [Tooltip("描述")]
        [TextArea]
        public string Description;

        [Tooltip("图标")]
        public Sprite Icon;

        [Header("节点")]
        [Tooltip("天赋树包含的所有节点")]
        public List<TalentNodeSO> Nodes = new List<TalentNodeSO>();

        [Tooltip("起始节点")]
        public TalentNodeSO StartNode;

        [Header("连接关系（编辑器用）")]
        [Tooltip("节点连接关系")]
        public List<TalentConnection> Connections = new List<TalentConnection>();

        // 缓存：ID -> Node
        private Dictionary<string, TalentNodeSO> _nodeCache;

        /// <summary>
        /// 节点数量
        /// </summary>
        public int NodeCount => Nodes.Count;

        /// <summary>
        /// 获取节点（按 ID）
        /// </summary>
        public TalentNodeSO GetNode(string nodeId)
        {
            EnsureCache();
            return _nodeCache.TryGetValue(nodeId, out var node) ? node : null;
        }

        /// <summary>
        /// 获取所有指定类型的节点
        /// </summary>
        public IEnumerable<TalentNodeSO> GetNodesByType(TalentNodeType type)
        {
            foreach (var node in Nodes)
            {
                if (node != null && node.NodeType == type)
                {
                    yield return node;
                }
            }
        }

        /// <summary>
        /// 检查节点是否在此树中
        /// </summary>
        public bool ContainsNode(TalentNodeSO node)
        {
            return node != null && Nodes.Contains(node);
        }

        /// <summary>
        /// 获取节点的所有后继节点
        /// </summary>
        public IEnumerable<TalentNodeSO> GetSuccessors(TalentNodeSO node)
        {
            if (node == null) yield break;

            foreach (var conn in Connections)
            {
                if (conn.FromNodeId == node.NodeId)
                {
                    var successor = GetNode(conn.ToNodeId);
                    if (successor != null)
                    {
                        yield return successor;
                    }
                }
            }
        }

        /// <summary>
        /// 获取节点的所有前置节点
        /// </summary>
        public IEnumerable<TalentNodeSO> GetPredecessors(TalentNodeSO node)
        {
            if (node == null) yield break;

            foreach (var conn in Connections)
            {
                if (conn.ToNodeId == node.NodeId)
                {
                    var predecessor = GetNode(conn.FromNodeId);
                    if (predecessor != null)
                    {
                        yield return predecessor;
                    }
                }
            }
        }

        /// <summary>
        /// 计算天赋树总点数需求
        /// </summary>
        public int GetTotalPointsRequired()
        {
            int total = 0;
            foreach (var node in Nodes)
            {
                if (node != null)
                {
                    total += node.GetTotalCost(node.MaxLevel);
                }
            }
            return total;
        }

        /// <summary>
        /// 验证天赋树完整性
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrEmpty(TreeId))
            {
                errors.Add("TreeId 为空");
            }

            if (Nodes.Count == 0)
            {
                errors.Add("天赋树没有节点");
            }

            // 检查重复 ID
            var ids = new HashSet<string>();
            foreach (var node in Nodes)
            {
                if (node == null)
                {
                    errors.Add("存在空节点引用");
                    continue;
                }

                if (!ids.Add(node.NodeId))
                {
                    errors.Add($"重复的节点 ID: {node.NodeId}");
                }
            }

            // 检查连接有效性
            foreach (var conn in Connections)
            {
                if (!ids.Contains(conn.FromNodeId))
                {
                    errors.Add($"连接引用了不存在的节点: {conn.FromNodeId}");
                }
                if (!ids.Contains(conn.ToNodeId))
                {
                    errors.Add($"连接引用了不存在的节点: {conn.ToNodeId}");
                }
            }

            // 检查循环依赖
            if (HasCyclicDependency())
            {
                errors.Add("存在循环依赖");
            }

            return errors.Count == 0;
        }

        private bool HasCyclicDependency()
        {
            var visited = new HashSet<string>();
            var stack = new HashSet<string>();

            foreach (var node in Nodes)
            {
                if (node != null && HasCycleDFS(node.NodeId, visited, stack))
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasCycleDFS(string nodeId, HashSet<string> visited, HashSet<string> stack)
        {
            if (stack.Contains(nodeId)) return true;
            if (visited.Contains(nodeId)) return false;

            visited.Add(nodeId);
            stack.Add(nodeId);

            foreach (var conn in Connections)
            {
                if (conn.FromNodeId == nodeId)
                {
                    if (HasCycleDFS(conn.ToNodeId, visited, stack))
                    {
                        return true;
                    }
                }
            }

            stack.Remove(nodeId);
            return false;
        }

        private void EnsureCache()
        {
            if (_nodeCache == null)
            {
                _nodeCache = new Dictionary<string, TalentNodeSO>();
                foreach (var node in Nodes)
                {
                    if (node != null && !string.IsNullOrEmpty(node.NodeId))
                    {
                        _nodeCache[node.NodeId] = node;
                    }
                }
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(TreeId))
            {
                TreeId = name.ToLowerInvariant().Replace(" ", "_");
            }
            if (string.IsNullOrEmpty(DisplayName))
            {
                DisplayName = name;
            }

            // 清除缓存
            _nodeCache = null;
        }

#if UNITY_EDITOR
        [ContextMenu("验证天赋树")]
        private void ValidateTree()
        {
            if (Validate(out var errors))
            {
                Debug.Log($"[TalentTree] {DisplayName} 验证通过");
            }
            else
            {
                foreach (var error in errors)
                {
                    Debug.LogError($"[TalentTree] {DisplayName}: {error}");
                }
            }
        }
#endif
    }
}
