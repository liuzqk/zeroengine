namespace ZeroEngine.Combat
{
    /// <summary>
    /// 仇恨来源接口 — 任何能产生仇恨的实体实现此接口
    /// </summary>
    public interface IThreatSource
    {
        /// <summary>
        /// 唯一标识符（用作 ThreatTable 的 key）
        /// </summary>
        string SourceId { get; }

        /// <summary>
        /// 是否存活（死亡后仇恨会被清理或转移）
        /// </summary>
        bool IsAlive { get; }
    }
}
