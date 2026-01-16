namespace ZeroEngine.Performance
{
    /// <summary>
    /// 性能统计数据结构
    /// </summary>
    public struct PerformanceStats
    {
        /// <summary>List 池统计</summary>
        public PoolStats ListPoolStats;

        /// <summary>Dictionary 池统计</summary>
        public PoolStats DictionaryPoolStats;

        /// <summary>StringBuilder 池统计</summary>
        public PoolStats StringBuilderPoolStats;

        /// <summary>缓存命中率</summary>
        public float CacheHitRate;

        /// <summary>缓存条目数</summary>
        public int CacheEntryCount;
    }

    /// <summary>
    /// 对象池统计
    /// </summary>
    public struct PoolStats
    {
        /// <summary>池中可用对象数</summary>
        public int PooledCount;

        /// <summary>活跃使用中的对象数</summary>
        public int ActiveCount;

        /// <summary>总创建数</summary>
        public int TotalCreated;

        /// <summary>获取次数</summary>
        public int GetCount;

        /// <summary>归还次数</summary>
        public int ReturnCount;

        /// <summary>池命中率</summary>
        public float HitRate => GetCount > 0 ? (float)ReturnCount / GetCount : 1f;
    }
}
