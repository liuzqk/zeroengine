using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ZeroEngine.Dialog;

namespace ZeroEngine.Editor.Dialog
{
    /// <summary>
    /// GraphView for visual dialog graph editing.
    /// </summary>
    public class DialogGraphView : GraphView
    {
        private readonly DialogGraphEditorWindow _window;
        private DialogGraphSO _graph;
        private readonly Dictionary<string, DialogGraphNode> _nodeViews = new();

        // Node colors
        public static readonly Color StartNodeColor = new Color(0.2f, 0.8f, 0.2f);
        public static readonly Color EndNodeColor = new Color(0.8f, 0.2f, 0.2f);
        public static readonly Color TextNodeColor = new Color(0.3f, 0.5f, 0.9f);
        public static readonly Color ChoiceNodeColor = new Color(0.9f, 0.7f, 0.2f);
        public static readonly Color ConditionNodeColor = new Color(0.7f, 0.4f, 0.9f);
        public static readonly Color SetVariableNodeColor = new Color(0.4f, 0.7f, 0.7f);
        public static readonly Color RandomNodeColor = new Color(0.9f, 0.5f, 0.3f);
        public static readonly Color CallbackNodeColor = new Color(0.6f, 0.6f, 0.6f);

        public DialogGraphView(DialogGraphEditorWindow window)
        {
            _window = window;

            // Add manipulators
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ContentZoomer());

            // Add grid background
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // Set up callbacks
            graphViewChanged = OnGraphViewChanged;
            nodeCreationRequest = OnNodeCreationRequest;

