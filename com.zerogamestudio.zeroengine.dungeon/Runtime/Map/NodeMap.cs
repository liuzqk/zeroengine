using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Dungeon.Map
{
    /// <summary>
    /// 节点地图，管理地下城的节点结构
    /// </summary>
    public class NodeMap
    {
        /// <summary>
        /// 地图ID
        /// </summary>
        public string MapId { get; }

        /// <summary>
        /// 地图层级（第几层地下城）
        /// </summary>
        public int Floor { get; }

        /// <summary>
        /// 所有节点
        /// </summary>
        private readonly Dictionary<string, MapNode> _nodes = new();

        /// <summary>
        /// 起始节点
        /// </summary>
        public MapNode StartNode { get; private set; }

        /// <summary>
        /// BOSS节点
        /// </summary>
        public MapNode BossNode { get; private set; }

        /// <summary>
        /// 当前节点
        /// </summary>
        public MapNode CurrentNode { get; private set; }

        /// <summary>
        /// 节点进入事件
        /// </summary>
        public event Action<MapNode> OnNodeEntered;

        /// <summary>
        /// 节点完成事件
        /// </summary>
        public event Action<MapNode> OnNodeCompleted;

        public NodeMap(string mapId, int floor)
        {
            MapId = mapId;
            Floor = floor;
        }

        /// <summary>
        /// 添加节点
        /// </summary>
        public void AddNode(MapNode node)
        {
            if (_nodes.ContainsKey(node.NodeId))
            {
                Debug.LogWarning($"[NodeMap] 节点 {node.NodeId} 已存在");
                return;
            }
            _nodes[node.NodeId] = node;
        }

        /// <summary>
        /// 设置起始节点
        /// </summary>
        public void SetStartNode(MapNode node)
        {
            StartNode = node;
            CurrentNode = node;
        }

        /// <summary>
        /// 设置BOSS节点
        /// </summary>
        public void SetBossNode(MapNode node)
        {
            BossNode = node;
        }

        /// <summary>
        /// 获取节点
        /// </summary>
        public MapNode GetNode(string nodeId)
        {
            return _nodes.TryGetValue(nodeId, out var node) ? node : null;
        }

        /// <summary>
        /// 获取所有节点
        /// </summary>
        public IEnumerable<MapNode> GetAllNodes()
        {
            return _nodes.Values;
        }

        /// <summary>
        /// 连接两个节点
        /// </summary>
        public void ConnectNodes(MapNode from, MapNode to)
        {
            from.AddConnection(to);
        }

        /// <summary>
        /// 移动到指定节点
        /// </summary>
        public bool MoveToNode(MapNode targetNode)
        {
            if (CurrentNode == null)
            {
                Debug.LogWarning("[NodeMap] 当前节点为空");
                return false;
            }

            if (!CurrentNode.IsConnectedTo(targetNode))
            {
                Debug.LogWarning($"[NodeMap] 无法从 {CurrentNode.NodeId} 移动到 {targetNode.NodeId}");
                return false;
            }

            if (!CurrentNode.IsCompleted)
            {
                Debug.LogWarning("[NodeMap] 当前节点未完成");
                return false;
            }

            CurrentNode = targetNode;
            targetNode.MarkVisited();
            OnNodeEntered?.Invoke(targetNode);
            return true;
        }

        /// <summary>
        /// 完成当前节点
        /// </summary>
        public void CompleteCurrentNode()
        {
            if (CurrentNode == null) return;
            CurrentNode.MarkCompleted();
            OnNodeCompleted?.Invoke(CurrentNode);
        }

        /// <summary>
        /// 获取当前可移动的节点
        /// </summary>
        public IEnumerable<MapNode> GetAvailableNodes()
        {
            if (CurrentNode == null || !CurrentNode.IsCompleted)
                yield break;

            foreach (var node in CurrentNode.ConnectedNodes)
            {
                yield return node;
            }
        }

        /// <summary>
        /// 检查是否到达BOSS节点
        /// </summary>
        public bool IsAtBoss()
        {
            return CurrentNode == BossNode;
        }

        /// <summary>
        /// 检查地图是否完成
        /// </summary>
        public bool IsCompleted()
        {
            return BossNode != null && BossNode.IsCompleted;
        }
    }
}
