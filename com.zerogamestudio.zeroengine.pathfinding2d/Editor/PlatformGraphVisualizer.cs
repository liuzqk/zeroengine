// PlatformGraphVisualizer.cs
// 平台图可视化编辑器工具
// 在 Scene 视图中绘制平台节点和链接

using UnityEngine;
using UnityEditor;

namespace ZeroEngine.Pathfinding2D.Editor
{
    /// <summary>
    /// 平台图可视化编辑器
    /// </summary>
    [CustomEditor(typeof(PlatformGraphGenerator))]
    public class PlatformGraphGeneratorEditor : UnityEditor.Editor
    {
        private PlatformGraphGenerator generator;

        private void OnEnable()
        {
            generator = target as PlatformGraphGenerator;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(10);

            // 状态信息
            EditorGUILayout.LabelField("状态信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("已生成", generator.IsGenerated ? "是" : "否");
            EditorGUILayout.LabelField("节点数", generator.Nodes?.Count.ToString() ?? "0");
            EditorGUILayout.LabelField("链接数", generator.Links?.Count.ToString() ?? "0");

            EditorGUILayout.Space(10);

            // 操作按钮
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("生成平台图", GUILayout.Height(30)))
            {
                generator.GeneratePlatformGraph();

                // 尝试同时生成跳跃链接
                var jumpCalculator = generator.GetComponent<JumpLinkCalculator>();
                if (jumpCalculator != null)
                {
                    jumpCalculator.GenerateJumpLinks();
                }

                SceneView.RepaintAll();
            }

            if (GUILayout.Button("清除平台图", GUILayout.Height(30)))
            {
                generator.ClearGraph();
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("刷新场景视图", GUILayout.Height(25)))
            {
                SceneView.RepaintAll();
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawGizmos(PlatformGraphGenerator generator, GizmoType gizmoType)
        {
            if (generator == null || !generator.IsGenerated) return;

            bool isSelected = (gizmoType & GizmoType.Selected) != 0;
            float alpha = isSelected ? 1f : 0.5f;

            // 绘制节点
            foreach (var node in generator.Nodes)
            {
                Color nodeColor;
                float nodeSize = 0.2f;

                switch (node.NodeType)
                {
                    case PlatformNodeType.LeftEdge:
                        nodeColor = new Color(1f, 0.8f, 0f, alpha); // 黄色 - 左边缘
                        nodeSize = 0.25f;
                        break;
                    case PlatformNodeType.RightEdge:
                        nodeColor = new Color(1f, 0.5f, 0f, alpha); // 橙色 - 右边缘
                        nodeSize = 0.25f;
                        break;
                    case PlatformNodeType.OneWay:
                        nodeColor = new Color(0f, 1f, 1f, alpha); // 青色 - 单向平台
                        break;
                    default:
                        nodeColor = node.IsOneWay
                            ? new Color(0f, 0.8f, 1f, alpha)   // 浅蓝 - 单向平台表面
                            : new Color(0f, 1f, 0f, alpha);    // 绿色 - 普通表面
                        break;
                }

                Gizmos.color = nodeColor;
                Gizmos.DrawSphere(node.Position, nodeSize);

                // 绘制节点 ID（仅选中时）
                if (isSelected)
                {
                    Handles.Label(node.Position + Vector3.up * 0.3f, node.NodeId.ToString());
                }
            }

            // 绘制链接
            foreach (var link in generator.Links)
            {
                var fromNode = generator.GetNode(link.FromNodeId);
                var toNode = generator.GetNode(link.ToNodeId);

                if (!fromNode.HasValue || !toNode.HasValue) continue;

                Color linkColor;
                float lineWidth = 1f;

                switch (link.LinkType)
                {
                    case PlatformLinkType.Walk:
                        linkColor = new Color(0f, 1f, 0f, alpha * 0.5f); // 绿色 - 行走
                        break;
                    case PlatformLinkType.Jump:
                        linkColor = new Color(1f, 1f, 0f, alpha); // 黄色 - 跳跃
                        lineWidth = 2f;
                        break;
                    case PlatformLinkType.Fall:
                        linkColor = new Color(0f, 0.5f, 1f, alpha); // 蓝色 - 下落
                        break;
                    case PlatformLinkType.DropThrough:
                        linkColor = new Color(0.5f, 0f, 1f, alpha); // 紫色 - 穿透下落
                        break;
                    default:
                        linkColor = new Color(1f, 1f, 1f, alpha * 0.3f);
                        break;
                }

                // 使用 Handles 绘制带箭头的线
                Handles.color = linkColor;

                Vector3 from = fromNode.Value.Position;
                Vector3 to = toNode.Value.Position;

                // 绘制线
                Handles.DrawLine(from, to, lineWidth);

                // 绘制箭头
                if (link.LinkType != PlatformLinkType.Walk) // 行走链接是双向的，不绘制箭头
                {
                    Vector3 dir = (to - from).normalized;
                    Vector3 arrowPos = Vector3.Lerp(from, to, 0.7f);
                    float arrowSize = 0.15f;

                    // 绘制简单的箭头
                    Vector3 right = Vector3.Cross(dir, Vector3.forward).normalized;
                    Handles.DrawLine(arrowPos, arrowPos - dir * arrowSize + right * arrowSize * 0.5f);
                    Handles.DrawLine(arrowPos, arrowPos - dir * arrowSize - right * arrowSize * 0.5f);
                }
            }
        }
    }

    /// <summary>
    /// 跳跃链接计算器编辑器
    /// </summary>
    [CustomEditor(typeof(JumpLinkCalculator))]
    public class JumpLinkCalculatorEditor : UnityEditor.Editor
    {
        private JumpLinkCalculator calculator;

        private void OnEnable()
        {
            calculator = target as JumpLinkCalculator;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("生成跳跃链接", GUILayout.Height(30)))
            {
                // 确保平台图已生成
                var generator = calculator.GetComponent<PlatformGraphGenerator>();
                if (generator != null && !generator.IsGenerated)
                {
                    generator.GeneratePlatformGraph();
                }

                calculator.GenerateJumpLinks();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("清除跳跃链接", GUILayout.Height(30)))
            {
                calculator.ClearJumpLinks();
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 平台寻路器编辑器
    /// </summary>
    [CustomEditor(typeof(Platform2DPathfinder))]
    public class Platform2DPathfinderEditor : UnityEditor.Editor
    {
        private Platform2DPathfinder pathfinder;

        private void OnEnable()
        {
            pathfinder = target as Platform2DPathfinder;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(10);

            // 状态信息
            EditorGUILayout.LabelField("运行时信息", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                var path = pathfinder.CurrentPath;
                if (path != null)
                {
                    EditorGUILayout.LabelField("路径状态", path.Status.ToString());
                    EditorGUILayout.LabelField("指令数", path.Commands.Count.ToString());
                    EditorGUILayout.LabelField("当前索引", path.CurrentIndex.ToString());
                    EditorGUILayout.LabelField("剩余指令", path.RemainingCount.ToString());
                }
                else
                {
                    EditorGUILayout.LabelField("路径状态", "无");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("运行时信息仅在播放模式下可用", MessageType.Info);
            }
        }
    }
}
