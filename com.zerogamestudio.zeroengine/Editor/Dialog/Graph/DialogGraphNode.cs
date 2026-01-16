using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ZeroEngine.Dialog;

namespace ZeroEngine.Editor.Dialog
{
    /// <summary>
    /// Base class for dialog node visual elements.
    /// </summary>
    public abstract class DialogGraphNode : Node
    {
        public string NodeId { get; protected set; }
        public DialogNodeType NodeType { get; protected set; }

        protected Port InputPort;
        protected Port OutputPort;
        protected readonly List<Port> OutputPorts = new();

        protected DialogGraphNode(string title, Color headerColor)
        {
            this.title = title;
            titleContainer.style.backgroundColor = headerColor;

            // Style
            mainContainer.style.minWidth = 200;
        }

        public virtual Port GetInputPort() => InputPort;

        public virtual List<Port> GetOutputPorts()
        {
            if (OutputPorts.Count > 0) return OutputPorts;
            if (OutputPort != null) return new List<Port> { OutputPort };
            return new List<Port>();
        }

        public virtual void ConnectOutput(Port port, string targetNodeId)
        {
            // Default implementation - override for multi-output nodes
        }

        public virtual void DisconnectOutput(Port port, string targetNodeId)
        {
            // Default implementation - override for multi-output nodes
        }

        protected Port CreateInputPort(string name = "In")
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            port.portName = name;
            port.portColor = Color.white;
            inputContainer.Add(port);
            InputPort = port;
            return port;
        }

