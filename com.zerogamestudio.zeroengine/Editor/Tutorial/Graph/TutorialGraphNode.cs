using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ZeroEngine.Tutorial;

namespace ZeroEngine.Editor.Tutorial
{
    /// <summary>
    /// Base class for tutorial step visual nodes (v1.14.0+)
    /// </summary>
    public abstract class TutorialGraphNode : Node
    {
        public int StepIndex { get; protected set; }
        public string StepType { get; protected set; }

        protected Port InputPort;
        protected Port OutputPort;
        protected readonly List<Port> OutputPorts = new();

        protected TutorialGraphNode(string title, Color headerColor, int stepIndex = -1)
        {
            this.title = title;
            StepIndex = stepIndex;
            titleContainer.style.backgroundColor = headerColor;
            mainContainer.style.minWidth = 180;
        }

        public virtual Port GetInputPort() => InputPort;

        public virtual List<Port> GetOutputPorts()
        {
            if (OutputPorts.Count > 0) return OutputPorts;
            if (OutputPort != null) return new List<Port> { OutputPort };
            return new List<Port>();
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
            valueElement.style.maxWidth = 150;
            container.Add(valueElement);

            extensionContainer.Add(container);
        }

        protected void AddMultilineField(string label, string value, int maxLines = 2)
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
            valueElement.style.maxWidth = 180;
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

    #region Special Nodes

    /// <summary>
    /// Start node (sequence entry point)
    /// </summary>
    public class StartStepNode : TutorialGraphNode
    {
        public StartStepNode() : base("Start", TutorialGraphView.StartNodeColor, -1)
        {
            StepType = "Start";
            CreateOutputPort("Begin");
            FinishSetup();

            capabilities &= ~Capabilities.Deletable;
        }
    }

    /// <summary>
    /// End node (sequence exit point)
    /// </summary>
    public class EndStepNode : TutorialGraphNode
    {
        public EndStepNode() : base("End", TutorialGraphView.EndNodeColor, -2)
        {
            StepType = "End";
            CreateInputPort("Complete");
            FinishSetup();

            capabilities &= ~Capabilities.Deletable;
        }
    }

    #endregion

    #region Step Nodes

    /// <summary>
    /// Generic step node for unknown step types
    /// </summary>
    public class GenericStepNode : TutorialGraphNode
    {
        public GenericStepNode(TutorialStep step, int index)
            : base(step?.StepType ?? "Step", new Color(0.5f, 0.5f, 0.5f), index)
        {
            StepType = step?.StepType ?? "Unknown";

            CreateInputPort();
            CreateOutputPort();

            if (!string.IsNullOrEmpty(step?.Description))
            {
                AddMultilineField("Info", step.Description);
            }

            FinishSetup();
        }
    }

    /// <summary>
    /// Dialogue step node
    /// </summary>
    public class DialogueStepNode : TutorialGraphNode
    {
        public DialogueStepNode(DialogueStep step, int index)
            : base("Dialogue", TutorialGraphView.DialogueNodeColor, index)
        {
            StepType = "Dialogue";

            CreateInputPort();
            CreateOutputPort();

            if (!string.IsNullOrEmpty(step.SpeakerName))
            {
                AddContentField("Speaker", step.SpeakerName);
            }

            AddMultilineField("Text", step.DialogueText);

            FinishSetup();
        }
    }

    /// <summary>
    /// Highlight step node
    /// </summary>
    public class HighlightStepNode : TutorialGraphNode
    {
        public HighlightStepNode(HighlightStep step, int index)
            : base("Highlight", TutorialGraphView.HighlightNodeColor, index)
        {
            StepType = "Highlight";

            CreateInputPort();
            CreateOutputPort();

            AddContentField("Target", step.TargetPath ?? "(none)");
            AddContentField("Type", step.HighlightType.ToString());

            if (!string.IsNullOrEmpty(step.HintText))
            {
                AddMultilineField("Hint", step.HintText);
            }

            FinishSetup();
        }
    }

    /// <summary>
    /// Wait Input step node
    /// </summary>
    public class WaitInputStepNode : TutorialGraphNode
    {
        public WaitInputStepNode(WaitInputStep step, int index)
            : base("Wait Input", TutorialGraphView.WaitNodeColor, index)        
        {
            StepType = "WaitInput";

            CreateInputPort();
            CreateOutputPort();

            string keyText = "(none)";
            if (step.RequiredKeys != null && step.RequiredKeys.Length > 0)
            {
                var keyNames = new string[step.RequiredKeys.Length];
                for (int i = 0; i < step.RequiredKeys.Length; i++)
                {
                    keyNames[i] = step.RequiredKeys[i].ToString();
                }
                keyText = step.RequireAll
                    ? string.Join(" + ", keyNames)
                    : string.Join(" or ", keyNames);
            }
            AddContentField("Keys", keyText);

            if (!string.IsNullOrEmpty(step.PromptText))
            {
                AddMultilineField("Prompt", step.PromptText);
            }

            FinishSetup();
        }
    }

