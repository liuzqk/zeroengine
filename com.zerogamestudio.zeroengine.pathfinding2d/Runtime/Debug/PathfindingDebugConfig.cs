// PathfindingDebugConfig.cs
// 寻路调试可视化配置
// 定义颜色、尺寸、开关等调试参数

using UnityEngine;

namespace ZeroEngine.Pathfinding2D
{
    /// <summary>
    /// 寻路调试可视化配置
    /// </summary>
    [System.Serializable]
    public class PathfindingDebugConfig
    {
        [Header("启用选项")]
        [Tooltip("启用路径 Gizmo 绘制")]
        public bool EnablePathGizmo = true;

        [Tooltip("启用跳跃轨迹绘制")]
        public bool EnableJumpTrajectory = true;

        [Tooltip("启用节点标签")]
        public bool EnableNodeLabels = false;

        [Tooltip("仅在选中时绘制")]
        public bool DrawOnlyWhenSelected = false;

        [Header("颜色配置")]
        [Tooltip("行走路径颜色")]
        public Color WalkColor = Color.green;

        [Tooltip("跳跃路径颜色")]
        public Color JumpColor = Color.yellow;

        [Tooltip("下落路径颜色")]
        public Color FallColor = Color.cyan;

        [Tooltip("穿透下落颜色")]
        public Color DropDownColor = Color.blue;

        [Tooltip("当前目标颜色")]
        public Color CurrentTargetColor = Color.red;

        [Tooltip("路径终点颜色")]
        public Color EndPointColor = Color.magenta;

        [Header("全路径预览")]
        [Tooltip("启用完整路径预览（红线）")]
        public bool EnableFullPathPreview = true;

        [Tooltip("全路径预览颜色")]
        public Color FullPathColor = new Color(1f, 0f, 0f, 0.7f);

        [Tooltip("已完成路径颜色（灰色）")]
        public Color CompletedPathColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        [Header("尺寸配置")]
        [Tooltip("节点球体大小")]
        public float NodeSize = 0.25f;

        [Tooltip("当前目标大小")]
        public float CurrentTargetSize = 0.35f;

        [Tooltip("跳跃轨迹采样点数")]
        [Range(10, 50)]
        public int TrajectorySegments = 20;

        [Tooltip("线条宽度")]
        [Range(1f, 5f)]
        public float LineWidth = 2f;

        [Header("物理参数")]
        [Tooltip("重力缩放（与 Rigidbody2D.gravityScale 匹配）")]
        public float GravityScale = 3f;
    }
}
