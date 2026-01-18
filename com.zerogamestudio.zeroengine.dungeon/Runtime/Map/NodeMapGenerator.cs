using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Dungeon.Map
{
    /// <summary>
    /// 节点地图生成器
    /// </summary>
    public class NodeMapGenerator
    {
        /// <summary>
        /// 生成配置
        /// </summary>
        public NodeMapGeneratorConfig Config { get; }

        private System.Random _random;

        public NodeMapGenerator(NodeMapGeneratorConfig config = null)
        {
            Config = config ?? new NodeMapGeneratorConfig();
        }

        /// <summary>
        /// 生成节点地图
        /// </summary>
        public NodeMap Generate(string mapId, int floor, int? seed = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

            var map = new NodeMap(mapId, floor);

            // 创建起始节点
            var startNode = new MapNode($"{mapId}_start", NodeType.Start)
            {
                Position = new Vector2(0, Config.PathCount / 2f),
                Depth = 0
            };
            map.AddNode(startNode);
            map.SetStartNode(startNode);

            // 生成中间层节点
            var previousLayer = new List<MapNode> { startNode };
            for (int depth = 1; depth < Config.MapDepth; depth++)
            {
                var currentLayer = GenerateLayer(map, mapId, depth, floor);

                // 连接上一层到当前层
                ConnectLayers(previousLayer, currentLayer);

                previousLayer = currentLayer;
            }

            // 创建BOSS节点
            var bossNode = new MapNode($"{mapId}_boss", NodeType.Boss)
            {
                Position = new Vector2(Config.MapDepth, Config.PathCount / 2f),
                Depth = Config.MapDepth
            };
            map.AddNode(bossNode);
            map.SetBossNode(bossNode);

            // 连接最后一层到BOSS
            foreach (var node in previousLayer)
            {
                map.ConnectNodes(node, bossNode);
            }

            return map;
        }

        /// <summary>
        /// 生成一层节点
        /// </summary>
        private List<MapNode> GenerateLayer(NodeMap map, string mapId, int depth, int floor)
        {
            var nodes = new List<MapNode>();
            int nodeCount = _random.Next(Config.MinNodesPerLayer, Config.MaxNodesPerLayer + 1);

            for (int i = 0; i < nodeCount; i++)
            {
                var type = GenerateNodeType(depth, floor);
                var node = new MapNode($"{mapId}_{depth}_{i}", type)
                {
                    Position = new Vector2(depth, i),
                    Depth = depth
                };
                map.AddNode(node);
                nodes.Add(node);
            }

            return nodes;
        }

        /// <summary>
        /// 生成节点类型
        /// </summary>
        private NodeType GenerateNodeType(int depth, int floor)
        {
            float roll = (float)_random.NextDouble();
            float cumulative = 0f;

            // 精英节点出现在中后期
            if (depth >= Config.MapDepth / 2)
            {
                cumulative += Config.EliteChance;
                if (roll < cumulative) return NodeType.Elite;
            }

            // 玩家队伍（异步PvP）
            float pvpChance = Config.PlayerTeamBaseChance + floor * Config.PlayerTeamFloorBonus;
            cumulative += pvpChance;
            if (roll < cumulative) return NodeType.PlayerTeam;

            // 商店
            cumulative += Config.ShopChance;
            if (roll < cumulative) return NodeType.Shop;

            // 休息点
            cumulative += Config.RestChance;
            if (roll < cumulative) return NodeType.Rest;

            // 随机事件
            cumulative += Config.EventChance;
            if (roll < cumulative) return NodeType.Event;

            // 宝箱
            cumulative += Config.TreasureChance;
            if (roll < cumulative) return NodeType.Treasure;

            // 默认普通战斗
            return NodeType.Battle;
        }

        /// <summary>
        /// 连接两层节点
        /// </summary>
        private void ConnectLayers(List<MapNode> fromLayer, List<MapNode> toLayer)
        {
            // 确保每个节点至少有一个连接
            foreach (var fromNode in fromLayer)
            {
                // 随机选择 1-2 个目标节点连接
                int connectionCount = _random.Next(1, Math.Min(3, toLayer.Count + 1));
                var selectedIndices = new HashSet<int>();

                while (selectedIndices.Count < connectionCount)
                {
                    int index = _random.Next(toLayer.Count);
                    if (selectedIndices.Add(index))
                    {
                        fromNode.AddConnection(toLayer[index]);
                    }
                }
            }

            // 确保每个目标节点至少被一个节点连接
            foreach (var toNode in toLayer)
            {
                bool hasConnection = false;
                foreach (var fromNode in fromLayer)
                {
                    if (fromNode.IsConnectedTo(toNode))
                    {
                        hasConnection = true;
                        break;
                    }
                }

                if (!hasConnection)
                {
                    // 随机选择一个源节点连接
                    var fromNode = fromLayer[_random.Next(fromLayer.Count)];
                    fromNode.AddConnection(toNode);
                }
            }
        }
    }

    /// <summary>
    /// 地图生成配置
    /// </summary>
    [Serializable]
    public class NodeMapGeneratorConfig
    {
        /// <summary>
        /// 地图深度（层数）
        /// </summary>
        public int MapDepth { get; set; } = 8;

        /// <summary>
        /// 路径数量
        /// </summary>
        public int PathCount { get; set; } = 3;

        /// <summary>
        /// 每层最少节点数
        /// </summary>
        public int MinNodesPerLayer { get; set; } = 2;

        /// <summary>
        /// 每层最多节点数
        /// </summary>
        public int MaxNodesPerLayer { get; set; } = 4;

        /// <summary>
        /// 精英节点概率
        /// </summary>
        public float EliteChance { get; set; } = 0.15f;

        /// <summary>
        /// 商店概率
        /// </summary>
        public float ShopChance { get; set; } = 0.1f;

        /// <summary>
        /// 休息点概率
        /// </summary>
        public float RestChance { get; set; } = 0.1f;

        /// <summary>
        /// 事件概率
        /// </summary>
        public float EventChance { get; set; } = 0.15f;

        /// <summary>
        /// 宝箱概率
        /// </summary>
        public float TreasureChance { get; set; } = 0.05f;

        /// <summary>
        /// 玩家队伍基础概率
        /// </summary>
        public float PlayerTeamBaseChance { get; set; } = 0.1f;

        /// <summary>
        /// 玩家队伍每层增加的概率
        /// </summary>
        public float PlayerTeamFloorBonus { get; set; } = 0.02f;
    }
}
