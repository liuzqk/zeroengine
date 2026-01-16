namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 始终成功节点：无论子节点结果如何，都返回成功
    /// </summary>
    public class AlwaysSucceed : BTDecorator
    {
        /// <summary>
        /// 创建始终成功节点
        /// </summary>
        public AlwaysSucceed()
        {
            Name = "AlwaysSucceed";
        }

        /// <inheritdoc/>
        protected override NodeState OnExecute(BTContext context)
        {
            if (_child == null) return NodeState.Success;

            var state = _child.Execute(context);
            return state == NodeState.Running ? NodeState.Running : NodeState.Success;
        }
    }
}
