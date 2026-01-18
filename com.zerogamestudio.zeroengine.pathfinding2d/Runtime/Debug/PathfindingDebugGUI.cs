// PathfindingDebugGUI.cs
// 寻路调试 GUI 面板
// 在运行时显示寻路状态信息（可选组件）

using UnityEngine;

namespace ZeroEngine.Pathfinding2D
{
    /// <summary>
    /// 寻路调试 GUI 面板
    /// 在 Game 视图显示运行时寻路状态
    /// </summary>
    public class PathfindingDebugGUI : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("关联的寻路器")]
        private Platform2DPathfinder _pathfinder;

        [SerializeField]
        [Tooltip("启用 GUI 显示")]
        private bool _enableGUI = true;

        [SerializeField]
        [Tooltip("GUI 位置偏移")]
        private Vector2 _guiOffset = new Vector2(10, 10);

        [SerializeField]
        [Tooltip("GUI 宽度")]
        private float _guiWidth = 250f;

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;

        /// <summary>启用/禁用 GUI</summary>
        public bool EnableGUI
        {
            get => _enableGUI;
            set => _enableGUI = value;
        }

        private void Awake()
        {
            if (_pathfinder == null)
            {
                _pathfinder = GetComponent<Platform2DPathfinder>();
            }
        }

        private void OnGUI()
        {
            if (!_enableGUI || _pathfinder == null) return;

            InitStyles();

            float x = _guiOffset.x;
            float y = _guiOffset.y;
            float lineHeight = 20f;
            float padding = 5f;

            var path = _pathfinder.CurrentPath;

            // 计算面板高度
            int lineCount = 6;
            if (path != null && path.Status == PathStatus.Valid)
            {
                lineCount += 4;
            }
            float panelHeight = lineCount * lineHeight + padding * 2;

            // 绘制背景
            GUI.Box(new Rect(x, y, _guiWidth, panelHeight), "", _boxStyle);

            // 标题
            GUI.Label(new Rect(x + padding, y + padding, _guiWidth - padding * 2, lineHeight),
                "Pathfinding Debug", _headerStyle);
            y += lineHeight + padding;

            // 基本信息
            GUI.Label(new Rect(x + padding, y, _guiWidth - padding * 2, lineHeight),
                $"Has Valid Path: {_pathfinder.HasValidPath}", _labelStyle);
            y += lineHeight;

            if (path == null)
            {
                GUI.Label(new Rect(x + padding, y, _guiWidth - padding * 2, lineHeight),
                    "Path: None", _labelStyle);
                return;
            }

            GUI.Label(new Rect(x + padding, y, _guiWidth - padding * 2, lineHeight),
                $"Status: {path.Status}", _labelStyle);
            y += lineHeight;

            if (path.Status != PathStatus.Valid) return;

            GUI.Label(new Rect(x + padding, y, _guiWidth - padding * 2, lineHeight),
                $"Commands: {path.CurrentIndex + 1}/{path.Commands.Count}", _labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(x + padding, y, _guiWidth - padding * 2, lineHeight),
                $"Complete: {path.IsComplete}", _labelStyle);
            y += lineHeight;

            // 当前指令
            var currentCmd = path.GetCurrentCommand();
            if (currentCmd.HasValue)
            {
                var cmd = currentCmd.Value;
                y += padding;

                GUI.Label(new Rect(x + padding, y, _guiWidth - padding * 2, lineHeight),
                    $"Current: {cmd.CommandType}", _labelStyle);
                y += lineHeight;

                GUI.Label(new Rect(x + padding, y, _guiWidth - padding * 2, lineHeight),
                    $"Target: {cmd.Target.x:F1}, {cmd.Target.y:F1}", _labelStyle);
                y += lineHeight;

                if (cmd.CommandType == MoveCommandType.Jump)
                {
                    GUI.Label(new Rect(x + padding, y, _guiWidth - padding * 2, lineHeight),
                        $"Jump Vel: X={cmd.JumpVelocityX:F1}, Y={cmd.JumpVelocityY:F1}", _labelStyle);
                    y += lineHeight;

                    // 显示是否有缓存轨迹
                    bool hasCachedTrajectory = cmd.JumpTrajectory != null && cmd.JumpTrajectory.Length > 0;
                    GUI.Label(new Rect(x + padding, y, _guiWidth - padding * 2, lineHeight),
                        $"Cached Trajectory: {(hasCachedTrajectory ? cmd.JumpTrajectory.Length + " pts" : "None")}", _labelStyle);
                }
            }
        }

        private void InitStyles()
        {
            if (_boxStyle != null) return;

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(2, 2, new Color(0, 0, 0, 0.7f)) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow }
            };
        }

        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
