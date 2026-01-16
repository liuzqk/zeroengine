using System;

namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 动作节点：执行一个委托并返回结果
    /// </summary>
    public class ActionNode : BTLeaf
    {
        private readonly Func<BTContext, NodeState> _action;

        /// <summary>
        /// 创建动作节点
        /// </summary>
        /// <param name="action">执行的动作委托</param>
        public ActionNode(Func<BTContext, NodeState> action)
        {
            _action = action;
        }

        /// <summary>
        /// 创建动作节点并设置名称
        /// </summary>
        /// <param name="name">节点名称</param>
        /// <param name="action">执行的动作委托</param>
        public ActionNode(string name, Func<BTContext, NodeState> action) : this(action)
        {
            Name = name;
        }

        /// <inheritdoc/>
        protected override NodeState OnExecute(BTContext context)
        {
            return _action?.Invoke(context) ?? NodeState.Failure;
        }
    }
}
