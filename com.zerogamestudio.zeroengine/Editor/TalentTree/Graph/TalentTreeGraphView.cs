using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ZeroEngine.TalentTree;

namespace ZeroEngine.Editor.TalentTree
{
    /// <summary>
    /// 天赋树 GraphView 组件
    /// </summary>
    public class TalentTreeGraphView : GraphView
    {
        private TalentTreeSO _currentTree;
        private TalentTreeEditorWindow _editorWindow;
        private readonly Dictionary<string, TalentGraphNode> _nodeViews = new();

        public event Action<TalentNodeSO> OnNodeSelected;

        public TalentTreeGraphView(TalentTreeEditorWindow window)
        {
            _editorWindow = window;

            // 设置 GraphView 功能
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // 添加网格背景
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // 注册回调
            graphViewChanged = OnGraphViewChanged;
        }

        public void LoadTree(TalentTreeSO tree)
        {
            _currentTree = tree;
            RefreshView();
        }

        public void RefreshView()
        {
            // 清除现有节点和边
            DeleteElements(graphElements.ToList());
            _nodeViews.Clear();

            if (_currentTree == null) return;

            // 创建节点
            foreach (var node in _currentTree.Nodes)
            {
                if (node != null)
                {
                    CreateNodeView(node);
                }
            }

            // 创建连接
            foreach (var conn in _currentTree.Connections)
            {
                CreateEdge(conn);
            }
        }

        private void CreateNodeView(TalentNodeSO node)
        {
            var nodeView = new TalentGraphNode(node);
            nodeView.SetPosition(new Rect(node.EditorPosition, new Vector2(200, 150)));

            nodeView.NodeSelected += () => OnNodeSelected?.Invoke(node);

            AddElement(nodeView);
            _nodeViews[node.NodeId] = nodeView;
        }

        private void CreateEdge(TalentConnection connection)
        {
            if (!_nodeViews.TryGetValue(connection.FromNodeId, out var fromNode)) return;
            if (!_nodeViews.TryGetValue(connection.ToNodeId, out var toNode)) return;

            var outputPort = fromNode.GetOutputPort();
            var inputPort = toNode.GetInputPort();

            if (outputPort == null || inputPort == null) return;

            var edge = outputPort.ConnectTo(inputPort);
            AddElement(edge);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange changes)
        {
            if (_currentTree == null) return changes;

            // 处理删除
            if (changes.elementsToRemove != null)
            {
                foreach (var element in changes.elementsToRemove)
                {
                    if (element is TalentGraphNode nodeView)
                    {
                        DeleteNode(nodeView.NodeData);
                    }
                    else if (element is Edge edge)
                    {
                        DeleteEdge(edge);
                    }
                }
            }

            // 处理新连接
            if (changes.edgesToCreate != null)
            {
                foreach (var edge in changes.edgesToCreate)
                {
                    CreateConnection(edge);
                }
            }

            // 处理移动
            if (changes.movedElements != null)
            {
                foreach (var element in changes.movedElements)
                {
                    if (element is TalentGraphNode nodeView)
                    {
                        nodeView.NodeData.EditorPosition = nodeView.GetPosition().position;
                        EditorUtility.SetDirty(nodeView.NodeData);
                    }
                }
            }

            return changes;
        }

        private void DeleteNode(TalentNodeSO node)
        {
            if (node == null) return;

            // 移除相关连接
            _currentTree.Connections.RemoveAll(c => c.FromNodeId == node.NodeId || c.ToNodeId == node.NodeId);

            // 从树中移除
            _currentTree.Nodes.Remove(node);
            _nodeViews.Remove(node.NodeId);

            // 删除子资产
            AssetDatabase.RemoveObjectFromAsset(node);
            UnityEngine.Object.DestroyImmediate(node, true);

            EditorUtility.SetDirty(_currentTree);
        }

        private void DeleteEdge(Edge edge)
        {
            if (edge.output?.node is TalentGraphNode fromNode &&
                edge.input?.node is TalentGraphNode toNode)
            {
                _currentTree.Connections.RemoveAll(c =>
                    c.FromNodeId == fromNode.NodeData.NodeId &&
                    c.ToNodeId == toNode.NodeData.NodeId);

                EditorUtility.SetDirty(_currentTree);
            }
        }

        private void CreateConnection(Edge edge)
        {
            if (edge.output?.node is TalentGraphNode fromNode &&
                edge.input?.node is TalentGraphNode toNode)
            {
                var conn = new TalentConnection
                {
                    FromNodeId = fromNode.NodeData.NodeId,
                    ToNodeId = toNode.NodeData.NodeId
                };

                // 避免重复
                if (!_currentTree.Connections.Any(c =>
                    c.FromNodeId == conn.FromNodeId && c.ToNodeId == conn.ToNodeId))
                {
                    _currentTree.Connections.Add(conn);

                    // 更新前置依赖
                    if (!toNode.NodeData.Prerequisites.Contains(fromNode.NodeData))
                    {
                        toNode.NodeData.Prerequisites.Add(fromNode.NodeData);
                        EditorUtility.SetDirty(toNode.NodeData);
                    }

                    EditorUtility.SetDirty(_currentTree);
                }
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort != port &&
                    startPort.node != port.node &&
                    startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });

            return compatiblePorts;
        }

        public Vector2 GetCenterPosition()
        {
            var viewCenter = contentViewContainer.WorldToLocal(
                new Vector2(layout.width / 2, layout.height / 2));
            return viewCenter;
        }

        public void SavePositions()
        {
            if (_currentTree == null) return;

            foreach (var kvp in _nodeViews)
            {
                var node = _currentTree.GetNode(kvp.Key);
                if (node != null)
                {
                    node.EditorPosition = kvp.Value.GetPosition().position;
                    EditorUtility.SetDirty(node);
                }
            }
        }
    }
}
