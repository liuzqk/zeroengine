using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Performance.Collections;

namespace ZeroEngine.Performance.Caching
{
    /// <summary>
    /// 格式化值缓存，用于 BindableProperty 的 UI 显示
    /// 避免重复格式化同一值产生的 GC 分配
    /// </summary>
    public sealed class FormattedValueCache
    {
        private struct CacheEntry
        {
            public string FormattedString;
            public float Timestamp;
            public int ValueHash;
            public string FormatHash;
        }

        private readonly Dictionary<int, CacheEntry> _cache;
        private readonly float _ttlSeconds;
        private readonly int _maxEntries;
        private float _lastCleanupTime;

        // 统计
        private int _hitCount;
        private int _missCount;

        /// <summary>
        /// 创建格式化值缓存
        /// </summary>
        /// <param name="maxEntries">最大缓存条目数</param>
        /// <param name="ttlSeconds">缓存条目过期时间（秒）</param>
        public FormattedValueCache(int maxEntries = 1000, float ttlSeconds = 60f)
        {
            _maxEntries = maxEntries;
            _ttlSeconds = ttlSeconds;
            _cache = new Dictionary<int, CacheEntry>(maxEntries);
            _lastCleanupTime = Time.unscaledTime;
        }

        /// <summary>
        /// 获取或创建格式化字符串
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">要格式化的值</param>
        /// <param name="formatter">格式化函数</param>
        /// <returns>格式化后的字符串</returns>
        public string GetOrCreate<T>(string key, T value, Func<T, string> formatter)
        {
            // 计算组合哈希
            int keyHash = key?.GetHashCode() ?? 0;
            int valueHash = value?.GetHashCode() ?? 0;
            int combinedHash = HashCode.Combine(keyHash, valueHash);

            float now = Time.unscaledTime;

            // 检查缓存
            if (_cache.TryGetValue(combinedHash, out var entry))
            {
                // 验证值和格式是否匹配，且未过期
                if (entry.ValueHash == valueHash &&
                    entry.FormatHash == key &&
                    now - entry.Timestamp < _ttlSeconds)
                {
                    _hitCount++;
                    return entry.FormattedString;
                }
            }

            // 缓存未命中，执行格式化
            _missCount++;
            string formatted = formatter(value);

            // 定期清理
            if (now - _lastCleanupTime > _ttlSeconds)
            {
                Cleanup();
                _lastCleanupTime = now;
            }

            // 容量检查
            if (_cache.Count >= _maxEntries)
            {
                Cleanup();
                if (_cache.Count >= _maxEntries)
                {
                    // 仍然满，强制清空一半
                    ForceEvict();
                }
            }

            // 存入缓存
            _cache[combinedHash] = new CacheEntry
            {
                FormattedString = formatted,
                Timestamp = now,
                ValueHash = valueHash,
                FormatHash = key
            };

            return formatted;
        }

        /// <summary>
        /// 获取或创建格式化字符串（使用格式字符串）
        /// </summary>
        public string GetOrCreate<T>(string key, T value, string format)
        {
            return GetOrCreate(key, value, v => string.Format(format, v));
        }

        /// <summary>
        /// 使指定前缀的缓存失效
        /// </summary>
        /// <param name="keyPrefix">键前缀</param>
        public void Invalidate(string keyPrefix)
        {
            if (string.IsNullOrEmpty(keyPrefix)) return;

            // 使用 ListPool 避免 GC 分配
            var keysToRemove = ListPool<int>.Get();
            try
            {
                foreach (var kvp in _cache)
                {
                    if (kvp.Value.FormatHash != null && kvp.Value.FormatHash.StartsWith(keyPrefix))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                for (int i = 0; i < keysToRemove.Count; i++)
                {
                    _cache.Remove(keysToRemove[i]);
                }
            }
            finally
            {
                ListPool<int>.Return(keysToRemove);
            }
        }

        /// <summary>
        /// 清理过期条目
        /// </summary>
        public void Cleanup()
        {
            float now = Time.unscaledTime;

            // 使用 ListPool 避免 GC 分配
            var keysToRemove = ListPool<int>.Get();
            try
            {
                foreach (var kvp in _cache)
                {
                    if (now - kvp.Value.Timestamp > _ttlSeconds)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                for (int i = 0; i < keysToRemove.Count; i++)
                {
                    _cache.Remove(keysToRemove[i]);
                }
            }
            finally
            {
                ListPool<int>.Return(keysToRemove);
            }
        }

        /// <summary>
        /// 强制驱逐一半条目（最旧优先）
        /// </summary>
        private void ForceEvict()
        {
            // 简单策略：删除一半条目
            int toRemove = _cache.Count / 2;

            // 使用 ListPool 避免 GC 分配
            var keysToRemove = ListPool<int>.Get();
            try
            {
                foreach (var key in _cache.Keys)
                {
                    keysToRemove.Add(key);
                    if (keysToRemove.Count >= toRemove)
                        break;
                }

                for (int i = 0; i < keysToRemove.Count; i++)
                {
                    _cache.Remove(keysToRemove[i]);
                }
            }
            finally
            {
                ListPool<int>.Return(keysToRemove);
            }
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            _hitCount = 0;
            _missCount = 0;
        }

        /// <summary>缓存条目数</summary>
        public int Count => _cache.Count;

        /// <summary>缓存命中率</summary>
        public float HitRate => (_hitCount + _missCount) > 0
            ? (float)_hitCount / (_hitCount + _missCount)
            : 0f;

        /// <summary>命中次数</summary>
        public int HitCount => _hitCount;

        /// <summary>未命中次数</summary>
        public int MissCount => _missCount;
    }
}