    /// <summary>
    /// Wait Interaction step node
    /// </summary>
    public class WaitInteractionStepNode : TutorialGraphNode
    {
        public WaitInteractionStepNode(WaitInteractionStep step, int index)
            : base("Wait Interaction", TutorialGraphView.WaitNodeColor, index)
        {
            StepType = "WaitInteraction";

            CreateInputPort();
            CreateOutputPort();

            AddContentField("Target", step.InteractableId ?? "(any)");

            if (step.InteractionRequirement != InteractionRequirement.Any)
            {
                AddContentField("Type", step.InteractionRequirement.ToString());
            }

            FinishSetup();
        }
    }

    /// <summary>
    /// Wait Event step node
    /// </summary>
    public class WaitEventStepNode : TutorialGraphNode
    {
        public WaitEventStepNode(WaitEventStep step, int index)
            : base("Wait Event", TutorialGraphView.WaitNodeColor, index)
        {
            StepType = "WaitEvent";

            CreateInputPort();
            CreateOutputPort();

            AddContentField("Event", step.EventKey ?? "(none)");

            if (!string.IsNullOrEmpty(step.ExpectedValue))
            {
                AddContentField("Value", step.ExpectedValue);
            }

            FinishSetup();
        }
    }

    /// <summary>
    /// Move To step node
    /// </summary>
    public class MoveToStepNode : TutorialGraphNode
    {
        public MoveToStepNode(MoveToStep step, int index)
            : base("Move To", TutorialGraphView.MoveNodeColor, index)
        {
            StepType = "MoveTo";

            CreateInputPort();
            CreateOutputPort();

            if (!string.IsNullOrEmpty(step.TargetObjectPath))
            {
                AddContentField("Target", step.TargetObjectPath);
            }
            else
            {
                AddContentField("Position", step.TargetPosition.ToString());
            }

            AddContentField("Distance", $"{step.ArrivalDistance}m");

            FinishSetup();
        }
    }

    /// <summary>
    /// Delay step node
    /// </summary>
    public class DelayStepNode : TutorialGraphNode
    {
        public DelayStepNode(DelayStep step, int index)
            : base("Delay", TutorialGraphView.DelayNodeColor, index)
        {
            StepType = "Delay";

            CreateInputPort();
            CreateOutputPort();

            AddContentField("Duration", $"{step.Duration}s");

            if (step.ShowProgress)
            {
                var progressLabel = new Label("Shows progress");
                progressLabel.style.paddingLeft = 8;
                progressLabel.style.marginTop = 4;
                progressLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                progressLabel.style.color = new Color(0.7f, 0.7f, 1f);
                extensionContainer.Add(progressLabel);
            }

            FinishSetup();
        }
    }

    /// <summary>
    /// Callback step node
    /// </summary>
    public class CallbackStepNode : TutorialGraphNode
    {
        public CallbackStepNode(CallbackStep step, int index)
            : base("Callback", TutorialGraphView.DelayNodeColor, index)
        {
            StepType = "Callback";

            CreateInputPort();
            CreateOutputPort();

            AddContentField("ID", step.CallbackId ?? "(none)");

            if (step.WaitForComplete)
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
    }

    /// <summary>
    /// Composite step node
    /// </summary>
    public class CompositeStepNode : TutorialGraphNode
    {
        public CompositeStepNode(CompositeStep step, int index)
            : base($"Composite ({step.Mode})", TutorialGraphView.CompositeNodeColor, index)
        {
            StepType = "Composite";

            CreateInputPort();
            CreateOutputPort();

            int subCount = step.SubSteps?.Count ?? 0;
            AddContentField("SubSteps", subCount.ToString());

            // Show sub-step types
            if (step.SubSteps != null && step.SubSteps.Count > 0)
            {
                var subContainer = new VisualElement();
                subContainer.style.paddingLeft = 8;
                subContainer.style.marginTop = 4;

                int displayCount = Mathf.Min(step.SubSteps.Count, 3);
                for (int i = 0; i < displayCount; i++)
                {
                    var subStep = step.SubSteps[i];
                    var subLabel = new Label($"• {subStep?.StepType ?? "null"}");
                    subLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
                    subLabel.style.fontSize = 10;
                    subContainer.Add(subLabel);
                }

                if (step.SubSteps.Count > 3)
                {
                    var moreLabel = new Label($"  ... +{step.SubSteps.Count - 3} more");
                    moreLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                    moreLabel.style.fontSize = 10;
                    subContainer.Add(moreLabel);
                }

                extensionContainer.Add(subContainer);
            }

            FinishSetup();
        }
    }

    #endregion
}
