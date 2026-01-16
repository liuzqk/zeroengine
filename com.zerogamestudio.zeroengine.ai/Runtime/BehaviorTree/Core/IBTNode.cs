namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 行为树节点基础接口
    /// </summary>
    public interface IBTNode
    {
        /// <summary>节点名称（用于调试）</summary>
        string Name { get; set; }

        /// <summary>节点的中断模式</summary>
        AbortMode AbortMode { get; }

        /// <summary>当前节点状态</summary>
        NodeState CurrentState { get; }

        /// <summary>
        /// 执行节点逻辑
        /// </summary>
        /// <param name="context">执行上下文</param>
        /// <returns>节点执行状态</returns>
        NodeState Execute(BTContext context);

        /// <summary>
        /// 中断节点执行
        /// </summary>
        void Abort();

        /// <summary>
        /// 重置节点状态
        /// </summary>
        void Reset();
    }
}