            // Add right-click context menu
            this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));

            // Load stylesheet
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.zerogamestudio.zeroengine/Editor/Dialog/Graph/DialogGraphStyles.uss");
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }

            // Keyboard shortcuts
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        public void LoadGraph(DialogGraphSO graph)
        {
            _graph = graph;

            // Clear existing
            foreach (var node in _nodeViews.Values)
            {
                RemoveElement(node);
            }
            _nodeViews.Clear();

            foreach (var edge in edges.ToList())
            {
                RemoveElement(edge);
            }

            if (_graph == null) return;

            // Create node views
            foreach (var node in _graph.Nodes)
            {
                CreateNodeView(node);
            }

            // Create edges
            foreach (var node in _graph.Nodes)
            {
                CreateEdgesForNode(node);
            }

            // Frame all content
            schedule.Execute(() =>
            {
                FrameAll();
            });
        }

        private void CreateNodeView(DialogNode node)
        {
            DialogGraphNode nodeView = node.Type switch
            {
                DialogNodeType.Start => new StartNodeView(node as DialogStartNode),
                DialogNodeType.End => new EndNodeView(node as DialogEndNode),
                DialogNodeType.Text => new TextNodeView(node as DialogTextNode),
                DialogNodeType.Choice => new ChoiceNodeView(node as DialogChoiceNode),
                DialogNodeType.Condition => new ConditionNodeView(node as DialogConditionNode),
                DialogNodeType.SetVariable => new SetVariableNodeView(node as DialogSetVariableNode),
                DialogNodeType.Random => new RandomNodeView(node as DialogRandomNode),
                DialogNodeType.Callback => new CallbackNodeView(node as DialogCallbackNode),
                _ => null
            };

            if (nodeView == null) return;

            nodeView.SetPosition(new Rect(node.EditorPosition, Vector2.zero));
            _nodeViews[node.Id] = nodeView;
            AddElement(nodeView);
        }

        private void CreateEdgesForNode(DialogNode node)
        {
            if (!_nodeViews.TryGetValue(node.Id, out var sourceView)) return;

            var outputIds = node.GetOutputNodeIds();
            var outputPorts = sourceView.GetOutputPorts();

            for (int i = 0; i < outputIds.Count && i < outputPorts.Count; i++)
            {
                string targetId = outputIds[i];
                if (string.IsNullOrEmpty(targetId) || targetId == "__END__") continue;

                if (_nodeViews.TryGetValue(targetId, out var targetView))
                {
                    var inputPort = targetView.GetInputPort();
                    if (inputPort != null)
                    {
                        var edge = outputPorts[i].ConnectTo(inputPort);
                        AddElement(edge);
                    }
                }
            }
        }

        public void CreateNode(DialogNodeType type)
        {
            if (_graph == null) return;

            Undo.RecordObject(_graph, "Create Dialog Node");

            var node = DialogGraphSO.CreateNode(type);
            if (node == null) return;

            // Generate unique ID
            node.Id = _graph.GenerateUniqueId(type.ToString());

            // Position in center of view
            var viewCenter = contentViewContainer.WorldToLocal(
                new Vector2(layout.width / 2, layout.height / 2));
            node.EditorPosition = viewCenter;

            _graph.AddNode(node);
            EditorUtility.SetDirty(_graph);

            CreateNodeView(node);
        }

        public void SaveGraphPositions()
        {
            if (_graph == null) return;

            bool changed = false;
            foreach (var kvp in _nodeViews)
            {
                var node = _graph.GetNode(kvp.Key);
                if (node != null)
                {
                    var pos = kvp.Value.GetPosition().position;
                    if (node.EditorPosition != pos)
                    {
                        node.EditorPosition = pos;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(_graph);
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_graph == null) return change;

            // Handle node removal
            if (change.elementsToRemove != null)
            {
                Undo.RecordObject(_graph, "Delete Dialog Elements");

                foreach (var element in change.elementsToRemove)
                {
                    if (element is DialogGraphNode nodeView)
                    {
                        _graph.RemoveNode(nodeView.NodeId);
                        _nodeViews.Remove(nodeView.NodeId);
                    }
                    else if (element is Edge edge)
                    {
                        // Disconnect edge
                        if (edge.output?.node is DialogGraphNode outputNode &&
                            edge.input?.node is DialogGraphNode inputNode)
                        {
                            outputNode.DisconnectOutput(edge.output, inputNode.NodeId);
                        }
                    }
                }

                EditorUtility.SetDirty(_graph);
            }

            // Handle edge creation
            if (change.edgesToCreate != null)
            {
                Undo.RecordObject(_graph, "Connect Dialog Nodes");

                foreach (var edge in change.edgesToCreate)
                {
                    if (edge.output?.node is DialogGraphNode outputNode &&
                        edge.input?.node is DialogGraphNode inputNode)
                    {
                        outputNode.ConnectOutput(edge.output, inputNode.NodeId);
                    }
                }

                EditorUtility.SetDirty(_graph);
            }

            // Handle moves
            if (change.movedElements != null)
            {
                foreach (var element in change.movedElements)
                {
                    if (element is DialogGraphNode nodeView)
                    {
                        var node = _graph.GetNode(nodeView.NodeId);
                        if (node != null)
                        {
                            node.EditorPosition = nodeView.GetPosition().position;
                        }
                    }
                }

                EditorUtility.SetDirty(_graph);
            }

            return change;
        }

        private void OnNodeCreationRequest(NodeCreationContext context)
        {
            // Show node creation menu
            var screenPoint = GUIUtility.GUIToScreenPoint(context.screenMousePosition);
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Text"), false, () => CreateNodeAtPosition(DialogNodeType.Text, context.screenMousePosition));
            menu.AddItem(new GUIContent("Choice"), false, () => CreateNodeAtPosition(DialogNodeType.Choice, context.screenMousePosition));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Condition"), false, () => CreateNodeAtPosition(DialogNodeType.Condition, context.screenMousePosition));
            menu.AddItem(new GUIContent("Set Variable"), false, () => CreateNodeAtPosition(DialogNodeType.SetVariable, context.screenMousePosition));
            menu.AddItem(new GUIContent("Random"), false, () => CreateNodeAtPosition(DialogNodeType.Random, context.screenMousePosition));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Callback"), false, () => CreateNodeAtPosition(DialogNodeType.Callback, context.screenMousePosition));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Start"), false, () => CreateNodeAtPosition(DialogNodeType.Start, context.screenMousePosition));
            menu.AddItem(new GUIContent("End"), false, () => CreateNodeAtPosition(DialogNodeType.End, context.screenMousePosition));

            menu.ShowAsContext();
        }

        private void CreateNodeAtPosition(DialogNodeType type, Vector2 screenPosition)
        {
            if (_graph == null) return;

            Undo.RecordObject(_graph, "Create Dialog Node");

            var node = DialogGraphSO.CreateNode(type);
            if (node == null) return;

            node.Id = _graph.GenerateUniqueId(type.ToString());

            // Convert screen position to graph position
            var localPos = contentViewContainer.WorldToLocal(screenPosition);
            node.EditorPosition = localPos;

            _graph.AddNode(node);
            EditorUtility.SetDirty(_graph);

            CreateNodeView(node);
        }

        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (_graph == null)
            {
                evt.menu.AppendAction("No graph loaded", null, DropdownMenuAction.Status.Disabled);
                return;
            }

            var localPos = contentViewContainer.WorldToLocal(evt.mousePosition);

            evt.menu.AppendAction("Add/Text Node", _ => CreateNodeAt(DialogNodeType.Text, localPos));
            evt.menu.AppendAction("Add/Choice Node", _ => CreateNodeAt(DialogNodeType.Choice, localPos));
            evt.menu.AppendSeparator("Add/");
            evt.menu.AppendAction("Add/Condition Node", _ => CreateNodeAt(DialogNodeType.Condition, localPos));
            evt.menu.AppendAction("Add/Set Variable Node", _ => CreateNodeAt(DialogNodeType.SetVariable, localPos));
            evt.menu.AppendAction("Add/Random Node", _ => CreateNodeAt(DialogNodeType.Random, localPos));
            evt.menu.AppendSeparator("Add/");
            evt.menu.AppendAction("Add/Callback Node", _ => CreateNodeAt(DialogNodeType.Callback, localPos));
            evt.menu.AppendSeparator("Add/");
            evt.menu.AppendAction("Add/Start Node", _ => CreateNodeAt(DialogNodeType.Start, localPos));
            evt.menu.AppendAction("Add/End Node", _ => CreateNodeAt(DialogNodeType.End, localPos));
        }

        private void CreateNodeAt(DialogNodeType type, Vector2 position)
        {
            if (_graph == null) return;

            Undo.RecordObject(_graph, "Create Dialog Node");

            var node = DialogGraphSO.CreateNode(type);
            if (node == null) return;

            node.Id = _graph.GenerateUniqueId(type.ToString());
            node.EditorPosition = position;

            _graph.AddNode(node);
            EditorUtility.SetDirty(_graph);

            CreateNodeView(node);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            // Delete selected nodes
            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                DeleteSelection();
                evt.StopPropagation();
            }

            // Duplicate
            if (evt.keyCode == KeyCode.D && evt.ctrlKey)
            {
                DuplicateSelection();
                evt.StopPropagation();
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(port =>
                port.direction != startPort.direction &&
                port.node != startPort.node &&
                port.portType == startPort.portType
            ).ToList();
        }

        private void DuplicateSelection()
        {
            if (_graph == null) return;

            var selectedNodes = selection.OfType<DialogGraphNode>().ToList();
            if (selectedNodes.Count == 0) return;

            Undo.RecordObject(_graph, "Duplicate Dialog Nodes");

            ClearSelection();

            foreach (var nodeView in selectedNodes)
            {
                var originalNode = _graph.GetNode(nodeView.NodeId);
                if (originalNode == null || originalNode.Type == DialogNodeType.Start) continue;

                // Create copy
                var newNode = DialogGraphSO.CreateNode(originalNode.Type);
                CopyNodeData(originalNode, newNode);

                newNode.Id = _graph.GenerateUniqueId(originalNode.Type.ToString());
                newNode.EditorPosition = originalNode.EditorPosition + new Vector2(50, 50);
                newNode.OutputNodeId = null; // Clear connections

                _graph.AddNode(newNode);
                CreateNodeView(newNode);

                // Select the new node
                if (_nodeViews.TryGetValue(newNode.Id, out var newNodeView))
                {
                    AddToSelection(newNodeView);
                }
            }

            EditorUtility.SetDirty(_graph);
        }

        private void CopyNodeData(DialogNode source, DialogNode target)
        {
            target.Comment = source.Comment;

            if (source is DialogTextNode textSrc && target is DialogTextNode textTgt)
            {
                textTgt.Speaker = textSrc.Speaker;
                textTgt.Text = textSrc.Text;
                textTgt.LocalizationKey = textSrc.LocalizationKey;
                textTgt.VoiceKey = textSrc.VoiceKey;
                textTgt.PortraitKey = textSrc.PortraitKey;
            }
            else if (source is DialogChoiceNode choiceSrc && target is DialogChoiceNode choiceTgt)
            {
                choiceTgt.PromptText = choiceSrc.PromptText;
                choiceTgt.Choices = new List<DialogChoiceNode.ChoiceOption>(choiceSrc.Choices);
            }
            else if (source is DialogConditionNode condSrc && target is DialogConditionNode condTgt)
            {
                condTgt.Condition = condSrc.Condition;
            }
            else if (source is DialogSetVariableNode varSrc && target is DialogSetVariableNode varTgt)
            {
                varTgt.Variable = varSrc.Variable;
                varTgt.Op = varSrc.Op;
                varTgt.Value = varSrc.Value;
                varTgt.TypeHint = varSrc.TypeHint;
            }
            else if (source is DialogRandomNode randSrc && target is DialogRandomNode randTgt)
            {
                randTgt.Outputs = new List<DialogRandomNode.WeightedOutput>(randSrc.Outputs);
            }
            else if (source is DialogCallbackNode cbSrc && target is DialogCallbackNode cbTgt)
            {
                cbTgt.CallbackId = cbSrc.CallbackId;
                cbTgt.Parameter = cbSrc.Parameter;
                cbTgt.WaitForCompletion = cbSrc.WaitForCompletion;
            }
            else if (source is DialogEndNode endSrc && target is DialogEndNode endTgt)
            {
                endTgt.EndTag = endSrc.EndTag;
            }
        }
    }
}
