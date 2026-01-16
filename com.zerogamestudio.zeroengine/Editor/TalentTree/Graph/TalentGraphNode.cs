using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ZeroEngine.TalentTree;

namespace ZeroEngine.Editor.TalentTree
{
    /// <summary>
    /// 天赋节点可视化元素
    /// </summary>
    public class TalentGraphNode : Node
    {
        public TalentNodeSO NodeData { get; private set; }
        public event Action NodeSelected;

        private Port _inputPort;
        private Port _outputPort;

        private static readonly Color NormalColor = new Color(0.3f, 0.5f, 0.3f);
        private static readonly Color KeystoneColor = new Color(0.6f, 0.4f, 0.1f);
        private static readonly Color StartColor = new Color(0.2f, 0.4f, 0.6f);
        private static readonly Color BranchColor = new Color(0.5f, 0.3f, 0.5f);

        public TalentGraphNode(TalentNodeSO node)
        {
            NodeData = node;
            title = node.DisplayName;

            // 设置颜色
            var headerColor = node.NodeType switch
            {
                TalentNodeType.Normal => NormalColor,
                TalentNodeType.Keystone => KeystoneColor,
                TalentNodeType.Start => StartColor,
                TalentNodeType.Branch => BranchColor,
                _ => NormalColor
            };

            titleContainer.style.backgroundColor = headerColor;

            // 创建端口
            if (node.NodeType != TalentNodeType.Start)
            {
                _inputPort = CreateInputPort();
            }
            _outputPort = CreateOutputPort();

            // 添加内容
            AddContent();

            // 设置样式
            mainContainer.style.minWidth = 180;
            mainContainer.style.minHeight = 80;

            RefreshExpandedState();
            RefreshPorts();
        }

        private Port CreateInputPort()
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            port.portName = "In";
            port.portColor = Color.cyan;
            inputContainer.Add(port);
            return port;
        }

        private Port CreateOutputPort()
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            port.portName = "Out";
            port.portColor = Color.yellow;
            outputContainer.Add(port);
            return port;
        }

        private void AddContent()
        {
            var container = new VisualElement();
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.paddingTop = 4;
            container.style.paddingBottom = 4;

            // 节点类型
            var typeLabel = new Label($"[{NodeData.NodeType}]");
            typeLabel.style.fontSize = 10;
            typeLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            container.Add(typeLabel);

            // 最大等级
            var levelLabel = new Label($"Max Lv: {NodeData.MaxLevel}");
            levelLabel.style.fontSize = 11;
            container.Add(levelLabel);

            // 消耗
            var costLabel = new Label($"Cost: {NodeData.PointCostPerLevel}/Lv");
            costLabel.style.fontSize = 11;
            container.Add(costLabel);

            // 效果数量
            if (NodeData.Effects != null && NodeData.Effects.Count > 0)
            {
                var effectLabel = new Label($"Effects: {NodeData.Effects.Count}");
                effectLabel.style.fontSize = 11;
                effectLabel.style.color = new Color(0.5f, 0.8f, 0.5f);
                container.Add(effectLabel);
            }

            // 图标
            if (NodeData.Icon != null)
            {
                var icon = new Image();
                icon.image = NodeData.Icon.texture;
                icon.style.width = 32;
                icon.style.height = 32;
                icon.style.alignSelf = Align.Center;
                icon.style.marginTop = 4;
                container.Add(icon);
            }

            extensionContainer.Add(container);
        }

        public Port GetInputPort() => _inputPort;
        public Port GetOutputPort() => _outputPort;

        public override void OnSelected()
        {
            base.OnSelected();
            NodeSelected?.Invoke();
        }

        public void Refresh()
        {
            title = NodeData.DisplayName;
            extensionContainer.Clear();
            AddContent();
            RefreshExpandedState();
        }
    }
}
