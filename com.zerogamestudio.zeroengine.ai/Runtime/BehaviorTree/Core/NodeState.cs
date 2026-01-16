namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 行为树节点执行状态
    /// </summary>
    public enum NodeState
    {
        /// <summary>节点正在执行中，需要下一帧继续</summary>
        Running,
        /// <summary>节点执行成功</summary>
        Success,
        /// <summary>节点执行失败</summary>
        Failure
    }
}
