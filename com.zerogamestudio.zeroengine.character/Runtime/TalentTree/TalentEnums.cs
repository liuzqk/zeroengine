namespace ZeroEngine.TalentTree
{
    /// <summary>
    /// 天赋节点类型
    /// </summary>
    public enum TalentNodeType
    {
        /// <summary>普通节点（属性加成）</summary>
        Normal,
        /// <summary>关键节点（重要技能/效果）</summary>
        Keystone,
        /// <summary>起始节点</summary>
        Start,
        /// <summary>分支节点</summary>
        Branch
    }

    /// <summary>
    /// 天赋事件类型
    /// </summary>
    public enum TalentEventType
    {
        NodeUnlocked,   // 节点解锁
        NodeLevelUp,    // 节点升级
        NodeReset,      // 节点重置
        TreeReset,      // 整棵树重置
        PointsChanged   // 点数变化
    }
}
