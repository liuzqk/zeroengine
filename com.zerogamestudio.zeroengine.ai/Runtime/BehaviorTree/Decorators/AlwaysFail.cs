namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 始终失败节点：无论子节点结果如何，都返回失败
    /// </summary>
    public class AlwaysFail : BTDecorator
    {
        /// <summary>
        /// 创建始终失败节点
        /// </summary>
        public AlwaysFail()
        {
            Name = "AlwaysFail";
        }

        /// <inheritdoc/>
        protected override NodeState OnExecute(BTContext context)
        {
            if (_child == null) return NodeState.Failure;

            var state = _child.Execute(context);
            return state == NodeState.Running ? NodeState.Running : NodeState.Failure;
        }
    }
}
