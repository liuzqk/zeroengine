namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 反转节点：反转子节点的结果（Success↔Failure）
    /// </summary>
    public class Inverter : BTDecorator
    {
        /// <summary>
        /// 创建反转节点
        /// </summary>
        public Inverter()
        {
            Name = "Inverter";
        }

        /// <inheritdoc/>
        protected override NodeState OnExecute(BTContext context)
        {
            if (_child == null) return NodeState.Failure;

            var state = _child.Execute(context);

            return state switch
            {
                NodeState.Success => NodeState.Failure,
                NodeState.Failure => NodeState.Success,
                _ => state
            };
        }
    }
}
