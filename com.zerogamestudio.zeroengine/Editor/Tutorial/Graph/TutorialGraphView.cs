using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ZeroEngine.Tutorial;

namespace ZeroEngine.Editor.Tutorial
{
    /// <summary>
    /// GraphView for visual tutorial sequence editing (v1.14.0+)
    /// </summary>
    public class TutorialGraphView : GraphView
    {
        private readonly TutorialGraphEditorWindow _window;
        private TutorialSequenceSO _sequence;
        private readonly Dictionary<int, TutorialGraphNode> _nodeViews = new();

        // Node colors
        public static readonly Color StartNodeColor = new Color(0.2f, 0.8f, 0.2f);
        public static readonly Color EndNodeColor = new Color(0.8f, 0.2f, 0.2f);
        public static readonly Color DialogueNodeColor = new Color(0.3f, 0.5f, 0.9f);
        public static readonly Color HighlightNodeColor = new Color(0.9f, 0.7f, 0.2f);
        public static readonly Color WaitNodeColor = new Color(0.7f, 0.4f, 0.9f);
        public static readonly Color MoveNodeColor = new Color(0.4f, 0.7f, 0.7f);
        public static readonly Color DelayNodeColor = new Color(0.6f, 0.6f, 0.6f);
        public static readonly Color CompositeNodeColor = new Color(0.9f, 0.5f, 0.3f);

