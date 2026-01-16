using System;
using System.Collections.Generic;

namespace ZeroEngine.Dialog
{
    /// <summary>
    /// Execution context for dialog graphs.
    /// Contains state and variables during dialog execution.
    /// </summary>
    public class DialogGraphContext
    {
        /// <summary>
        /// The dialog graph being executed.
        /// </summary>
        public DialogGraphSO Graph { get; }

        /// <summary>
        /// Variable storage for this dialog session.
        /// </summary>
        public DialogVariables Variables { get; }

        /// <summary>
        /// Current node being executed.
        /// </summary>
        public DialogNode CurrentNode { get; set; }

        /// <summary>
        /// Last displayed line.
        /// </summary>
        public DialogLine LastLine { get; set; }

        /// <summary>
        /// End tag from EndNode (null if not ended).
        /// </summary>
        public string EndTag { get; set; }

        /// <summary>
        /// Callback currently waiting for.
        /// </summary>
        public string WaitingForCallback { get; set; }

        /// <summary>
        /// Pending choice targets for the current choice node.
        /// </summary>
        public List<string> PendingChoiceTargets { get; set; }

        /// <summary>
        /// Event fired when a callback is triggered.
        /// </summary>
        public event Action<string, string> OnCallback;

        /// <summary>
        /// History of visited node IDs (ordered).
        /// </summary>
        public List<string> VisitHistory { get; } = new(32);

        // O(1) lookup structures
        private readonly HashSet<string> _visitedNodes = new(32);
        private readonly Dictionary<string, int> _visitCounts = new(32);

        /// <summary>
        /// Check if a node has been visited. O(1) complexity.
        /// </summary>
        public bool HasVisited(string nodeId)
        {
            return !string.IsNullOrEmpty(nodeId) && _visitedNodes.Contains(nodeId);
        }

        /// <summary>
        /// Get visit count for a node. O(1) complexity.
        /// </summary>
        public int GetVisitCount(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return 0;
            return _visitCounts.TryGetValue(nodeId, out int count) ? count : 0;
        }

        public DialogGraphContext(DialogGraphSO graph)
        {
            Graph = graph;
            Variables = new DialogVariables();
        }

        public DialogGraphContext(DialogGraphSO graph, DialogVariables sharedVariables)
        {
            Graph = graph;
            Variables = sharedVariables ?? new DialogVariables();
        }

        /// <summary>
        /// Initialize the context with graph's default variables.
        /// </summary>
        public void Initialize()
        {
            if (Graph != null && Graph.DefaultVariables != null)
            {
                foreach (var variable in Graph.DefaultVariables)
                {
                    Variables.SetLocal(variable.Name, variable.GetValue());
                }
            }
        }

        /// <summary>
        /// Trigger an external callback.
        /// </summary>
        public void TriggerCallback(string callbackId, string parameter)
        {
            OnCallback?.Invoke(callbackId, parameter);
        }

        /// <summary>
        /// Notify that an external callback has completed.
        /// </summary>
        public void CompleteCallback(string callbackId)
        {
            if (WaitingForCallback == callbackId)
            {
                WaitingForCallback = null;
            }
        }

        /// <summary>
        /// Record a node visit.
        /// </summary>
        public void RecordVisit(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return;

            VisitHistory.Add(nodeId);
            _visitedNodes.Add(nodeId);

            if (_visitCounts.TryGetValue(nodeId, out int count))
            {
                _visitCounts[nodeId] = count + 1;
            }
            else
            {
                _visitCounts[nodeId] = 1;
            }
        }

        /// <summary>
        /// Reset the context for reuse.
        /// </summary>
        public void Reset()
        {
            CurrentNode = null;
            LastLine = default;
            EndTag = null;
            WaitingForCallback = null;
            PendingChoiceTargets = null;
            VisitHistory.Clear();
            _visitedNodes.Clear();
            _visitCounts.Clear();
            Variables.ClearLocal();
            Initialize();
        }
    }
}
