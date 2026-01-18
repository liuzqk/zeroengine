// JumpLinkCalculator.cs
// 跳跃链接计算器
// 遍历平台边缘节点，计算可达的跳跃链接

using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Pathfinding2D
{
    /// <summary>
    /// 跳跃链接生成配置
    /// </summary>
    [System.Serializable]
    public class JumpLinkConfig
    {
        [Header("跳跃能力")]
        [Tooltip("最大跳跃初速度 (Y)")]
        public float MaxJumpVelocity = 14f;

        [Tooltip("最大水平跳跃距离")]
        public float MaxHorizontalDistance = 6f;

        [Tooltip("最大跳跃高度")]
        public float MaxJumpHeight = 6f;

        [Tooltip("重力缩放 (Rigidbody2D.gravityScale)")]
        public float GravityScale = 3f;

        [Header("下落配置")]
        [Tooltip("最大下落高度")]
        public float MaxFallHeight = 10f;

        [Tooltip("最大下落水平距离")]
        public float MaxFallHorizontalDistance = 4f;

        [Tooltip("表面节点垂直下落最大水平距离（小于此值才从表面节点生成下落链接）")]
        public float SurfaceNodeVerticalFallMaxHorizontal = 1.5f;

        [Header("验证参数")]
        [Tooltip("轨迹碰撞检测半径")]
        public float TrajectoryCheckRadius = 0.4f;

        [Tooltip("过冲系数 (1.0 = 刚好到达)")]
        public float Overshoot = 1.2f;

        [Tooltip("最小链接距离 (小于此距离不创建链接)")]
        public float MinLinkDistance = 0.5f;
    }

    /// <summary>
    /// 跳跃链接计算器
    /// 分析平台图中的边缘节点，生成跳跃和下落链接
    /// </summary>
    public class JumpLinkCalculator : MonoBehaviour
    {
        [SerializeField]
        private JumpLinkConfig config = new JumpLinkConfig();

        [SerializeField]
        private PlatformGraphGenerator graphGenerator;

        /// <summary>配置</summary>
        public JumpLinkConfig Config => config;

        /// <summary>
        /// 生成所有跳跃链接
        /// </summary>
        public void GenerateJumpLinks()
        {
            if (graphGenerator == null)
            {
                graphGenerator = GetComponent<PlatformGraphGenerator>();
            }

            if (graphGenerator == null || !graphGenerator.IsGenerated)
            {
                Debug.LogWarning("[JumpLinkCalculator] PlatformGraphGenerator 未找到或未生成");
                return;
            }

            var nodes = graphGenerator.Nodes;
            var obstacleLayer = graphGenerator.Config.ObstacleLayer;

            int jumpLinksCreated = 0;
            int fallLinksCreated = 0;
            int dropLinksCreated = 0;

            // 诊断计数器
            int jumpAttempts = 0;
            int jumpFailedDistance = 0;
            int jumpFailedHeight = 0;
            int jumpFailedReachable = 0;
            int jumpFailedTrajectory = 0;

            // 遍历所有节点（不仅限于边缘节点，让任意位置都能发起跳跃）
            for (int i = 0; i < nodes.Count; i++)
            {
                var fromNode = nodes[i];

                // 判断节点类型
                bool isEdgeNode = fromNode.NodeType == PlatformNodeType.LeftEdge ||
                                  fromNode.NodeType == PlatformNodeType.RightEdge;
                bool isSurfaceNode = fromNode.NodeType == PlatformNodeType.Surface;

                // 计算跳跃到其他平台的链接
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (i == j) continue;

                    var toNode = nodes[j];

                    // 跳过同一平台的节点
                    if (fromNode.PlatformCollider == toNode.PlatformCollider) continue;

                    // 检查距离限制
                    float horizontalDist = Mathf.Abs(toNode.Position.x - fromNode.Position.x);
                    float verticalDist = toNode.Position.y - fromNode.Position.y;

                    if (horizontalDist < config.MinLinkDistance) continue;

                    // 目标在上方或同高度 - 尝试跳跃
                    if (verticalDist >= -0.5f && verticalDist <= config.MaxJumpHeight)
                    {
                        if (horizontalDist <= config.MaxHorizontalDistance)
                        {
                            jumpAttempts++;
                            if (TryCreateJumpLink(fromNode, toNode, obstacleLayer, out string failReason))
                            {
                                jumpLinksCreated++;
                            }
                            else
                            {
                                if (failReason == "unreachable") jumpFailedReachable++;
                                else if (failReason == "trajectory") jumpFailedTrajectory++;
                            }
                        }
                        else
                        {
                            jumpFailedDistance++;
                        }
                    }
                    else if (verticalDist > config.MaxJumpHeight)
                    {
                        jumpFailedHeight++;
                    }
                    // 目标在下方 - 尝试下落
                    else if (verticalDist < -0.5f && Mathf.Abs(verticalDist) <= config.MaxFallHeight)
                    {
                        // 边缘节点：完整下落检测（水平 + 垂直）
                        if (isEdgeNode && horizontalDist <= config.MaxFallHorizontalDistance)
                        {
                            if (TryCreateFallLink(fromNode, toNode, obstacleLayer))
                            {
                                fallLinksCreated++;
                            }
                        }
                        // 表面节点：仅限垂直下落（水平距离很小）
                        else if (isSurfaceNode && horizontalDist <= config.SurfaceNodeVerticalFallMaxHorizontal)
                        {
                            if (TryCreateFallLink(fromNode, toNode, obstacleLayer))
                            {
                                fallLinksCreated++;
                            }
                        }
                    }
                }

                // 检查穿透单向平台下落（边缘节点或单向平台节点）
                if (isEdgeNode && fromNode.IsOneWay)
                {
                    var dropLinks = CreateDropThroughLinks(fromNode, nodes, obstacleLayer);
                    dropLinksCreated += dropLinks;
                }
            }

            Debug.Log($"[JumpLinkCalculator] 链接生成完成: 跳跃 {jumpLinksCreated}, 下落 {fallLinksCreated}, 穿透 {dropLinksCreated}");
            Debug.Log($"[JumpLinkCalculator] 跳跃诊断: 尝试={jumpAttempts}, 成功={jumpLinksCreated}, " +
                      $"超距离={jumpFailedDistance}, 超高度={jumpFailedHeight}, 不可达={jumpFailedReachable}, 轨迹阻挡={jumpFailedTrajectory}");
            Debug.Log($"[JumpLinkCalculator] 配置: MaxJumpHeight={config.MaxJumpHeight}, MaxHorizontalDistance={config.MaxHorizontalDistance}, " +
                      $"MaxJumpVelocity={config.MaxJumpVelocity}, ObstacleLayer={obstacleLayer.value}");
        }

        /// <summary>
        /// 尝试创建跳跃链接
        /// </summary>
        private bool TryCreateJumpLink(PlatformNodeData from, PlatformNodeData to, LayerMask obstacleLayer, out string failReason)
        {
            failReason = null;

            var result = JumpMovementHandler.CalculateJump(
                from.Position,
                to.Position,
                config.MaxJumpVelocity,
                config.GravityScale,
                config.Overshoot
            );

            if (!result.IsReachable)
            {
                failReason = "unreachable";
                return false;
            }

            // 验证轨迹无障碍
            if (!JumpMovementHandler.ValidateTrajectory(result.Trajectory, obstacleLayer, config.TrajectoryCheckRadius))
            {
                failReason = "trajectory";
                return false;
            }

            // 创建跳跃链接
            var link = PlatformLinkData.CreateJump(
                from.NodeId,
                to.NodeId,
                result.VelocityY,
                result.VelocityX,
                result.FlightTime
            );

            graphGenerator.Links.Add(link);
            return true;
        }

        /// <summary>
        /// 尝试创建下落链接
        /// </summary>
        private bool TryCreateFallLink(PlatformNodeData from, PlatformNodeData to, LayerMask obstacleLayer)
        {
            var result = JumpMovementHandler.CalculateFall(
                from.Position,
                to.Position,
                config.GravityScale
            );

            if (!result.IsReachable) return false;

            // 简单的直线碰撞检测
            Vector2 direction = ((Vector2)to.Position - (Vector2)from.Position).normalized;
            float distance = Vector2.Distance(from.Position, to.Position);

            RaycastHit2D hit = Physics2D.CircleCast(
                from.Position,
                config.TrajectoryCheckRadius,
                direction,
                distance,
                obstacleLayer
            );

            if (hit.collider != null && hit.collider != to.PlatformCollider)
            {
                return false;
            }

            // 创建下落链接
            var link = PlatformLinkData.CreateFall(from.NodeId, to.NodeId, result.FlightTime);
            graphGenerator.Links.Add(link);
            return true;
        }

        /// <summary>
        /// 创建穿透单向平台的下落链接
        /// </summary>
        private int CreateDropThroughLinks(PlatformNodeData fromNode, List<PlatformNodeData> allNodes, LayerMask obstacleLayer)
        {
            int created = 0;

            // 向下检测可以穿透到达的平台
            Vector2 startPos = fromNode.Position;

            foreach (var toNode in allNodes)
            {
                // 跳过同一平台
                if (toNode.PlatformCollider == fromNode.PlatformCollider) continue;

                // 目标必须在正下方附近
                float horizontalDist = Mathf.Abs(toNode.Position.x - startPos.x);
                float verticalDist = startPos.y - toNode.Position.y;

                if (horizontalDist > 1f) continue;
                if (verticalDist <= 0.5f || verticalDist > config.MaxFallHeight) continue;

                // 计算下落时间
                var result = JumpMovementHandler.CalculateFall(startPos, toNode.Position, config.GravityScale);

                if (!result.IsReachable) continue;

                // 创建穿透下落链接
                var link = PlatformLinkData.CreateDropThrough(fromNode.NodeId, toNode.NodeId, result.FlightTime);
                graphGenerator.Links.Add(link);
                created++;
            }

            return created;
        }

        /// <summary>
        /// 清除所有跳跃链接（保留行走链接）
        /// </summary>
        public void ClearJumpLinks()
        {
            if (graphGenerator == null) return;

            graphGenerator.Links.RemoveAll(link =>
                link.LinkType == PlatformLinkType.Jump ||
                link.LinkType == PlatformLinkType.Fall ||
                link.LinkType == PlatformLinkType.DropThrough
            );
        }

        /// <summary>
        /// 重新生成跳跃链接
        /// </summary>
        public void RegenerateJumpLinks()
        {
            ClearJumpLinks();
            GenerateJumpLinks();
        }

#if UNITY_EDITOR
        [ContextMenu("生成跳跃链接")]
        private void EditorGenerateJumpLinks()
        {
            if (graphGenerator == null)
            {
                graphGenerator = GetComponent<PlatformGraphGenerator>();
            }

            if (graphGenerator != null && !graphGenerator.IsGenerated)
            {
                graphGenerator.GeneratePlatformGraph();
            }

            GenerateJumpLinks();
        }

        [ContextMenu("清除跳跃链接")]
        private void EditorClearJumpLinks()
        {
            ClearJumpLinks();
        }
#endif
    }
}
