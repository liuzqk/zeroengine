// PlatformGraphGenerator.cs
// 平台图生成器
// 扫描场景中的平台碰撞体，生成用于寻路的节点网络

using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Pathfinding2D
{
    /// <summary>
    /// 平台图生成配置
    /// </summary>
    [System.Serializable]
    public class PlatformGraphConfig
    {
        [Header("扫描范围")]
        [Tooltip("扫描区域中心")]
        public Vector2 ScanCenter = Vector2.zero;

        [Tooltip("扫描区域尺寸")]
        public Vector2 ScanSize = new Vector2(100f, 50f);

        [Header("节点生成")]
        [Tooltip("平台表面节点间距")]
        public float NodeSpacing = 1.5f;

        [Tooltip("边缘节点内缩距离")]
        public float EdgeInset = 0.3f;

        [Tooltip("最小平台宽度（小于此值的平台只生成中心节点）")]
        public float MinPlatformWidth = 1f;

        [Header("密集节点模式")]
        [Tooltip("启用密集节点生成（用于更精确的寻路）")]
        public bool UseDenseNodes = false;

        [Tooltip("密集模式节点间距")]
        public float DenseNodeSpacing = 0.75f;

        [Header("空间索引")]
        [Tooltip("空间网格单元尺寸（用于加速节点查询）")]
        public float SpatialGridCellSize = 3f;

        [Header("层级配置")]
        [Tooltip("地面层")]
        public LayerMask GroundLayer;

        [Tooltip("单向平台层")]
        public LayerMask OneWayPlatformLayer;

        [Tooltip("障碍物层（用于碰撞检测）")]
        public LayerMask ObstacleLayer;

        [Header("角色参数")]
        [Tooltip("角色碰撞体半径")]
        public float CharacterRadius = 0.4f;

        [Tooltip("角色高度")]
        public float CharacterHeight = 1.8f;

        /// <summary>
        /// 所有平台层的组合
        /// </summary>
        public LayerMask AllPlatformLayers => GroundLayer | OneWayPlatformLayer;

        /// <summary>
        /// 获取实际使用的节点间距
        /// </summary>
        public float ActualNodeSpacing => UseDenseNodes ? DenseNodeSpacing : NodeSpacing;
    }

    /// <summary>
    /// 平台图生成器
    /// 扫描场景中的 Collider2D 并生成平台节点网络
    /// </summary>
    public class PlatformGraphGenerator : MonoBehaviour
    {
        [SerializeField]
        private PlatformGraphConfig config = new PlatformGraphConfig();

        /// <summary>配置</summary>
        public PlatformGraphConfig Config => config;

        /// <summary>生成的节点列表</summary>
        public List<PlatformNodeData> Nodes { get; private set; } = new List<PlatformNodeData>();

        /// <summary>节点 ID 到索引的映射</summary>
        public Dictionary<int, int> NodeIdToIndex { get; private set; } = new Dictionary<int, int>();

        /// <summary>生成的链接列表</summary>
        public List<PlatformLinkData> Links { get; private set; } = new List<PlatformLinkData>();

        /// <summary>是否已生成</summary>
        public bool IsGenerated { get; private set; }

        /// <summary>上次生成时间</summary>
        public float LastGenerateTime { get; private set; }

        /// <summary>空间索引</summary>
        public SpatialGrid2D SpatialGrid { get; private set; }

        private int nextNodeId = 0;

        // 复用 List 避免 GC
        private readonly List<Vector2> _pathPointsCache = new List<Vector2>(64);
        private readonly List<PlatformNodeData> _nodesInRangeCache = new List<PlatformNodeData>(32);

        /// <summary>
        /// 生成平台图
        /// </summary>
        public void GeneratePlatformGraph()
        {
            ClearGraph();

            // 扫描区域内的所有平台碰撞体
            var colliders = ScanPlatformColliders();

            // 为每个平台生成节点
            foreach (var collider in colliders)
            {
                GenerateNodesForPlatform(collider);
            }

            // 生成同平台行走链接
            GenerateWalkLinks();

            // 构建空间索引
            SpatialGrid = new SpatialGrid2D(config.SpatialGridCellSize);
            SpatialGrid.Build(Nodes);

            IsGenerated = true;
            LastGenerateTime = Time.time;

            Debug.Log($"[PlatformGraphGenerator] 生成完成: {Nodes.Count} 节点, {Links.Count} 链接, 空间索引: {SpatialGrid.GetDebugInfo()}");
        }

        /// <summary>
        /// 清除现有图数据
        /// </summary>
        public void ClearGraph()
        {
            Nodes.Clear();
            NodeIdToIndex.Clear();
            Links.Clear();
            nextNodeId = 0;
            IsGenerated = false;
            SpatialGrid?.Clear();
            SpatialGrid = null;
        }

        /// <summary>
        /// 扫描区域内的平台碰撞体
        /// </summary>
        private List<Collider2D> ScanPlatformColliders()
        {
            var result = new List<Collider2D>();

            // 使用 OverlapBox 扫描
            var colliders = Physics2D.OverlapBoxAll(
                config.ScanCenter,
                config.ScanSize,
                0f,
                config.AllPlatformLayers
            );

            foreach (var col in colliders)
            {
                // 过滤无效碰撞体
                if (col == null || !col.enabled) continue;

                // 只处理 BoxCollider2D, EdgeCollider2D, CompositeCollider2D
                if (col is BoxCollider2D || col is EdgeCollider2D ||
                    col is CompositeCollider2D || col is PolygonCollider2D)
                {
                    result.Add(col);
                }
            }

            return result;
        }

        /// <summary>
        /// 为单个平台生成节点
        /// </summary>
        private void GenerateNodesForPlatform(Collider2D collider)
        {
            // 判断是否是单向平台
            bool isOneWay = ((1 << collider.gameObject.layer) & config.OneWayPlatformLayer) != 0;

            // 根据碰撞体类型分别处理
            if (collider is CompositeCollider2D composite)
            {
                GenerateNodesForCompositeCollider(composite, isOneWay);
            }
            else if (collider is PolygonCollider2D polygon)
            {
                GenerateNodesForPolygonCollider(polygon, isOneWay);
            }
            else
            {
                // BoxCollider2D, EdgeCollider2D 等使用简单的 bounds 处理
                GenerateNodesForSimplePlatform(collider, collider.bounds, isOneWay);
            }
        }

        /// <summary>
        /// 为 CompositeCollider2D 生成节点（支持多路径）
        /// </summary>
        private void GenerateNodesForCompositeCollider(CompositeCollider2D composite, bool isOneWay)
        {
            int pathCount = composite.pathCount;

            for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
            {
                _pathPointsCache.Clear();
                int pointCount = composite.GetPath(pathIndex, _pathPointsCache);

                if (pointCount < 2) continue;

                // 提取该路径的顶部边缘并生成节点
                GenerateNodesForPath(_pathPointsCache, composite, isOneWay);
            }
        }

        /// <summary>
        /// 为 PolygonCollider2D 生成节点（支持多路径）
        /// </summary>
        private void GenerateNodesForPolygonCollider(PolygonCollider2D polygon, bool isOneWay)
        {
            int pathCount = polygon.pathCount;

            for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
            {
                var points = polygon.GetPath(pathIndex);
                if (points.Length < 2) continue;

                // 转换为世界坐标
                _pathPointsCache.Clear();
                var transform = polygon.transform;
                foreach (var localPoint in points)
                {
                    _pathPointsCache.Add(transform.TransformPoint(localPoint));
                }

                GenerateNodesForPath(_pathPointsCache, polygon, isOneWay);
            }
        }

        /// <summary>
        /// 从路径点中提取顶部边缘并生成节点
        /// </summary>
        private void GenerateNodesForPath(List<Vector2> worldPoints, Collider2D collider, bool isOneWay)
        {
            if (worldPoints.Count < 2) return;

            // 找到顶部边缘：遍历所有边，找出近似水平且位于顶部的边
            var topEdges = FindTopEdges(worldPoints);

            foreach (var edge in topEdges)
            {
                GenerateNodesForEdge(edge.left, edge.right, edge.y, collider, isOneWay);
            }
        }

        /// <summary>
        /// 从多边形路径中找出顶部边缘
        /// 使用法线方向判断：边的法线 Y 分量 > 0 即为顶部边（可行走表面）
        /// </summary>
        private List<(float left, float right, float y)> FindTopEdges(List<Vector2> points)
        {
            var edges = new List<(float left, float right, float y)>();
            const float slopeThreshold = 0.5f; // 斜率阈值，放宽以支持斜坡
            const float mergeThreshold = 0.1f; // Y 坐标合并阈值
            const float normalYThreshold = 0.3f; // 法线 Y 分量阈值

            int count = points.Count;
            for (int i = 0; i < count; i++)
            {
                var p1 = points[i];
                var p2 = points[(i + 1) % count];

                // 计算边的水平跨度和垂直跨度
                float dx = Mathf.Abs(p2.x - p1.x);
                float dy = Mathf.Abs(p2.y - p1.y);

                // 跳过垂直边或太短的边
                if (dx < 0.1f) continue;

                // 检查是否近似水平
                float slope = dy / dx;
                if (slope > slopeThreshold) continue;

                // 使用叉积计算边的法线方向
                // 对于逆时针多边形（Unity 2D 物理默认），法线朝外
                // edge = p2 - p1, normal = (-edge.y, edge.x) 旋转90度
                Vector2 edge = p2 - p1;
                Vector2 normal = new Vector2(-edge.y, edge.x).normalized;

                // 法线 Y > 0 表示边朝上（顶部边候选）
                bool isTopEdge = normal.y > normalYThreshold;

                if (isTopEdge)
                {
                    float edgeY = (p1.y + p2.y) / 2f;
                    float left = Mathf.Min(p1.x, p2.x);
                    float right = Mathf.Max(p1.x, p2.x);
                    edges.Add((left, right, edgeY));
                }
            }

            // 合并相邻的边
            return MergeAdjacentEdges(edges, mergeThreshold);
        }


        /// <summary>
        /// 合并相邻的顶部边缘
        /// </summary>
        private List<(float left, float right, float y)> MergeAdjacentEdges(
            List<(float left, float right, float y)> edges, float threshold)
        {
            if (edges.Count <= 1) return edges;

            // 按 Y 坐标和 X 坐标排序
            edges.Sort((a, b) =>
            {
                int yCompare = b.y.CompareTo(a.y); // Y 降序
                return yCompare != 0 ? yCompare : a.left.CompareTo(b.left);
            });

            var merged = new List<(float left, float right, float y)>();
            var current = edges[0];

            for (int i = 1; i < edges.Count; i++)
            {
                var next = edges[i];

                // 检查是否可以合并（Y 坐标接近且 X 范围重叠或相邻）
                if (Mathf.Abs(current.y - next.y) < threshold &&
                    next.left <= current.right + threshold)
                {
                    // 合并
                    current = (
                        Mathf.Min(current.left, next.left),
                        Mathf.Max(current.right, next.right),
                        (current.y + next.y) / 2f
                    );
                }
                else
                {
                    merged.Add(current);
                    current = next;
                }
            }
            merged.Add(current);

            return merged;
        }

        /// <summary>
        /// 为单条顶部边缘生成节点
        /// </summary>
        private void GenerateNodesForEdge(float left, float right, float y, Collider2D collider, bool isOneWay)
        {
            float width = right - left;
            float nodeSpacing = config.ActualNodeSpacing;

            // 平台太窄，只生成一个中心节点
            if (width < config.MinPlatformWidth)
            {
                Vector3 centerPos = new Vector3((left + right) / 2f, y, 0f);
                AddNode(PlatformNodeData.CreateSurface(nextNodeId++, centerPos, collider, isOneWay));
                return;
            }

            // 生成左边缘节点
            Vector3 leftEdgePos = new Vector3(left + config.EdgeInset, y, 0f);
            AddNode(PlatformNodeData.CreateEdge(nextNodeId++, leftEdgePos, collider, true, isOneWay));

            // 生成右边缘节点
            Vector3 rightEdgePos = new Vector3(right - config.EdgeInset, y, 0f);
            AddNode(PlatformNodeData.CreateEdge(nextNodeId++, rightEdgePos, collider, false, isOneWay));

            // 生成中间表面节点
            float innerWidth = width - 2 * config.EdgeInset;
            int innerNodeCount = Mathf.FloorToInt(innerWidth / nodeSpacing);

            if (innerNodeCount > 0)
            {
                float actualSpacing = innerWidth / (innerNodeCount + 1);
                for (int i = 1; i <= innerNodeCount; i++)
                {
                    float x = left + config.EdgeInset + actualSpacing * i;
                    Vector3 surfacePos = new Vector3(x, y, 0f);
                    AddNode(PlatformNodeData.CreateSurface(nextNodeId++, surfacePos, collider, isOneWay));
                }
            }
        }

        /// <summary>
        /// 为简单碰撞体生成节点（使用 bounds）
        /// </summary>
        private void GenerateNodesForSimplePlatform(Collider2D collider, Bounds bounds, bool isOneWay)
        {
            float left = bounds.min.x;
            float right = bounds.max.x;
            float top = bounds.max.y;

            GenerateNodesForEdge(left, right, top, collider, isOneWay);
        }

        /// <summary>
        /// 添加节点
        /// </summary>
        private void AddNode(PlatformNodeData node)
        {
            NodeIdToIndex[node.NodeId] = Nodes.Count;
            Nodes.Add(node);
        }

        /// <summary>
        /// 生成同平台行走链接
        /// </summary>
        private void GenerateWalkLinks()
        {
            // 按平台分组节点
            var platformNodes = new Dictionary<Collider2D, List<int>>();

            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                if (node.PlatformCollider == null) continue;

                if (!platformNodes.ContainsKey(node.PlatformCollider))
                {
                    platformNodes[node.PlatformCollider] = new List<int>();
                }
                platformNodes[node.PlatformCollider].Add(i);
            }

            // 为每个平台的节点生成行走链接
            foreach (var kvp in platformNodes)
            {
                var nodeIndices = kvp.Value;

                // 按 X 坐标排序
                nodeIndices.Sort((a, b) => Nodes[a].Position.x.CompareTo(Nodes[b].Position.x));

                // 相邻节点之间创建双向行走链接
                for (int i = 0; i < nodeIndices.Count - 1; i++)
                {
                    int fromIndex = nodeIndices[i];
                    int toIndex = nodeIndices[i + 1];

                    var fromNode = Nodes[fromIndex];
                    var toNode = Nodes[toIndex];

                    float distance = Vector2.Distance(fromNode.Position, toNode.Position);

                    // 创建双向链接
                    Links.Add(PlatformLinkData.CreateWalk(fromNode.NodeId, toNode.NodeId, distance));
                    Links.Add(PlatformLinkData.CreateWalk(toNode.NodeId, fromNode.NodeId, distance));
                }
            }
        }

        /// <summary>
        /// 获取节点数据
        /// </summary>
        public PlatformNodeData? GetNode(int nodeId)
        {
            if (NodeIdToIndex.TryGetValue(nodeId, out int index))
            {
                return Nodes[index];
            }
            return null;
        }

        /// <summary>
        /// 查找最近的节点（使用空间索引加速）
        /// </summary>
        public PlatformNodeData? FindNearestNode(Vector2 position, float maxDistance = float.MaxValue)
        {
            // 优先使用空间索引
            if (SpatialGrid != null)
            {
                return SpatialGrid.FindNearest(position, maxDistance);
            }

            // 回退到线性搜索
            PlatformNodeData? nearest = null;
            float nearestDist = maxDistance;

            foreach (var node in Nodes)
            {
                float dist = Vector2.Distance(position, node.Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = node;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 查找指定范围内的所有节点（使用空间索引加速）
        /// </summary>
        public List<PlatformNodeData> FindNodesInRange(Vector2 position, float range)
        {
            // 优先使用空间索引
            if (SpatialGrid != null)
            {
                _nodesInRangeCache.Clear();
                SpatialGrid.FindNodesInRange(position, range, _nodesInRangeCache);
                return new List<PlatformNodeData>(_nodesInRangeCache);
            }

            // 回退到线性搜索
            var result = new List<PlatformNodeData>();

            foreach (var node in Nodes)
            {
                if (Vector2.Distance(position, node.Position) <= range)
                {
                    result.Add(node);
                }
            }

            return result;
        }

        /// <summary>
        /// 查找指定范围内的所有节点（无 GC 分配版本）
        /// </summary>
        /// <param name="position">查询位置</param>
        /// <param name="range">范围半径</param>
        /// <param name="results">结果列表（调用者提供）</param>
        public void FindNodesInRangeNonAlloc(Vector2 position, float range, List<PlatformNodeData> results)
        {
            results.Clear();

            if (SpatialGrid != null)
            {
                SpatialGrid.FindNodesInRange(position, range, results);
            }
            else
            {
                foreach (var node in Nodes)
                {
                    if (Vector2.Distance(position, node.Position) <= range)
                    {
                        results.Add(node);
                    }
                }
            }
        }

        /// <summary>
        /// 获取节点的所有出边链接
        /// </summary>
        public List<PlatformLinkData> GetOutgoingLinks(int nodeId)
        {
            var result = new List<PlatformLinkData>();

            foreach (var link in Links)
            {
                if (link.FromNodeId == nodeId)
                {
                    result.Add(link);
                }
            }

            return result;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 绘制扫描区域
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireCube(config.ScanCenter, config.ScanSize);

            if (!IsGenerated) return;

            // 绘制节点
            foreach (var node in Nodes)
            {
                switch (node.NodeType)
                {
                    case PlatformNodeType.LeftEdge:
                    case PlatformNodeType.RightEdge:
                        Gizmos.color = Color.yellow;
                        break;
                    case PlatformNodeType.OneWay:
                        Gizmos.color = Color.cyan;
                        break;
                    default:
                        Gizmos.color = node.IsOneWay ? Color.cyan : Color.green;
                        break;
                }

                Gizmos.DrawSphere(node.Position, 0.2f);
            }

            // 绘制链接
            foreach (var link in Links)
            {
                var fromNode = GetNode(link.FromNodeId);
                var toNode = GetNode(link.ToNodeId);

                if (!fromNode.HasValue || !toNode.HasValue) continue;

                switch (link.LinkType)
                {
                    case PlatformLinkType.Walk:
                        Gizmos.color = Color.green;
                        break;
                    case PlatformLinkType.Jump:
                        Gizmos.color = Color.yellow;
                        break;
                    case PlatformLinkType.Fall:
                    case PlatformLinkType.DropThrough:
                        Gizmos.color = Color.blue;
                        break;
                }

                Gizmos.DrawLine(fromNode.Value.Position, toNode.Value.Position);
            }
        }
#endif
    }
}
