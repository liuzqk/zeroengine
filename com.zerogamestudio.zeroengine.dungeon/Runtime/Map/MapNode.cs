using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Dungeon.Map
{
    /// <summary>
    /// 地图节点
    /// </summary>
    public class MapNode
    {
        /// <summary>
        /// 节点唯一ID
        /// </summary>
        public string NodeId { get; }

        /// <summary>
        /// 节点类型
        /// </summary>
        public NodeType Type { get; }

        /// <summary>
        /// 节点在地图上的位置（用于显示）
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// 节点层级（从起点开始的深度）
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// 是否已访问
        /// </summary>
        public bool IsVisited { get; private set; }

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// 连接的后续节点
        /// </summary>
        public IReadOnlyList<MapNode> ConnectedNodes => _connectedNodes;
        private readonly List<MapNode> _connectedNodes = new();

        /// <summary>
        /// 节点附加数据
        /// </summary>
        public object Data { get; set; }

        public MapNode(string nodeId, NodeType type)
        {
            NodeId = nodeId;
            Type = type;
        }

        /// <summary>
        /// 添加连接节点
        /// </summary>
        public void AddConnection(MapNode node)
        {
            if (!_connectedNodes.Contains(node))
            {
                _connectedNodes.Add(node);
            }
        }

        /// <summary>
        /// 检查是否连接到指定节点
        /// </summary>
        public bool IsConnectedTo(MapNode node)
        {
            return _connectedNodes.Contains(node);
        }

        /// <summary>
        /// 标记为已访问
        /// </summary>
        public void MarkVisited()
        {
            IsVisited = true;
        }

        /// <summary>
        /// 标记为已完成
        /// </summary>
        public void MarkCompleted()
        {
            IsCompleted = true;
        }

        public override string ToString()
        {
            return $"Node({NodeId}, {Type}, Depth={Depth})";
        }
    }

    /// <summary>
    /// 节点类型
    /// </summary>
    public enum NodeType
    {
        /// <summary>
        /// 起始节点
        /// </summary>
        Start,

        /// <summary>
        /// 普通战斗
        /// </summary>
        Battle,

        /// <summary>
        /// 精英战斗
        /// </summary>
        Elite,

        /// <summary>
        /// BOSS战斗
        /// </summary>
        Boss,

        /// <summary>
        /// 商店
        /// </summary>
        Shop,

        /// <summary>
        /// 休息点
        /// </summary>
        Rest,

        /// <summary>
        /// 随机事件
        /// </summary>
        Event,

        /// <summary>
        /// 宝箱
        /// </summary>
        Treasure,

        /// <summary>
        /// 玩家队伍（异步PvP）
        /// </summary>
        PlayerTeam
    }
}