        protected Port CreateOutputPort(string name = "Out")
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = name;
            port.portColor = Color.white;
            outputContainer.Add(port);
            OutputPort = port;
            OutputPorts.Add(port);
            return port;
        }

        protected void AddContentField(string label, string value)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.marginTop = 4;

            var labelElement = new Label(label + ":");
            labelElement.style.width = 60;
            labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(labelElement);

            var valueElement = new Label(value ?? "(empty)");
            valueElement.style.flexGrow = 1;
            valueElement.style.whiteSpace = WhiteSpace.Normal;
            valueElement.style.maxWidth = 200;
            container.Add(valueElement);

            extensionContainer.Add(container);
        }

        protected void AddMultilineField(string label, string value, int maxLines = 3)
        {
            var container = new VisualElement();
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.marginTop = 4;

            var labelElement = new Label(label + ":");
            labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            labelElement.style.marginBottom = 2;
            container.Add(labelElement);

            var valueElement = new Label(TruncateText(value, maxLines) ?? "(empty)");
            valueElement.style.whiteSpace = WhiteSpace.Normal;
            valueElement.style.maxWidth = 250;
            valueElement.style.backgroundColor = new Color(0, 0, 0, 0.2f);
            valueElement.style.paddingLeft = 4;
            valueElement.style.paddingRight = 4;
            valueElement.style.paddingTop = 2;
            valueElement.style.paddingBottom = 2;
            valueElement.style.borderTopLeftRadius = 3;
            valueElement.style.borderTopRightRadius = 3;
            valueElement.style.borderBottomLeftRadius = 3;
            valueElement.style.borderBottomRightRadius = 3;
            container.Add(valueElement);

            extensionContainer.Add(container);
        }

        private string TruncateText(string text, int maxLines)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var lines = text.Split('\n');
            if (lines.Length <= maxLines) return text;

            return string.Join("\n", lines, 0, maxLines) + "...";
        }

        protected void FinishSetup()
        {
            RefreshExpandedState();
            RefreshPorts();
        }
    }

    /// <summary>
    /// Start node view.
    /// </summary>
    public class StartNodeView : DialogGraphNode
    {
        private readonly DialogStartNode _node;

        public StartNodeView(DialogStartNode node) : base("Start", DialogGraphView.StartNodeColor)
        {
            _node = node;
            NodeId = node.Id;
            NodeType = DialogNodeType.Start;

            CreateOutputPort("Next");
            FinishSetup();

            // Prevent deletion
            capabilities &= ~Capabilities.Deletable;
        }

        public override void ConnectOutput(Port port, string targetNodeId)
        {
            _node.OutputNodeId = targetNodeId;
        }

        public override void DisconnectOutput(Port port, string targetNodeId)
        {
            if (_node.OutputNodeId == targetNodeId)
            {
                _node.OutputNodeId = null;
            }
        }
    }

    /// <summary>
    /// End node view.
    /// </summary>
    public class EndNodeView : DialogGraphNode
    {
        private readonly DialogEndNode _node;

        public EndNodeView(DialogEndNode node) : base("End", DialogGraphView.EndNodeColor)
        {
            _node = node;
            NodeId = node.Id;
            NodeType = DialogNodeType.End;

            CreateInputPort("In");

            if (!string.IsNullOrEmpty(node.EndTag))
            {
                AddContentField("Tag", node.EndTag);
            }

            FinishSetup();
        }
    }

    /// <summary>
    /// Text node view.
    /// </summary>
    public class TextNodeView : DialogGraphNode
    {
        private readonly DialogTextNode _node;

        public TextNodeView(DialogTextNode node) : base("Text", DialogGraphView.TextNodeColor)
        {
            _node = node;
            NodeId = node.Id;
            NodeType = DialogNodeType.Text;

            CreateInputPort("In");
            CreateOutputPort("Next");

            if (!string.IsNullOrEmpty(node.Speaker))
            {
                AddContentField("Speaker", node.Speaker);
            }

            AddMultilineField("Text", node.Text);

            FinishSetup();
        }

        public override void ConnectOutput(Port port, string targetNodeId)
        {
            _node.OutputNodeId = targetNodeId;
        }

        public override void DisconnectOutput(Port port, string targetNodeId)
        {
            if (_node.OutputNodeId == targetNodeId)
            {
                _node.OutputNodeId = null;
            }
        }
    }

    /// <summary>
    /// Choice node view.
    /// </summary>
    public class ChoiceNodeView : DialogGraphNode
    {
        private readonly DialogChoiceNode _node;
        private readonly List<Port> _choicePorts = new();

        public ChoiceNodeView(DialogChoiceNode node) : base("Choice", DialogGraphView.ChoiceNodeColor)
        {
            _node = node;
            NodeId = node.Id;
            NodeType = DialogNodeType.Choice;

            CreateInputPort("In");

            if (!string.IsNullOrEmpty(node.PromptText))
            {
                AddMultilineField("Prompt", node.PromptText, 2);
            }

            // Create output ports for each choice
            for (int i = 0; i < node.Choices.Count; i++)
            {
                var choice = node.Choices[i];
                var port = CreateChoicePort(i, choice.Text);
                _choicePorts.Add(port);
            }

            FinishSetup();
        }

        private Port CreateChoicePort(int index, string text)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));

            string displayText = text ?? "(empty)";
            if (displayText.Length > 25)
            {
                displayText = displayText.Substring(0, 22) + "...";
            }

            port.portName = $"[{index + 1}] {displayText}";
            port.portColor = DialogGraphView.ChoiceNodeColor;
            outputContainer.Add(port);
            OutputPorts.Add(port);
            return port;
        }

        public override List<Port> GetOutputPorts() => _choicePorts;

        public override void ConnectOutput(Port port, string targetNodeId)
        {
            int index = _choicePorts.IndexOf(port);
            if (index >= 0 && index < _node.Choices.Count)
            {
                _node.Choices[index].TargetNodeId = targetNodeId;
            }
        }

        public override void DisconnectOutput(Port port, string targetNodeId)
        {
            int index = _choicePorts.IndexOf(port);
            if (index >= 0 && index < _node.Choices.Count)
            {
                if (_node.Choices[index].TargetNodeId == targetNodeId)
                {
                    _node.Choices[index].TargetNodeId = null;
                }
            }
        }
    }

    /// <summary>
    /// Condition node view.
    /// </summary>
    public class ConditionNodeView : DialogGraphNode
    {
        private readonly DialogConditionNode _node;
        private readonly Port _truePort;
        private readonly Port _falsePort;

        public ConditionNodeView(DialogConditionNode node) : base("Condition", DialogGraphView.ConditionNodeColor)
        {
            _node = node;
            NodeId = node.Id;
            NodeType = DialogNodeType.Condition;

            CreateInputPort("In");

            AddContentField("If", node.Condition ?? "(empty)");

            _truePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            _truePort.portName = "True";
            _truePort.portColor = new Color(0.3f, 0.8f, 0.3f);
            outputContainer.Add(_truePort);
            OutputPorts.Add(_truePort);

            _falsePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            _falsePort.portName = "False";
            _falsePort.portColor = new Color(0.8f, 0.3f, 0.3f);
            outputContainer.Add(_falsePort);
            OutputPorts.Add(_falsePort);

            FinishSetup();
        }

        public override void ConnectOutput(Port port, string targetNodeId)
        {
            if (port == _truePort)
            {
                _node.TrueNodeId = targetNodeId;
            }
            else if (port == _falsePort)
            {
                _node.FalseNodeId = targetNodeId;
            }
        }

        public override void DisconnectOutput(Port port, string targetNodeId)
        {
            if (port == _truePort && _node.TrueNodeId == targetNodeId)
            {
                _node.TrueNodeId = null;
            }
            else if (port == _falsePort && _node.FalseNodeId == targetNodeId)
            {
                _node.FalseNodeId = null;
            }
        }
    }

    /// <summary>
    /// Set Variable node view.
    /// </summary>
    public class SetVariableNodeView : DialogGraphNode
    {
        private readonly DialogSetVariableNode _node;

        public SetVariableNodeView(DialogSetVariableNode node) : base("Set Variable", DialogGraphView.SetVariableNodeColor)
        {
            _node = node;
            NodeId = node.Id;
            NodeType = DialogNodeType.SetVariable;

            CreateInputPort("In");
            CreateOutputPort("Next");

            string opStr = node.Op switch
            {
                DialogSetVariableNode.Operation.Set => "=",
                DialogSetVariableNode.Operation.Add => "+=",
                DialogSetVariableNode.Operation.Subtract => "-=",
                DialogSetVariableNode.Operation.Multiply => "*=",
                DialogSetVariableNode.Operation.Divide => "/=",
                DialogSetVariableNode.Operation.Toggle => "= !",
                _ => "="
            };

            string expr = $"{node.Variable ?? "?"} {opStr} {node.Value ?? "?"}";
            AddContentField("Set", expr);

            FinishSetup();
        }

        public override void ConnectOutput(Port port, string targetNodeId)
        {
            _node.OutputNodeId = targetNodeId;
        }

        public override void DisconnectOutput(Port port, string targetNodeId)
        {
            if (_node.OutputNodeId == targetNodeId)
            {
                _node.OutputNodeId = null;
            }
        }
    }

    /// <summary>
    /// Random node view.
    /// </summary>
    public class RandomNodeView : DialogGraphNode
    {
        private readonly DialogRandomNode _node;
        private readonly List<Port> _outputPorts = new();

        public RandomNodeView(DialogRandomNode node) : base("Random", DialogGraphView.RandomNodeColor)
        {
            _node = node;
            NodeId = node.Id;
            NodeType = DialogNodeType.Random;

            CreateInputPort("In");

            for (int i = 0; i < node.Outputs.Count; i++)
            {
                var output = node.Outputs[i];
                var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                port.portName = $"[{output.Weight}%]";
                port.portColor = DialogGraphView.RandomNodeColor;
                outputContainer.Add(port);
                _outputPorts.Add(port);
                OutputPorts.Add(port);
            }

            FinishSetup();
        }

        public override List<Port> GetOutputPorts() => _outputPorts;

        public override void ConnectOutput(Port port, string targetNodeId)
        {
            int index = _outputPorts.IndexOf(port);
            if (index >= 0 && index < _node.Outputs.Count)
            {
                _node.Outputs[index].NodeId = targetNodeId;
            }
        }

        public override void DisconnectOutput(Port port, string targetNodeId)
        {
            int index = _outputPorts.IndexOf(port);
            if (index >= 0 && index < _node.Outputs.Count)
            {
                if (_node.Outputs[index].NodeId == targetNodeId)
                {
                    _node.Outputs[index].NodeId = null;
                }
            }
        }
    }

    /// <summary>
    /// Callback node view.
    /// </summary>
    public class CallbackNodeView : DialogGraphNode
    {
        private readonly DialogCallbackNode _node;

        public CallbackNodeView(DialogCallbackNode node) : base("Callback", DialogGraphView.CallbackNodeColor)
        {
            _node = node;
            NodeId = node.Id;
            NodeType = DialogNodeType.Callback;

            CreateInputPort("In");
            CreateOutputPort("Next");

            AddContentField("ID", node.CallbackId);

            if (!string.IsNullOrEmpty(node.Parameter))
            {
                AddContentField("Param", node.Parameter);
            }

            if (node.WaitForCompletion)
            {
                var waitLabel = new Label("Waits for completion");
                waitLabel.style.paddingLeft = 8;
                waitLabel.style.marginTop = 4;
                waitLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                waitLabel.style.color = new Color(1, 1, 0.5f);
                extensionContainer.Add(waitLabel);
            }

            FinishSetup();
        }

        public override void ConnectOutput(Port port, string targetNodeId)
        {
            _node.OutputNodeId = targetNodeId;
        }

        public override void DisconnectOutput(Port port, string targetNodeId)
        {
            if (_node.OutputNodeId == targetNodeId)
            {
                _node.OutputNodeId = null;
            }
        }
    }
}
