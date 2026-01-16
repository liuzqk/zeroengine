using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ZeroEngine.BehaviorTree;

namespace ZeroEngine.Editor.BehaviorTree
{
    /// <summary>
    /// Visual node element for behavior tree graph.
    /// </summary>
    public class BTGraphNode : Node
    {
        public string NodeId { get; }
        public BTNodeType NodeType { get; }

        private Port _inputPort;
        private Port _outputPort;

        public BTGraphNode(BTNodeData data, bool isRoot)
        {
            NodeId = data.Id;
            NodeType = data.Type;

            // Title and color
            title = string.IsNullOrEmpty(data.Name) ? data.Type.ToString() : data.Name;

            var headerColor = GetNodeColor(data.Type);
            if (isRoot)
            {
                headerColor = BTGraphView.RootColor;
            }
            titleContainer.style.backgroundColor = headerColor;

            // Style
            mainContainer.style.minWidth = 180;

            // Create ports based on node type
            CreatePorts(data);

            // Add content based on type
            AddNodeContent(data);

            // Root indicator
            if (isRoot)
            {
                var rootBadge = new Label("ROOT");
                rootBadge.style.backgroundColor = new Color(1f, 0.4f, 0.1f);
                rootBadge.style.color = Color.white;
                rootBadge.style.paddingLeft = 6;
                rootBadge.style.paddingRight = 6;
                rootBadge.style.paddingTop = 2;
                rootBadge.style.paddingBottom = 2;
                rootBadge.style.borderTopLeftRadius = 4;
                rootBadge.style.borderTopRightRadius = 4;
                rootBadge.style.borderBottomLeftRadius = 4;
                rootBadge.style.borderBottomRightRadius = 4;
                rootBadge.style.fontSize = 10;
                rootBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
                rootBadge.style.marginLeft = 4;
                titleContainer.Add(rootBadge);

                // Root cannot be deleted
                capabilities &= ~Capabilities.Deletable;
            }

            RefreshExpandedState();
            RefreshPorts();
        }

        private void CreatePorts(BTNodeData data)
        {
            // Input port (all nodes except root-only)
            _inputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            _inputPort.portName = "";
            _inputPort.style.flexDirection = FlexDirection.Column;
            inputContainer.Add(_inputPort);

            // Output port (composites, decorators)
            bool hasOutput = IsComposite(data.Type) || IsDecorator(data.Type);
            if (hasOutput)
            {
                var capacity = IsDecorator(data.Type) ? Port.Capacity.Single : Port.Capacity.Multi;
                _outputPort = InstantiatePort(Orientation.Vertical, Direction.Output, capacity, typeof(bool));
                _outputPort.portName = "";
                _outputPort.style.flexDirection = FlexDirection.ColumnReverse;
                outputContainer.Add(_outputPort);
            }
        }

        private void AddNodeContent(BTNodeData data)
        {
            // Type badge
            var typeBadge = new Label(GetCategoryName(data.Type));
            typeBadge.style.fontSize = 9;
            typeBadge.style.color = new Color(0.7f, 0.7f, 0.7f);
            typeBadge.style.marginLeft = 8;
            typeBadge.style.marginTop = 2;
            extensionContainer.Add(typeBadge);

            // Type-specific content
            switch (data.Type)
            {
                case BTNodeType.Repeater:
                    AddField("Repeat", data.RepeatCount == -1 ? "∞ Infinite" : data.RepeatCount.ToString());
                    break;

                case BTNodeType.Wait:
                    AddField("Duration", $"{data.WaitDuration}s");
                    break;

                case BTNodeType.Log:
                    AddMultilineField("Message", data.LogMessage);
                    break;

                case BTNodeType.Parallel:
                    AddField("Success", data.SuccessPolicy.ToString());
                    AddField("Failure", data.FailurePolicy.ToString());
                    break;
            }

            // Comment
            if (!string.IsNullOrEmpty(data.Comment))
            {
                var commentLabel = new Label($"// {data.Comment}");
                commentLabel.style.fontSize = 10;
                commentLabel.style.color = new Color(0.5f, 0.7f, 0.5f);
                commentLabel.style.marginLeft = 8;
                commentLabel.style.marginTop = 4;
                commentLabel.style.whiteSpace = WhiteSpace.Normal;
                commentLabel.style.maxWidth = 160;
                extensionContainer.Add(commentLabel);
            }
        }

        private void AddField(string label, string value)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginLeft = 8;
            container.style.marginTop = 2;

            var labelElement = new Label(label + ":");
            labelElement.style.width = 60;
            labelElement.style.fontSize = 11;
            container.Add(labelElement);

            var valueElement = new Label(value ?? "-");
            valueElement.style.fontSize = 11;
            valueElement.style.color = new Color(0.9f, 0.9f, 0.9f);
            container.Add(valueElement);

            extensionContainer.Add(container);
        }

        private void AddMultilineField(string label, string value)
        {
            var container = new VisualElement();
            container.style.marginLeft = 8;
            container.style.marginTop = 4;

            var labelElement = new Label(label + ":");
            labelElement.style.fontSize = 10;
            container.Add(labelElement);

            string displayValue = value ?? "(empty)";
            if (displayValue.Length > 30)
            {
                displayValue = displayValue.Substring(0, 27) + "...";
            }

            var valueElement = new Label(displayValue);
            valueElement.style.fontSize = 11;
            valueElement.style.backgroundColor = new Color(0, 0, 0, 0.2f);
            valueElement.style.paddingLeft = 4;
            valueElement.style.paddingRight = 4;
            valueElement.style.paddingTop = 2;
            valueElement.style.paddingBottom = 2;
            valueElement.style.borderTopLeftRadius = 3;
            valueElement.style.borderTopRightRadius = 3;
            valueElement.style.borderBottomLeftRadius = 3;
            valueElement.style.borderBottomRightRadius = 3;
            valueElement.style.whiteSpace = WhiteSpace.Normal;
            valueElement.style.maxWidth = 150;
            container.Add(valueElement);

            extensionContainer.Add(container);
        }

        public Port GetInputPort() => _inputPort;
        public Port GetOutputPort() => _outputPort;

        private static Color GetNodeColor(BTNodeType type)
        {
            if (IsComposite(type)) return BTGraphView.CompositeColor;
            if (IsDecorator(type)) return BTGraphView.DecoratorColor;
            return BTGraphView.LeafColor;
        }

        private static bool IsComposite(BTNodeType type)
        {
            return type == BTNodeType.Sequence ||
                   type == BTNodeType.Selector ||
                   type == BTNodeType.Parallel;
        }

        private static bool IsDecorator(BTNodeType type)
        {
            return type == BTNodeType.Repeater ||
                   type == BTNodeType.Inverter ||
                   type == BTNodeType.AlwaysSucceed ||
                   type == BTNodeType.AlwaysFail ||
                   type == BTNodeType.Conditional;
        }

        private static string GetCategoryName(BTNodeType type)
        {
            if (IsComposite(type)) return "Composite";
            if (IsDecorator(type)) return "Decorator";
            return "Leaf";
        }
    }
}
