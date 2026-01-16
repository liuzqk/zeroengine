using System;
using System.Collections.Generic;
using ZeroEngine.Performance;
using ZeroEngine.Performance.Collections;

namespace ZeroEngine.Debugging
{
    /// <summary>
    /// 对象池性能分析模块
    /// </summary>
    public class PoolProfiler : IDebugModule
    {
        public const string MODULE_NAME = "ObjectPools";

        private readonly List<PoolDebugData> _poolData = new List<PoolDebugData>();
        private PerformanceStats _lastStats;
        private float _cacheHitRate;

        private bool _isEnabled = true;

        public string ModuleName => MODULE_NAME;
        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }

        /// <summary>获取最新的性能统计</summary>
        public PerformanceStats LastStats => _lastStats;

        /// <summary>缓存命中率</summary>
        public float CacheHitRate => _cacheHitRate;

        public void Update()
        {
            _poolData.Clear();

            // 获取全局统计
            _lastStats = ZeroGC.GetStats();
            _cacheHitRate = _lastStats.CacheHitRate;

            // List Pool 统计
            _poolData.Add(CreatePoolData("ListPool", _lastStats.ListPoolStats));

            // Dictionary Pool 统计
            _poolData.Add(CreatePoolData("DictionaryPool", _lastStats.DictionaryPoolStats));

            // StringBuilder Pool 统计
            _poolData.Add(CreatePoolData("StringBuilderPool", _lastStats.StringBuilderPoolStats));

            // 添加特定类型的 List Pool 统计
            AddTypedPoolStats();
        }

        private PoolDebugData CreatePoolData(string name, PoolStats stats)
        {
            return new PoolDebugData
            {
                PoolName = name,
                PooledCount = stats.PooledCount,
                ActiveCount = stats.ActiveCount,
                TotalCreated = stats.TotalCreated,
                GetCount = stats.GetCount,
                ReturnCount = stats.ReturnCount,
                HitRate = stats.HitRate
            };
        }

        private void AddTypedPoolStats()
        {
            // int List
            var intStats = ListPool<int>.GetStats();
            if (intStats.TotalCreated > 0)
            {
                _poolData.Add(CreatePoolData("ListPool<int>", intStats));
            }

            // float List
            var floatStats = ListPool<float>.GetStats();
            if (floatStats.TotalCreated > 0)
            {
                _poolData.Add(CreatePoolData("ListPool<float>", floatStats));
            }

            // string List
            var stringStats = ListPool<string>.GetStats();
            if (stringStats.TotalCreated > 0)
            {
                _poolData.Add(CreatePoolData("ListPool<string>", stringStats));
            }

            // object List
            var objectStats = ListPool<object>.GetStats();
            if (objectStats.TotalCreated > 0)
            {
                _poolData.Add(CreatePoolData("ListPool<object>", objectStats));
            }
        }

        public string GetSummary()
        {
            int totalPooled = 0;
            int totalActive = 0;
            int totalCreated = 0;

            foreach (var data in _poolData)
            {
                totalPooled += data.PooledCount;
                totalActive += data.ActiveCount;
                totalCreated += data.TotalCreated;
            }

            return $"Pooled: {totalPooled}, Active: {totalActive}, Created: {totalCreated}, Cache: {_cacheHitRate:P0}";
        }

        public IEnumerable<DebugEntry> GetEntries()
        {
            foreach (var data in _poolData)
            {
                var type = data.HitRate < 0.5f ? DebugEntryType.Warning :
                          data.HitRate > 0.9f ? DebugEntryType.Success :
                          DebugEntryType.Info;

                yield return new DebugEntry(data.PoolName, data.ToString(), type);
            }

            // 缓存信息
            yield return new DebugEntry("FormattedValueCache",
                $"{_lastStats.CacheEntryCount} entries, {_cacheHitRate:P0} hit rate",
                _cacheHitRate > 0.7f ? DebugEntryType.Success : DebugEntryType.Warning);
        }

        /// <summary>
        /// 获取池数据列表
        /// </summary>
        public IReadOnlyList<PoolDebugData> GetPoolData() => _poolData;

        public void Clear()
        {
            _poolData.Clear();
        }
    }
}
