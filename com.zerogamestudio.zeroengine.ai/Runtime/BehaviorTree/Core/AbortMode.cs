namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 条件中断模式
    /// </summary>
    [System.Flags]
    public enum AbortMode
    {
        /// <summary>不中断</summary>
        None = 0,
        /// <summary>中断自身（低优先级节点）</summary>
        Self = 1,
        /// <summary>中断低优先级兄弟节点</summary>
        LowerPriority = 2,
        /// <summary>同时中断自身和低优先级节点</summary>
        Both = Self | LowerPriority
    }
}
