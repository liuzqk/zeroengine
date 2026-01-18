using System;
using System.Collections.Generic;
using ZeroEngine.Dungeon.Map;

namespace ZeroEngine.Dungeon.Exploration
{
    /// <summary>
    /// 探索AI，自动选择路径
    /// </summary>
    public class ExplorationAI
    {
        /// <summary>
        /// AI策略
        /// </summary>
        public ExplorationStrategy Strategy { get; set; } = ExplorationStrategy.Balanced;

        /// <summary>
        /// 节点类型偏好权重
        /// </summary>
        private readonly Dictionary<NodeType, float> _typeWeights = new();

        public ExplorationAI()
        {
            SetStrategy(ExplorationStrategy.Balanced);
        }

        /// <summary>
        /// 设置探索策略
        /// </summary>
        public void SetStrategy(ExplorationStrategy strategy)
        {
            Strategy = strategy;
            _typeWeights.Clear();

            switch (strategy)
            {
                case ExplorationStrategy.Aggressive:
                    // 激进型：优先战斗
                    _typeWeights[NodeType.Battle] = 1.5f;
                    _typeWeights[NodeType.Elite] = 2.0f;
                    _typeWeights[NodeType.PlayerTeam] = 1.8f;
                    _typeWeights[NodeType.Shop] = 0.5f;
                    _typeWeights[NodeType.Rest] = 0.3f;
                    _typeWeights[NodeType.Event] = 0.8f;
                    _typeWeights[NodeType.Treasure] = 1.0f;
                    break;

                case ExplorationStrategy.Conservative:
                    // 稳健型：优先休息和商店
                    _typeWeights[NodeType.Battle] = 0.8f;
                    _typeWeights[NodeType.Elite] = 0.4f;
                    _typeWeights[NodeType.PlayerTeam] = 0.5f;
                    _typeWeights[NodeType.Shop] = 1.5f;
                    _typeWeights[NodeType.Rest] = 2.0f;
                    _typeWeights[NodeType.Event] = 0.6f;
                    _typeWeights[NodeType.Treasure] = 1.0f;
                    break;

                case ExplorationStrategy.Explorer:
                    // 探索型：优先事件和宝箱
                    _typeWeights[NodeType.Battle] = 0.6f;
                    _typeWeights[NodeType.Elite] = 0.8f;
                    _typeWeights[NodeType.PlayerTeam] = 0.7f;
                    _typeWeights[NodeType.Shop] = 1.0f;
                    _typeWeights[NodeType.Rest] = 0.8f;
                    _typeWeights[NodeType.Event] = 2.0f;
                    _typeWeights[NodeType.Treasure] = 2.0f;
                    break;

                case ExplorationStrategy.Balanced:
                default:
                    // 平衡型
                    _typeWeights[NodeType.Battle] = 1.0f;
                    _typeWeights[NodeType.Elite] = 1.0f;
                    _typeWeights[NodeType.PlayerTeam] = 1.0f;
                    _typeWeights[NodeType.Shop] = 1.0f;
                    _typeWeights[NodeType.Rest] = 1.0f;
                    _typeWeights[NodeType.Event] = 1.0f;
                    _typeWeights[NodeType.Treasure] = 1.2f;
                    break;
            }
        }

        /// <summary>
        /// 选择下一个节点
        /// </summary>
        public MapNode SelectNextNode(NodeMap map, float currentHealthPercent = 1f)
        {
            var availableNodes = new List<MapNode>();
            foreach (var node in map.GetAvailableNodes())
            {
                availableNodes.Add(node);
            }

            if (availableNodes.Count == 0)
                return null;

            if (availableNodes.Count == 1)
                return availableNodes[0];

            // 计算每个节点的分数
            MapNode bestNode = null;
            float bestScore = float.MinValue;

            foreach (var node in availableNodes)
            {
                float score = CalculateNodeScore(node, currentHealthPercent);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestNode = node;
                }
            }

            return bestNode;
        }

        /// <summary>
        /// 计算节点分数
        /// </summary>
        private float CalculateNodeScore(MapNode node, float healthPercent)
        {
            float baseWeight = _typeWeights.TryGetValue(node.Type, out float w) ? w : 1f;

            // 血量低时增加休息点权重
            if (healthPercent < 0.5f && node.Type == NodeType.Rest)
            {
                baseWeight *= 2f;
            }

            // 血量低时降低战斗节点权重
            if (healthPercent < 0.3f)
            {
                switch (node.Type)
                {
                    case NodeType.Battle:
                    case NodeType.Elite:
                    case NodeType.PlayerTeam:
                        baseWeight *= 0.5f;
                        break;
                }
            }

            // 添加一点随机性
            float randomFactor = 0.8f + (float)new System.Random().NextDouble() * 0.4f;

            return baseWeight * randomFactor;
        }
    }

    /// <summary>
    /// 探索策略
    /// </summary>
    public enum ExplorationStrategy
    {
        /// <summary>
        /// 平衡型
        /// </summary>
        Balanced,

        /// <summary>
        /// 激进型 - 优先战斗
        /// </summary>
        Aggressive,

        /// <summary>
        /// 稳健型 - 优先休息/商店
        /// </summary>
        Conservative,

        /// <summary>
        /// 探索型 - 优先事件
        /// </summary>
        Explorer
    }
}
