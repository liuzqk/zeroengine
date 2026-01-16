namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 顺序节点：按顺序执行子节点，全部成功则成功，任一失败则失败
    /// </summary>
    public class Sequence : BTComposite
    {
        /// <summary>
        /// 创建顺序节点
        /// </summary>
        public Sequence()
        {
            Name = "Sequence";
        }

        /// <inheritdoc/>
        protected override NodeState OnExecute(BTContext context)
        {
            // 检查中断条件
            if (_currentChildIndex > 0)
            {
                CheckAbortConditions(context, _currentChildIndex);
            }

            while (_currentChildIndex < _children.Count)
            {
                var child = _children[_currentChildIndex];
                var state = child.Execute(context);

                switch (state)
                {
                    case NodeState.Running:
                        return NodeState.Running;
                    case NodeState.Failure:
                        return NodeState.Failure;
                    case NodeState.Success:
                        _currentChildIndex++;
                        break;
                }
            }

            return NodeState.Success;
        }
    }
}
