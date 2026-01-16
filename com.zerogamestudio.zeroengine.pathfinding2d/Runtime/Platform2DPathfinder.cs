// Platform2DPathfinder.cs
// 2D 平台寻路器
// 使用生成的平台图进行路径查找，输出 MoveCommand 序列

using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Pathfinding2D
{
    /// <summary>
    /// 寻路器配置
    /// </summary>
    [System.Serializable]
    public class PathfinderConfig
    {
        [Header("路径请求")]
        [Tooltip("路径请求间隔（秒）")]
        public float PathRequestInterval = 0.3f;

        [Tooltip("路径过期时间（秒）")]
        public float PathExpireTime = 2f;

        [Tooltip("到达目标的判定距离")]
        public float ArriveDistance = 0.5f;

        [Header("节点查找")]
        [Tooltip("查找起点/终点节点的最大距离")]
        public float MaxNodeSearchRadius = 8f;

        [Tooltip("行走速度（用于时间估算）")]
        public float WalkSpeed = 5f;
    }

    /// <summary>
    /// 2D 平台寻路器
    /// 基于平台图进行 A* 寻路，生成 MoveCommand 序列
    /// </summary>
    public class Platform2DPathfinder : MonoBehaviour
    {
        [SerializeField]
        private PathfinderConfig config = new PathfinderConfig();

        [SerializeField]
        private PlatformGraphGenerator graphGenerator;

        /// <summary>配置</summary>
        public PathfinderConfig Config => config;

        /// <summary>当前路径</summary>
        public Platform2DPath CurrentPath { get; private set; }

        /// <summary>是否有有效路径</summary>
        public bool HasValidPath => CurrentPath != null &&
                                    CurrentPath.Status == PathStatus.Valid &&
                                    !CurrentPath.IsComplete;

        private float lastPathRequestTime;

        private void Awake()
        {
            if (graphGenerator == null)
            {
                graphGenerator = FindObjectOfType<PlatformGraphGenerator>();
            }
        }

        /// <summary>
        /// 请求新路径
        /// </summary>
        /// <param name="start">起点位置</param>
        /// <param name="end">终点位置</param>
        /// <param name="forceRequest">强制请求（忽略间隔限制）</param>
        /// <returns>是否成功发起请求</returns>
        public bool RequestPath(Vector3 start, Vector3 end, bool forceRequest = false)
        {
            // 检查请求间隔
            if (!forceRequest && Time.time - lastPathRequestTime < config.PathRequestInterval)
            {
                return false;
            }

            lastPathRequestTime = Time.time;

            // 检查图是否已生成
            if (graphGenerator == null || !graphGenerator.IsGenerated)
            {
                CurrentPath = Platform2DPath.NotFound(start, end);
                return false;
            }

            // 检查是否已经到达目标
            if (Vector2.Distance(start, end) <= config.ArriveDistance)
            {
                CurrentPath = new Platform2DPath(start, end, new List<MoveCommand>());
                return true;
            }

            // 查找起点和终点节点
            var startNode = graphGenerator.FindNearestNode(start, config.MaxNodeSearchRadius);
            var endNode = graphGenerator.FindNearestNode(end, config.MaxNodeSearchRadius);

            if (!startNode.HasValue || !endNode.HasValue)
            {
                CurrentPath = Platform2DPath.NotFound(start, end);
                return false;
            }

            // 执行 A* 寻路
            var path = FindPath(startNode.Value, endNode.Value, start, end);
            CurrentPath = path;

            return path.Status == PathStatus.Valid;
        }

        /// <summary>
        /// 获取当前移动指令
        /// </summary>
        public MoveCommand? GetCurrentCommand()
        {
            if (CurrentPath == null || CurrentPath.Status != PathStatus.Valid)
            {
                return null;
            }

            return CurrentPath.GetCurrentCommand();
        }

        /// <summary>
        /// 完成当前指令，前进到下一个
        /// </summary>
        public bool AdvanceToNextCommand()
        {
            if (CurrentPath == null) return false;
            return CurrentPath.AdvanceToNext();
        }

        /// <summary>
        /// 检查当前指令是否完成
        /// </summary>
        /// <param name="currentPosition">当前位置</param>
        /// <param name="isGrounded">是否在地面</param>
        /// <returns>是否完成</returns>
        public bool IsCurrentCommandComplete(Vector3 currentPosition, bool isGrounded)
        {
            var cmd = GetCurrentCommand();
            if (!cmd.HasValue) return true;

            var command = cmd.Value;

            switch (command.CommandType)
            {
                case MoveCommandType.Walk:
                    // 行走：到达目标 X 坐标附近
                    return Mathf.Abs(currentPosition.x - command.Target.x) < config.ArriveDistance;

                case MoveCommandType.Jump:
                case MoveCommandType.Fall:
                case MoveCommandType.DropDown:
                    // 跳跃/下落：落地且接近目标位置
                    if (!isGrounded) return false;
                    return Vector2.Distance(currentPosition, command.Target) < config.ArriveDistance * 2f;

                default:
                    return true;
            }
        }

        /// <summary>
        /// 清除当前路径
        /// </summary>
        public void ClearPath()
        {
            CurrentPath = null;
        }

        /// <summary>
        /// A* 寻路实现
        /// </summary>
        private Platform2DPath FindPath(PlatformNodeData startNode, PlatformNodeData endNode, Vector3 actualStart, Vector3 actualEnd)
        {
            var nodes = graphGenerator.Nodes;
            var nodeCount = nodes.Count;

            // 初始化寻路数据
            var gScore = new Dictionary<int, float>();
            var fScore = new Dictionary<int, float>();
            var cameFrom = new Dictionary<int, int>();
            var cameFromLink = new Dictionary<int, PlatformLinkData>();

            var openSet = new List<int>();
            var closedSet = new HashSet<int>();

            // 初始化起点
            gScore[startNode.NodeId] = 0;
            fScore[startNode.NodeId] = Heuristic(startNode.Position, endNode.Position);
            openSet.Add(startNode.NodeId);

            while (openSet.Count > 0)
            {
                // 找到 fScore 最小的节点
                int current = GetLowestFScore(openSet, fScore);

                // 到达终点
                if (current == endNode.NodeId)
                {
                    return ReconstructPath(cameFrom, cameFromLink, current, actualStart, actualEnd);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                // 遍历所有出边
                var outgoingLinks = graphGenerator.GetOutgoingLinks(current);

                foreach (var link in outgoingLinks)
                {
                    if (closedSet.Contains(link.ToNodeId)) continue;

                    float tentativeG = gScore[current] + link.Cost;

                    if (!gScore.ContainsKey(link.ToNodeId) || tentativeG < gScore[link.ToNodeId])
                    {
                        cameFrom[link.ToNodeId] = current;
                        cameFromLink[link.ToNodeId] = link;
                        gScore[link.ToNodeId] = tentativeG;

                        var toNode = graphGenerator.GetNode(link.ToNodeId);
                        if (toNode.HasValue)
                        {
                            fScore[link.ToNodeId] = tentativeG + Heuristic(toNode.Value.Position, endNode.Position);
                        }

                        if (!openSet.Contains(link.ToNodeId))
                        {
                            openSet.Add(link.ToNodeId);
                        }
                    }
                }
            }

            // 找不到路径
            return Platform2DPath.NotFound(actualStart, actualEnd);
        }

        /// <summary>
        /// 启发式函数（曼哈顿距离 + 高度惩罚）
        /// </summary>
        private float Heuristic(Vector3 a, Vector3 b)
        {
            float dx = Mathf.Abs(a.x - b.x);
            float dy = Mathf.Abs(a.y - b.y);

            // 向上移动代价更高
            if (b.y > a.y)
            {
                dy *= 1.5f;
            }

            return dx + dy;
        }

        /// <summary>
        /// 获取 fScore 最小的节点
        /// </summary>
        private int GetLowestFScore(List<int> openSet, Dictionary<int, float> fScore)
        {
            int best = openSet[0];
            float bestScore = fScore.ContainsKey(best) ? fScore[best] : float.MaxValue;

            for (int i = 1; i < openSet.Count; i++)
            {
                int node = openSet[i];
                float score = fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = node;
                }
            }

            return best;
        }

        /// <summary>
        /// 重建路径并生成 MoveCommand
        /// </summary>
        private Platform2DPath ReconstructPath(
            Dictionary<int, int> cameFrom,
            Dictionary<int, PlatformLinkData> cameFromLink,
            int current,
            Vector3 actualStart,
            Vector3 actualEnd)
        {
            var nodePath = new List<int>();
            var linkPath = new List<PlatformLinkData>();

            // 回溯路径
            while (cameFrom.ContainsKey(current))
            {
                nodePath.Insert(0, current);
                if (cameFromLink.ContainsKey(current))
                {
                    linkPath.Insert(0, cameFromLink[current]);
                }
                current = cameFrom[current];
            }
            nodePath.Insert(0, current); // 添加起点

            // 转换为 MoveCommand
            var commands = new List<MoveCommand>();

            // 从实际起点走到第一个节点
            if (nodePath.Count > 0)
            {
                var firstNode = graphGenerator.GetNode(nodePath[0]);
                if (firstNode.HasValue)
                {
                    float dist = Vector2.Distance(actualStart, firstNode.Value.Position);
                    if (dist > config.ArriveDistance)
                    {
                        commands.Add(MoveCommand.Walk(
                            firstNode.Value.Position,
                            dist / config.WalkSpeed
                        ));
                    }
                }
            }

            // 处理路径中的每个链接
            foreach (var link in linkPath)
            {
                var toNode = graphGenerator.GetNode(link.ToNodeId);
                if (!toNode.HasValue) continue;

                switch (link.LinkType)
                {
                    case PlatformLinkType.Walk:
                        commands.Add(MoveCommand.Walk(toNode.Value.Position, link.Duration));
                        break;

                    case PlatformLinkType.Jump:
                        commands.Add(MoveCommand.Jump(
                            toNode.Value.Position,
                            link.JumpVelocityY,
                            link.JumpVelocityX,
                            link.Duration
                        ));
                        break;

                    case PlatformLinkType.Fall:
                        commands.Add(MoveCommand.Fall(toNode.Value.Position, link.Duration));
                        break;

                    case PlatformLinkType.DropThrough:
                        commands.Add(MoveCommand.DropDown(
                            toNode.Value.Position,
                            toNode.Value.PlatformCollider,
                            link.Duration
                        ));
                        break;
                }
            }

            // 从最后一个节点走到实际终点
            if (nodePath.Count > 0)
            {
                var lastNode = graphGenerator.GetNode(nodePath[nodePath.Count - 1]);
                if (lastNode.HasValue)
                {
                    float dist = Vector2.Distance(lastNode.Value.Position, actualEnd);
                    if (dist > config.ArriveDistance)
                    {
                        commands.Add(MoveCommand.Walk(
                            actualEnd,
                            dist / config.WalkSpeed
                        ));
                    }
                }
            }

            return new Platform2DPath(actualStart, actualEnd, commands);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (CurrentPath == null || CurrentPath.Status != PathStatus.Valid) return;

            // 绘制当前路径
            Gizmos.color = Color.magenta;

            for (int i = CurrentPath.CurrentIndex; i < CurrentPath.Commands.Count; i++)
            {
                var cmd = CurrentPath.Commands[i];

                // 绘制目标点
                switch (cmd.CommandType)
                {
                    case MoveCommandType.Walk:
                        Gizmos.color = Color.green;
                        break;
                    case MoveCommandType.Jump:
                        Gizmos.color = Color.yellow;
                        break;
                    case MoveCommandType.Fall:
                    case MoveCommandType.DropDown:
                        Gizmos.color = Color.blue;
                        break;
                }

                Gizmos.DrawWireSphere(cmd.Target, 0.3f);

                // 绘制连线
                if (i > CurrentPath.CurrentIndex)
                {
                    var prevCmd = CurrentPath.Commands[i - 1];
                    Gizmos.DrawLine(prevCmd.Target, cmd.Target);
                }
            }
        }
#endif
    }
}