        public TutorialGraphView(TutorialGraphEditorWindow window)
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
                "Packages/com.zerogamestudio.zeroengine/Editor/Tutorial/Graph/TutorialGraphStyles.uss");
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }

            // Keyboard shortcuts
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        public void LoadSequence(TutorialSequenceSO sequence)
        {
            _sequence = sequence;

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

            if (_sequence == null) return;

            // Create start node
            var startNode = new StartStepNode();
            startNode.SetPosition(new Rect(100, 200, 0, 0));
            _nodeViews[-1] = startNode;
            AddElement(startNode);

            // Create step nodes
            for (int i = 0; i < _sequence.Steps.Count; i++)
            {
                var step = _sequence.Steps[i];
                if (step == null) continue;

                CreateStepNode(step, i);
            }

            // Create end node
            var endNode = new EndStepNode();
            float endX = 100 + (_sequence.Steps.Count + 1) * 250;
            endNode.SetPosition(new Rect(endX, 200, 0, 0));
            _nodeViews[-2] = endNode;
            AddElement(endNode);

            // Create edges (linear flow)
            CreateSequentialEdges();

            // Frame all content
            schedule.Execute(() =>
            {
                FrameAll();
            });
        }

        private void CreateStepNode(TutorialStep step, int index)
        {
            TutorialGraphNode nodeView = step switch
            {
                DialogueStep dialogueStep => new DialogueStepNode(dialogueStep, index),
                HighlightStep highlightStep => new HighlightStepNode(highlightStep, index),
                WaitInputStep waitInput => new WaitInputStepNode(waitInput, index),
                WaitInteractionStep waitInteraction => new WaitInteractionStepNode(waitInteraction, index),
                WaitEventStep waitEvent => new WaitEventStepNode(waitEvent, index),
                MoveToStep moveStep => new MoveToStepNode(moveStep, index),
                DelayStep delayStep => new DelayStepNode(delayStep, index),
                CallbackStep callbackStep => new CallbackStepNode(callbackStep, index),
                CompositeStep compositeStep => new CompositeStepNode(compositeStep, index),
                _ => new GenericStepNode(step, index)
            };

            // Position based on index
            float x = 100 + (index + 1) * 250;
            float y = step.EditorPosition.y != 0 ? step.EditorPosition.y : 200;

            if (step.EditorPosition != Vector2.zero)
            {
                nodeView.SetPosition(new Rect(step.EditorPosition, Vector2.zero));
            }
            else
            {
                nodeView.SetPosition(new Rect(x, y, 0, 0));
            }

            _nodeViews[index] = nodeView;
            AddElement(nodeView);

            // Selection callback
            nodeView.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    _window.SelectStep(step);
                }
            });
        }

        private void CreateSequentialEdges()
        {
            // Connect start to first step
            if (_nodeViews.TryGetValue(-1, out var startNode) && _nodeViews.Count > 2)
            {
                if (_nodeViews.TryGetValue(0, out var firstStep))
                {
                    var startOutput = startNode.GetOutputPorts().FirstOrDefault();
                    var firstInput = firstStep.GetInputPort();
                    if (startOutput != null && firstInput != null)
                    {
                        var edge = startOutput.ConnectTo(firstInput);
                        AddElement(edge);
                    }
                }
            }

            // Connect steps sequentially
            for (int i = 0; i < _sequence.Steps.Count - 1; i++)
            {
                if (_nodeViews.TryGetValue(i, out var currentNode) &&
                    _nodeViews.TryGetValue(i + 1, out var nextNode))
                {
                    var currentOutput = currentNode.GetOutputPorts().FirstOrDefault();
                    var nextInput = nextNode.GetInputPort();
                    if (currentOutput != null && nextInput != null)
                    {
                        var edge = currentOutput.ConnectTo(nextInput);
                        AddElement(edge);
                    }
                }
            }

            // Connect last step to end
            if (_nodeViews.TryGetValue(-2, out var endNode) && _sequence.Steps.Count > 0)
            {
                int lastIndex = _sequence.Steps.Count - 1;
                if (_nodeViews.TryGetValue(lastIndex, out var lastStep))
                {
                    var lastOutput = lastStep.GetOutputPorts().FirstOrDefault();
                    var endInput = endNode.GetInputPort();
                    if (lastOutput != null && endInput != null)
                    {
                        var edge = lastOutput.ConnectTo(endInput);
                        AddElement(edge);
                    }
                }
            }
        }

        public void CreateStep(Type stepType)
        {
            if (_sequence == null) return;

            Undo.RecordObject(_sequence, "Create Tutorial Step");

            var step = Activator.CreateInstance(stepType) as TutorialStep;
            if (step == null) return;

            // Generate unique ID
            step.StepId = GenerateUniqueStepId(stepType.Name);

            // Position in center of view
            var viewCenter = contentViewContainer.WorldToLocal(
                new Vector2(layout.width / 2, layout.height / 2));
            step.EditorPosition = viewCenter;

            _sequence.Steps.Add(step);
            EditorUtility.SetDirty(_sequence);

            CreateStepNode(step, _sequence.Steps.Count - 1);

            // Refresh edges
            RefreshEdges();
        }

        private void RefreshEdges()
        {
            // Remove all edges
            foreach (var edge in edges.ToList())
            {
                RemoveElement(edge);
            }

            // Recreate edges
            CreateSequentialEdges();
        }

        private string GenerateUniqueStepId(string prefix)
        {
            int counter = 1;
            string id;
            do
            {
                id = $"{prefix}_{counter:D3}";
                counter++;
            } while (_sequence.Steps.Any(s => s?.StepId == id));

            return id;
        }

        public void SaveGraphPositions()
        {
            if (_sequence == null) return;

            bool changed = false;
            foreach (var kvp in _nodeViews)
            {
                if (kvp.Key < 0) continue; // Skip start/end nodes

                if (kvp.Key < _sequence.Steps.Count)
                {
                    var step = _sequence.Steps[kvp.Key];
                    if (step != null)
                    {
                        var pos = kvp.Value.GetPosition().position;
                        if (step.EditorPosition != pos)
                        {
                            step.EditorPosition = pos;
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(_sequence);
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_sequence == null) return change;

            // Handle node removal
            if (change.elementsToRemove != null)
            {
                Undo.RecordObject(_sequence, "Delete Tutorial Step");

                foreach (var element in change.elementsToRemove)
                {
                    if (element is TutorialGraphNode nodeView && nodeView.StepIndex >= 0)
                    {
                        if (nodeView.StepIndex < _sequence.Steps.Count)
                        {
                            _sequence.Steps.RemoveAt(nodeView.StepIndex);
                        }
                        _nodeViews.Remove(nodeView.StepIndex);
                    }
                }

                // Rebuild node indices
                RebuildNodeIndices();
                EditorUtility.SetDirty(_sequence);
            }

            // Handle moves
            if (change.movedElements != null)
            {
                foreach (var element in change.movedElements)
                {
                    if (element is TutorialGraphNode nodeView && nodeView.StepIndex >= 0)
                    {
                        if (nodeView.StepIndex < _sequence.Steps.Count)
                        {
                            var step = _sequence.Steps[nodeView.StepIndex];
                            if (step != null)
                            {
                                step.EditorPosition = nodeView.GetPosition().position;
                            }
                        }
                    }
                }

                EditorUtility.SetDirty(_sequence);
            }

            return change;
        }

        private void RebuildNodeIndices()
        {
            // Clear and rebuild
            var oldNodes = new List<TutorialGraphNode>(_nodeViews.Values);

            foreach (var node in oldNodes)
            {
                if (node.StepIndex >= 0)
                {
                    RemoveElement(node);
                }
            }

            _nodeViews.Clear();

            // Re-add start/end
            var startNode = oldNodes.FirstOrDefault(n => n.StepIndex == -1);
            var endNode = oldNodes.FirstOrDefault(n => n.StepIndex == -2);

            if (startNode != null)
            {
                _nodeViews[-1] = startNode;
            }
            if (endNode != null)
            {
                _nodeViews[-2] = endNode;
            }

            // Recreate step nodes
            for (int i = 0; i < _sequence.Steps.Count; i++)
            {
                var step = _sequence.Steps[i];
                if (step != null)
                {
                    CreateStepNode(step, i);
                }
            }

            RefreshEdges();
        }

        private void OnNodeCreationRequest(NodeCreationContext context)
        {
            ShowAddStepMenu(context.screenMousePosition);
        }

        private void ShowAddStepMenu(Vector2 position)
        {
            if (_sequence == null) return;

            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Dialogue"), false, () => CreateStepAtPosition(typeof(DialogueStep), position));
            menu.AddItem(new GUIContent("Highlight"), false, () => CreateStepAtPosition(typeof(HighlightStep), position));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Wait Input"), false, () => CreateStepAtPosition(typeof(WaitInputStep), position));
            menu.AddItem(new GUIContent("Wait Interaction"), false, () => CreateStepAtPosition(typeof(WaitInteractionStep), position));
            menu.AddItem(new GUIContent("Wait Event"), false, () => CreateStepAtPosition(typeof(WaitEventStep), position));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Move To"), false, () => CreateStepAtPosition(typeof(MoveToStep), position));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delay"), false, () => CreateStepAtPosition(typeof(DelayStep), position));
            menu.AddItem(new GUIContent("Callback"), false, () => CreateStepAtPosition(typeof(CallbackStep), position));
            menu.AddItem(new GUIContent("Composite"), false, () => CreateStepAtPosition(typeof(CompositeStep), position));

            menu.ShowAsContext();
        }

        private void CreateStepAtPosition(Type stepType, Vector2 screenPosition)
        {
            if (_sequence == null) return;

            Undo.RecordObject(_sequence, "Create Tutorial Step");

            var step = Activator.CreateInstance(stepType) as TutorialStep;
            if (step == null) return;

            step.StepId = GenerateUniqueStepId(stepType.Name);

            var localPos = contentViewContainer.WorldToLocal(screenPosition);
            step.EditorPosition = localPos;

            _sequence.Steps.Add(step);
            EditorUtility.SetDirty(_sequence);

            CreateStepNode(step, _sequence.Steps.Count - 1);
            RefreshEdges();
        }

        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (_sequence == null)
            {
                evt.menu.AppendAction("No sequence loaded", null, DropdownMenuAction.Status.Disabled);
                return;
            }

            var localPos = contentViewContainer.WorldToLocal(evt.mousePosition);

            evt.menu.AppendAction("Add/Dialogue Step", _ => CreateStepAt(typeof(DialogueStep), localPos));
            evt.menu.AppendAction("Add/Highlight Step", _ => CreateStepAt(typeof(HighlightStep), localPos));
            evt.menu.AppendSeparator("Add/");
            evt.menu.AppendAction("Add/Wait Input", _ => CreateStepAt(typeof(WaitInputStep), localPos));
            evt.menu.AppendAction("Add/Wait Interaction", _ => CreateStepAt(typeof(WaitInteractionStep), localPos));
            evt.menu.AppendAction("Add/Wait Event", _ => CreateStepAt(typeof(WaitEventStep), localPos));
            evt.menu.AppendSeparator("Add/");
            evt.menu.AppendAction("Add/Move To", _ => CreateStepAt(typeof(MoveToStep), localPos));
            evt.menu.AppendSeparator("Add/");
            evt.menu.AppendAction("Add/Delay", _ => CreateStepAt(typeof(DelayStep), localPos));
            evt.menu.AppendAction("Add/Callback", _ => CreateStepAt(typeof(CallbackStep), localPos));
            evt.menu.AppendAction("Add/Composite", _ => CreateStepAt(typeof(CompositeStep), localPos));
        }

        private void CreateStepAt(Type stepType, Vector2 position)
        {
            if (_sequence == null) return;

            Undo.RecordObject(_sequence, "Create Tutorial Step");

            var step = Activator.CreateInstance(stepType) as TutorialStep;
            if (step == null) return;

            step.StepId = GenerateUniqueStepId(stepType.Name);
            step.EditorPosition = position;

            _sequence.Steps.Add(step);
            EditorUtility.SetDirty(_sequence);

            CreateStepNode(step, _sequence.Steps.Count - 1);
            RefreshEdges();
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
            if (_sequence == null) return;

            var selectedNodes = selection.OfType<TutorialGraphNode>()
                .Where(n => n.StepIndex >= 0 && n.StepIndex < _sequence.Steps.Count)
                .ToList();

            if (selectedNodes.Count == 0) return;

            Undo.RecordObject(_sequence, "Duplicate Tutorial Steps");

            ClearSelection();

            foreach (var nodeView in selectedNodes)
            {
                var originalStep = _sequence.Steps[nodeView.StepIndex];
                if (originalStep == null) continue;

                // Deep copy using JSON serialization
                var json = JsonUtility.ToJson(originalStep);
                var newStep = Activator.CreateInstance(originalStep.GetType()) as TutorialStep;
                JsonUtility.FromJsonOverwrite(json, newStep);

                newStep.StepId = GenerateUniqueStepId(originalStep.GetType().Name);
                newStep.EditorPosition = originalStep.EditorPosition + new Vector2(50, 50);

                _sequence.Steps.Add(newStep);
                CreateStepNode(newStep, _sequence.Steps.Count - 1);
            }

            EditorUtility.SetDirty(_sequence);
            RefreshEdges();
        }
    }
}
