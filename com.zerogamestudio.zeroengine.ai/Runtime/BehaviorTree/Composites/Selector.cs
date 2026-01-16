namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 选择节点：按顺序尝试子节点，任一成功则成功，全部失败则失败
    /// </summary>
    public class Selector : BTComposite
    {
        /// <summary>
        /// 创建选择节点
        /// </summary>
        public Selector()
        {
            Name = "Selector";
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
                    case NodeState.Success:
                        return NodeState.Success;
                    case NodeState.Failure:
                        _currentChildIndex++;
                        break;
                }
            }

            return NodeState.Failure;
        }
    }
}
