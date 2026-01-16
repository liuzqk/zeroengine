namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 装饰节点抽象基类，包装单个子节点并修改其行为
    /// </summary>
    public abstract class BTDecorator : BTNode
    {
        /// <summary>子节点</summary>
        protected IBTNode _child;

        /// <summary>子节点只读访问</summary>
        public IBTNode Child => _child;

        /// <summary>
        /// 设置子节点
        /// </summary>
        /// <param name="child">子节点</param>
        /// <returns>返回自身以支持链式调用</returns>
        public BTDecorator SetChild(IBTNode child)
        {
            _child = child;
            return this;
        }

        /// <inheritdoc/>
        public override void Abort()
        {
            _child?.Abort();
            base.Abort();
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            _child?.Reset();
        }

        /// <summary>
        /// 检查装饰器条件（用于中断评估）
        /// </summary>
        /// <param name="context">执行上下文</param>
        /// <returns>条件是否满足</returns>
        public virtual bool CheckCondition(BTContext context) => true;
    }
}
