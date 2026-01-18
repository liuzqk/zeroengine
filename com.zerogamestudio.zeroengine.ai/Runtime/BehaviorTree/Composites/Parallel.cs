using System.Collections.Generic;

namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 并行执行策略
    /// </summary>
    public enum ParallelPolicy
    {
        /// <summary>任一子节点满足条件即可</summary>
        RequireOne,
        /// <summary>所有子节点必须满足条件</summary>
        RequireAll
    }

    /// <summary>
    /// 并行节点：同时执行所有子节点
    /// </summary>
    public class Parallel : BTComposite
    {
        private readonly ParallelPolicy _successPolicy;
        private readonly ParallelPolicy _failurePolicy;
        private readonly List<NodeState> _childStates = new List<NodeState>();

        /// <summary>
        /// 创建并行节点
        /// </summary>
        /// <param name="successPolicy">成功策略，默认需要全部成功</param>
        /// <param name="failurePolicy">失败策略，默认任一失败即失败</param>
        public Parallel(
            ParallelPolicy successPolicy = ParallelPolicy.RequireAll,
            ParallelPolicy failurePolicy = ParallelPolicy.RequireOne)
        {
            _successPolicy = successPolicy;
            _failurePolicy = failurePolicy;
            Name = "Parallel";
        }

        /// <summary>
        /// 添加子节点（同步扩容状态列表，避免 GC）
        /// </summary>
        public new BTComposite AddChild(IBTNode child)
        {
            base.AddChild(child);
            _childStates.Add(NodeState.Running);
            return this;
        }

        /// <summary>
        /// 添加多个子节点
        /// </summary>
        public new BTComposite AddChildren(params IBTNode[] children)
        {
            foreach (var child in children)
            {
                AddChild(child);
            }
            return this;
        }

        /// <inheritdoc/>
        protected override void OnStart(BTContext context)
        {
            base.OnStart(context);
            // 确保 _childStates 与 _children 大小同步
            // 修复：当通过基类 AddChild 添加子节点时，_childStates 可能未同步扩容
            while (_childStates.Count < _children.Count)
            {
                _childStates.Add(NodeState.Running);
            }
            // 重置所有状态
            for (int i = 0; i < _childStates.Count; i++)
            {
                _childStates[i] = NodeState.Running;
            }
        }

        /// <inheritdoc/>
        protected override NodeState OnExecute(BTContext context)
        {
            int successCount = 0;
            int failureCount = 0;
            int runningCount = 0;

            for (int i = 0; i < _children.Count; i++)
            {
                // 只执行仍在运行的节点
                if (_childStates[i] == NodeState.Running)
                {
                    _childStates[i] = _children[i].Execute(context);
                }

                switch (_childStates[i])
                {
                    case NodeState.Success:
                        successCount++;
                        break;
                    case NodeState.Failure:
                        failureCount++;
                        break;
                    case NodeState.Running:
                        runningCount++;
                        break;
                }
            }

            // 检查失败条件
            if (_failurePolicy == ParallelPolicy.RequireOne && failureCount > 0)
            {
                AbortRunningChildren();
                return NodeState.Failure;
            }
            if (_failurePolicy == ParallelPolicy.RequireAll && failureCount == _children.Count)
            {
                return NodeState.Failure;
            }

            // 检查成功条件
            if (_successPolicy == ParallelPolicy.RequireOne && successCount > 0)
            {
                AbortRunningChildren();
                return NodeState.Success;
            }
            if (_successPolicy == ParallelPolicy.RequireAll && successCount == _children.Count)
            {
                return NodeState.Success;
            }

            return runningCount > 0 ? NodeState.Running : NodeState.Failure;
        }

        private void AbortRunningChildren()
        {
            for (int i = 0; i < _children.Count; i++)
            {
                if (_childStates[i] == NodeState.Running)
                {
                    _children[i].Abort();
                }
            }
        }

        /// <inheritdoc/>
        public override void Abort()
        {
            AbortRunningChildren();
            base.Abort();
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            _childStates.Clear();
        }
    }
}
