using System;

namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 条件节点：根据条件决定是否执行子节点，支持条件中断
    /// </summary>
    public class Conditional : BTDecorator
    {
        private readonly Func<BTContext, bool> _condition;
        private readonly AbortMode _abortMode;

        /// <inheritdoc/>
        public override AbortMode AbortMode => _abortMode;

        /// <summary>
        /// 创建条件节点
        /// </summary>
        /// <param name="condition">条件委托</param>
        /// <param name="abortMode">中断模式</param>
        public Conditional(Func<BTContext, bool> condition, AbortMode abortMode = AbortMode.None)
        {
            _condition = condition;
            _abortMode = abortMode;
            Name = "Conditional";
        }

        /// <summary>
        /// 创建条件节点并设置名称
        /// </summary>
        /// <param name="name">节点名称</param>
        /// <param name="condition">条件委托</param>
        /// <param name="abortMode">中断模式</param>
        public Conditional(string name, Func<BTContext, bool> condition, AbortMode abortMode = AbortMode.None)
            : this(condition, abortMode)
        {
            Name = name;
        }

        /// <inheritdoc/>
        public override bool CheckCondition(BTContext context)
        {
            return _condition?.Invoke(context) ?? true;
        }

        /// <inheritdoc/>
        protected override NodeState OnExecute(BTContext context)
        {
            if (_child == null) return NodeState.Failure;

            if (!CheckCondition(context))
            {
                return NodeState.Failure;
            }

            return _child.Execute(context);
        }
    }
}
