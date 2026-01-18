// PathfindingDebugger.cs
// 寻路调试可视化器
// 在运行时绘制完整的寻路轨迹，包括跳跃抛物线

using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Pathfinding2D
{
    /// <summary>
    /// 寻路调试可视化器
    /// 在 Scene 视图绘制完整路径轨迹（包括跳跃抛物线）
    /// </summary>
    public class PathfindingDebugger : MonoBehaviour
    {
        [SerializeField]
        private PathfindingDebugConfig _config = new PathfindingDebugConfig();

        [SerializeField]
        private Platform2DPathfinder _pathfinder;

        /// <summary>调试配置</summary>
        public PathfindingDebugConfig Config => _config;

        /// <summary>关联的寻路器</summary>
        public Platform2DPathfinder Pathfinder
        {
            get => _pathfinder;
            set => _pathfinder = value;
        }

        // 缓存的轨迹数据
        private readonly List<TrajectoryCache> _cachedTrajectories = new List<TrajectoryCache>();
        private int _cachedPathHash;

        /// <summary>
        /// 轨迹缓存结构
        /// </summary>
        private struct TrajectoryCache
        {
            public Vector3[] Points;
            public MoveCommandType CommandType;
        }

        private void Awake()
        {
            if (_pathfinder == null)
            {
                _pathfinder = GetComponent<Platform2DPathfinder>();
            }
        }

        /// <summary>
        /// 计算跳跃抛物线轨迹点
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="velocityX">X 方向初速度</param>
        /// <param name="velocityY">Y 方向初速度</param>
        /// <param name="duration">飞行时间</param>
        /// <param name="segments">采样点数</param>
        /// <returns>轨迹点数组</returns>
        public Vector3[] CalculateJumpTrajectory(
            Vector3 start,
            float velocityX,
            float velocityY,
            float duration,
            int segments = 20)
        {
            var points = new Vector3[segments + 1];
            float gravity = Physics2D.gravity.y * _config.GravityScale;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments * duration;
                float x = start.x + velocityX * t;
                float y = start.y + velocityY * t + 0.5f * gravity * t * t;
                points[i] = new Vector3(x, y, start.z);
            }

            return points;
        }

        /// <summary>
        /// 从预计算轨迹创建 Vector3 数组（用于已缓存的链接轨迹）
        /// </summary>
        public Vector3[] ConvertTrajectory(Vector2[] trajectory, float z = 0f)
        {
            if (trajectory == null) return null;

            var points = new Vector3[trajectory.Length];
            for (int i = 0; i < trajectory.Length; i++)
            {
                points[i] = new Vector3(trajectory[i].x, trajectory[i].y, z);
            }
            return points;
        }

        /// <summary>
        /// 获取指令对应的颜色
        /// </summary>
        public Color GetCommandColor(MoveCommandType type)
        {
            return type switch
            {
                MoveCommandType.Walk => _config.WalkColor,
                MoveCommandType.Jump => _config.JumpColor,
                MoveCommandType.Fall => _config.FallColor,
                MoveCommandType.DropDown => _config.DropDownColor,
                _ => Color.white
            };
        }

        /// <summary>
        /// 更新轨迹缓存
        /// </summary>
        private void UpdateTrajectoryCache()
        {
            if (_pathfinder == null || _pathfinder.CurrentPath == null)
            {
                _cachedTrajectories.Clear();
                _cachedPathHash = 0;
                return;
            }

            var path = _pathfinder.CurrentPath;
            int newHash = path.GetHashCode() ^ path.CurrentIndex;

            if (newHash == _cachedPathHash) return;

            _cachedPathHash = newHash;
            _cachedTrajectories.Clear();

            Vector3 currentPos = transform.position;

            for (int i = path.CurrentIndex; i < path.Commands.Count; i++)
            {
                var cmd = path.Commands[i];
                TrajectoryCache cache;

                if (cmd.CommandType == MoveCommandType.Jump)
                {
                    // 优先使用预计算的轨迹（来自 JumpLinkCalculator）
                    Vector3[] trajectoryPoints;
                    if (cmd.JumpTrajectory != null && cmd.JumpTrajectory.Length > 0)
                    {
                        trajectoryPoints = ConvertTrajectory(cmd.JumpTrajectory, currentPos.z);
                    }
                    else
                    {
                        // 回退：实时计算轨迹
                        trajectoryPoints = CalculateJumpTrajectory(
                            currentPos,
                            cmd.JumpVelocityX,
                            cmd.JumpVelocityY,
                            cmd.EstimatedDuration,
                            _config.TrajectorySegments
                        );
                    }

                    cache = new TrajectoryCache
                    {
                        Points = trajectoryPoints,
                        CommandType = cmd.CommandType
                    };
                }
                else
                {
                    // 非跳跃指令：只存储起点和终点
                    cache = new TrajectoryCache
                    {
                        Points = new[] { currentPos, cmd.Target },
                        CommandType = cmd.CommandType
                    };
                }

                _cachedTrajectories.Add(cache);
                currentPos = cmd.Target;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_config.EnablePathGizmo) return;
            if (_config.DrawOnlyWhenSelected) return;
            if (!Application.isPlaying) return;

            DrawPathGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if (!_config.EnablePathGizmo) return;
            if (!Application.isPlaying) return;

            DrawPathGizmos();
        }

        private void DrawPathGizmos()
        {
            if (_pathfinder == null) return;

            var path = _pathfinder.CurrentPath;
            if (path == null || path.Status != PathStatus.Valid) return;
            if (path.Commands == null || path.Commands.Count == 0) return;

            UpdateTrajectoryCache();

            // 绘制全路径预览
            DrawFullPathPreview();

            Vector3 currentPos = transform.position;

            // 绘制从当前位置到第一个目标的连线
            if (path.CurrentIndex < path.Commands.Count)
            {
                var firstCmd = path.Commands[path.CurrentIndex];
                Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
                Gizmos.DrawLine(currentPos, firstCmd.Target);
            }

            // 绘制所有指令
            int trajectoryIndex = 0;
            for (int i = path.CurrentIndex; i < path.Commands.Count; i++)
            {
                var cmd = path.Commands[i];
                Color cmdColor = GetCommandColor(cmd.CommandType);

                // 绘制目标节点
                if (i == path.CurrentIndex)
                {
                    // 当前目标用实心球
                    Gizmos.color = _config.CurrentTargetColor;
                    Gizmos.DrawSphere(cmd.Target, _config.CurrentTargetSize);
                }
                else
                {
                    Gizmos.color = cmdColor;
                    Gizmos.DrawWireSphere(cmd.Target, _config.NodeSize);
                }

                // 绘制路径线/轨迹
                if (trajectoryIndex < _cachedTrajectories.Count)
                {
                    var cache = _cachedTrajectories[trajectoryIndex];
                    Gizmos.color = cmdColor;

                    if (cmd.CommandType == MoveCommandType.Jump && _config.EnableJumpTrajectory)
                    {
                        // 跳跃：绘制抛物线
                        for (int j = 0; j < cache.Points.Length - 1; j++)
                        {
                            Gizmos.DrawLine(cache.Points[j], cache.Points[j + 1]);
                        }
                    }
                    else if (cache.Points.Length >= 2)
                    {
                        // 其他：绘制直线
                        Gizmos.DrawLine(cache.Points[0], cache.Points[1]);
                    }

                    trajectoryIndex++;
                }

                // 节点标签
                if (_config.EnableNodeLabels)
                {
                    string label = cmd.CommandType.ToString();
                    if (cmd.CommandType == MoveCommandType.Jump)
                    {
                        label += $"\nVy={cmd.JumpVelocityY:F1}";
                    }
                    UnityEditor.Handles.Label(cmd.Target + Vector3.up * 0.5f, label);
                }
            }

            // 绘制路径终点
            Gizmos.color = _config.EndPointColor;
            Gizmos.DrawWireCube(path.EndPosition, Vector3.one * _config.NodeSize * 1.5f);
        }

        /// <summary>
        /// 绘制完整路径预览（从起点到终点的红线）
        /// 包括所有跳跃轨迹
        /// </summary>
        private void DrawFullPathPreview()
        {
            if (!_config.EnableFullPathPreview) return;

            var path = _pathfinder.CurrentPath;
            if (path == null) return;

            // 绘制已完成的路径（灰色）
            Gizmos.color = _config.CompletedPathColor;
            Vector3 prevPos = path.StartPosition;
            for (int i = 0; i < path.CurrentIndex && i < path.Commands.Count; i++)
            {
                var cmd = path.Commands[i];
                Gizmos.DrawLine(prevPos, cmd.Target);
                prevPos = cmd.Target;
            }

            // 绘制剩余路径（红色）
            Gizmos.color = _config.FullPathColor;
            prevPos = path.CurrentIndex > 0 && path.CurrentIndex <= path.Commands.Count
                ? path.Commands[path.CurrentIndex - 1].Target
                : path.StartPosition;

            for (int i = path.CurrentIndex; i < path.Commands.Count; i++)
            {
                var cmd = path.Commands[i];

                if (cmd.CommandType == MoveCommandType.Jump)
                {
                    // 跳跃：绘制抛物线（优先使用缓存轨迹）
                    Vector3[] trajectory;
                    if (cmd.JumpTrajectory != null && cmd.JumpTrajectory.Length > 0)
                    {
                        trajectory = ConvertTrajectory(cmd.JumpTrajectory, prevPos.z);
                    }
                    else
                    {
                        trajectory = CalculateJumpTrajectory(
                            prevPos,
                            cmd.JumpVelocityX,
                            cmd.JumpVelocityY,
                            cmd.EstimatedDuration,
                            _config.TrajectorySegments
                        );
                    }
                    for (int j = 0; j < trajectory.Length - 1; j++)
                    {
                        Gizmos.DrawLine(trajectory[j], trajectory[j + 1]);
                    }
                }
                else
                {
                    Gizmos.DrawLine(prevPos, cmd.Target);
                }

                prevPos = cmd.Target;
            }

            // 到终点的最后一段
            if (path.Commands.Count > 0)
            {
                var lastTarget = path.Commands[path.Commands.Count - 1].Target;
                if (Vector3.Distance(lastTarget, path.EndPosition) > 0.1f)
                {
                    Gizmos.DrawLine(lastTarget, path.EndPosition);
                }
            }
        }
#endif
    }
}
