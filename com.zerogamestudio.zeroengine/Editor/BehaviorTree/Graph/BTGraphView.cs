using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ZeroEngine.BehaviorTree;

namespace ZeroEngine.Editor.BehaviorTree
{
    /// <summary>
    /// GraphView for visual behavior tree editing.
    /// </summary>
    public class BTGraphView : GraphView
    {
        private readonly BTGraphEditorWindow _window;
        private BTTreeAsset _tree;
        private readonly Dictionary<string, BTGraphNode> _nodeViews = new();

        // Node colors by category
        public static readonly Color CompositeColor = new Color(0.2f, 0.6f, 0.8f);
        public static readonly Color DecoratorColor = new Color(0.7f, 0.5f, 0.9f);
        public static readonly Color LeafColor = new Color(0.4f, 0.8f, 0.4f);
        public static readonly Color RootColor = new Color(0.9f, 0.6f, 0.2f);

        public BTGraphView(BTGraphEditorWindow window)
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
                "Packages/com.zerogamestudio.zeroengine/Editor/BehaviorTree/Graph/BTGraphStyles.uss");
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }

            // Keyboard shortcuts
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        public void LoadTree(BTTreeAsset tree)
        {
            _tree = tree;

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

            if (_tree == null) return;

            // Create node views
            foreach (var node in _tree.Nodes)
            {
                CreateNodeView(node);
            }

            // Create edges (parent -> child connections)
            foreach (var node in _tree.Nodes)
            {
                if (!_nodeViews.TryGetValue(node.Id, out var parentView)) continue;

                foreach (var childId in node.ChildIds)
                {
                    if (_nodeViews.TryGetValue(childId, out var childView))
                    {
                        var outputPort = parentView.GetOutputPort();
                        var inputPort = childView.GetInputPort();

                        if (outputPort != null && inputPort != null)
                        {
                            var edge = outputPort.ConnectTo(inputPort);
                            AddElement(edge);
                        }
                    }
                }
            }

            // Frame all content
            schedule.Execute(() =>
            {
                FrameAll();
            });
        }

        private void CreateNodeView(BTNodeData node)
        {
            var nodeView = new BTGraphNode(node, IsRootNode(node.Id));
            nodeView.SetPosition(new Rect(node.EditorPosition, Vector2.zero));
            _nodeViews[node.Id] = nodeView;
            AddElement(nodeView);
        }

        private bool IsRootNode(string id)
        {
            return _tree != null && _tree.RootNodeId == id;
        }

        public void CreateNode(BTNodeType type)
        {
            if (_tree == null) return;

            Undo.RecordObject(_tree, "Create BT Node");

            var node = BTTreeAsset.CreateNodeData(type);
            node.Id = _tree.GenerateUniqueId(type.ToString());

            var viewCenter = contentViewContainer.WorldToLocal(
                new Vector2(layout.width / 2, layout.height / 2));
            node.EditorPosition = viewCenter;

            _tree.AddNode(node);
            EditorUtility.SetDirty(_tree);

            CreateNodeView(node);
        }

        public void SaveTreePositions()
        {
            if (_tree == null) return;

            bool changed = false;
            foreach (var kvp in _nodeViews)
            {
                var node = _tree.GetNode(kvp.Key);
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
                EditorUtility.SetDirty(_tree);
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_tree == null) return change;

            // Handle node removal
            if (change.elementsToRemove != null)
            {
                Undo.RecordObject(_tree, "Delete BT Elements");

                foreach (var element in change.elementsToRemove)
                {
                    if (element is BTGraphNode nodeView)
                    {
                        // Can't delete root
                        if (_tree.RootNodeId == nodeView.NodeId)
                        {
                            continue;
                        }

                        // Remove from parent's children
                        var nodeData = _tree.GetNode(nodeView.NodeId);
                        if (nodeData != null && !string.IsNullOrEmpty(nodeData.ParentId))
                        {
                            var parent = _tree.GetNode(nodeData.ParentId);
                            parent?.ChildIds.Remove(nodeView.NodeId);
                        }

                        _tree.RemoveNode(nodeView.NodeId);
                        _nodeViews.Remove(nodeView.NodeId);
                    }
                    else if (element is Edge edge)
                    {
                        DisconnectEdge(edge);
                    }
                }

                EditorUtility.SetDirty(_tree);
            }

            // Handle edge creation
            if (change.edgesToCreate != null)
            {
                Undo.RecordObject(_tree, "Connect BT Nodes");

                foreach (var edge in change.edgesToCreate)
                {
                    ConnectEdge(edge);
                }

                EditorUtility.SetDirty(_tree);
            }

            // Handle moves
            if (change.movedElements != null)
            {
                foreach (var element in change.movedElements)
                {
                    if (element is BTGraphNode nodeView)
                    {
                        var node = _tree.GetNode(nodeView.NodeId);
                        if (node != null)
                        {
                            node.EditorPosition = nodeView.GetPosition().position;
                        }
                    }
                }

                EditorUtility.SetDirty(_tree);
            }

            return change;
        }

        private void ConnectEdge(Edge edge)
        {
            if (edge.output?.node is not BTGraphNode parentView) return;
            if (edge.input?.node is not BTGraphNode childView) return;

            var parentData = _tree.GetNode(parentView.NodeId);
            var childData = _tree.GetNode(childView.NodeId);

            if (parentData == null || childData == null) return;

            // Remove from old parent
            if (!string.IsNullOrEmpty(childData.ParentId))
            {
                var oldParent = _tree.GetNode(childData.ParentId);
                oldParent?.ChildIds.Remove(childData.Id);
            }

            // Add to new parent
            if (!parentData.ChildIds.Contains(childData.Id))
            {
                parentData.ChildIds.Add(childData.Id);
            }
            childData.ParentId = parentData.Id;
        }

        private void DisconnectEdge(Edge edge)
        {
            if (edge.output?.node is not BTGraphNode parentView) return;
            if (edge.input?.node is not BTGraphNode childView) return;

            var parentData = _tree.GetNode(parentView.NodeId);
            var childData = _tree.GetNode(childView.NodeId);

            if (parentData == null || childData == null) return;

            parentData.ChildIds.Remove(childData.Id);
            childData.ParentId = null;
        }

        private void OnNodeCreationRequest(NodeCreationContext context)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Composites/Sequence"), false, () => CreateNodeAt(BTNodeType.Sequence, context.screenMousePosition));
            menu.AddItem(new GUIContent("Composites/Selector"), false, () => CreateNodeAt(BTNodeType.Selector, context.screenMousePosition));
            menu.AddItem(new GUIContent("Composites/Parallel"), false, () => CreateNodeAt(BTNodeType.Parallel, context.screenMousePosition));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Decorators/Repeater"), false, () => CreateNodeAt(BTNodeType.Repeater, context.screenMousePosition));
            menu.AddItem(new GUIContent("Decorators/Inverter"), false, () => CreateNodeAt(BTNodeType.Inverter, context.screenMousePosition));
            menu.AddItem(new GUIContent("Decorators/Always Succeed"), false, () => CreateNodeAt(BTNodeType.AlwaysSucceed, context.screenMousePosition));
            menu.AddItem(new GUIContent("Decorators/Always Fail"), false, () => CreateNodeAt(BTNodeType.AlwaysFail, context.screenMousePosition));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Leaves/Wait"), false, () => CreateNodeAt(BTNodeType.Wait, context.screenMousePosition));
            menu.AddItem(new GUIContent("Leaves/Log"), false, () => CreateNodeAt(BTNodeType.Log, context.screenMousePosition));

            menu.ShowAsContext();
        }

        private void CreateNodeAt(BTNodeType type, Vector2 screenPosition)
        {
            if (_tree == null) return;

            Undo.RecordObject(_tree, "Create BT Node");

            var node = BTTreeAsset.CreateNodeData(type);
            node.Id = _tree.GenerateUniqueId(type.ToString());

            var localPos = contentViewContainer.WorldToLocal(screenPosition);
            node.EditorPosition = localPos;

            _tree.AddNode(node);
            EditorUtility.SetDirty(_tree);

            CreateNodeView(node);
        }

        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (_tree == null)
            {
                evt.menu.AppendAction("No tree loaded", null, DropdownMenuAction.Status.Disabled);
                return;
            }

            var localPos = contentViewContainer.WorldToLocal(evt.mousePosition);

            evt.menu.AppendAction("Add/Composites/Sequence", _ => CreateNodeAtLocal(BTNodeType.Sequence, localPos));
            evt.menu.AppendAction("Add/Composites/Selector", _ => CreateNodeAtLocal(BTNodeType.Selector, localPos));
            evt.menu.AppendAction("Add/Composites/Parallel", _ => CreateNodeAtLocal(BTNodeType.Parallel, localPos));

            evt.menu.AppendAction("Add/Decorators/Repeater", _ => CreateNodeAtLocal(BTNodeType.Repeater, localPos));
            evt.menu.AppendAction("Add/Decorators/Inverter", _ => CreateNodeAtLocal(BTNodeType.Inverter, localPos));
            evt.menu.AppendAction("Add/Decorators/Always Succeed", _ => CreateNodeAtLocal(BTNodeType.AlwaysSucceed, localPos));
            evt.menu.AppendAction("Add/Decorators/Always Fail", _ => CreateNodeAtLocal(BTNodeType.AlwaysFail, localPos));

            evt.menu.AppendAction("Add/Leaves/Wait", _ => CreateNodeAtLocal(BTNodeType.Wait, localPos));
            evt.menu.AppendAction("Add/Leaves/Log", _ => CreateNodeAtLocal(BTNodeType.Log, localPos));

            // Set as root option for selected node
            var selectedNodes = selection.OfType<BTGraphNode>().ToList();
            if (selectedNodes.Count == 1)
            {
                var selectedNode = selectedNodes[0];
                if (_tree.RootNodeId != selectedNode.NodeId)
                {
                    evt.menu.AppendSeparator("");
                    evt.menu.AppendAction("Set as Root", _ => SetAsRoot(selectedNode.NodeId));
                }
            }
        }

        private void CreateNodeAtLocal(BTNodeType type, Vector2 localPos)
        {
            if (_tree == null) return;

            Undo.RecordObject(_tree, "Create BT Node");

            var node = BTTreeAsset.CreateNodeData(type);
            node.Id = _tree.GenerateUniqueId(type.ToString());
            node.EditorPosition = localPos;

            _tree.AddNode(node);
            EditorUtility.SetDirty(_tree);

            CreateNodeView(node);
        }

        private void SetAsRoot(string nodeId)
        {
            if (_tree == null) return;

            Undo.RecordObject(_tree, "Set Root Node");

            _tree.RootNodeId = nodeId;
            EditorUtility.SetDirty(_tree);

            // Refresh views to update root highlighting
            LoadTree(_tree);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                DeleteSelection();
                evt.StopPropagation();
            }

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
            if (_tree == null) return;

            var selectedNodes = selection.OfType<BTGraphNode>().ToList();
            if (selectedNodes.Count == 0) return;

            Undo.RecordObject(_tree, "Duplicate BT Nodes");

            ClearSelection();

            foreach (var nodeView in selectedNodes)
            {
                // Can't duplicate root
                if (_tree.RootNodeId == nodeView.NodeId) continue;

                var original = _tree.GetNode(nodeView.NodeId);
                if (original == null) continue;

                var newNode = BTTreeAsset.CreateNodeData(original.Type);
                newNode.Id = _tree.GenerateUniqueId(original.Type.ToString());
                newNode.Name = original.Name;
                newNode.EditorPosition = original.EditorPosition + new Vector2(50, 50);
                newNode.RepeatCount = original.RepeatCount;
                newNode.WaitDuration = original.WaitDuration;
                newNode.LogMessage = original.LogMessage;
                newNode.Comment = original.Comment;
                // Don't copy parent/children relationships

                _tree.AddNode(newNode);
                CreateNodeView(newNode);

                if (_nodeViews.TryGetValue(newNode.Id, out var newNodeView))
                {
                    AddToSelection(newNodeView);
                }
            }

            EditorUtility.SetDirty(_tree);
        }
    }
}
