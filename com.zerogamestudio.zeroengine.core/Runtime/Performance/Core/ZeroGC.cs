using System;
using System.Collections.Generic;
using System.Text;
using ZeroEngine.Performance.Caching;
using ZeroEngine.Performance.Collections;

namespace ZeroEngine.Performance
{
    /// <summary>
    /// GC 优化工具集统一入口 (v1.4.0+)
    /// 提供零 GC 的集合池和缓存访问
    /// </summary>
    public static class ZeroGC
    {
        private static volatile FormattedValueCache _formattedValueCache;
        private static readonly object _cacheLock = new object();

        /// <summary>
        /// 格式化值缓存实例（懒加载）
        /// </summary>
        public static FormattedValueCache FormattedValueCache
        {
            get
            {
                if (_formattedValueCache == null)
                {
                    lock (_cacheLock)
                    {
                        if (_formattedValueCache == null)
                        {
                            _formattedValueCache = new FormattedValueCache();
                        }
                    }
                }
                return _formattedValueCache;
            }
        }

        #region List Pool

        /// <summary>
        /// 获取池化的 List（自动归还版本）
        /// 用法: using var list = ZeroGC.GetList&lt;int&gt;();
        /// </summary>
        public static PooledList<T> GetList<T>(int capacity = 16)
        {
            return new PooledList<T>(ListPool<T>.Get(capacity));
        }

        /// <summary>
        /// 获取原始 List（需手动归还）
        /// </summary>
        public static List<T> GetListRaw<T>(int capacity = 16)
        {
            return ListPool<T>.Get(capacity);
        }

        /// <summary>
        /// 归还 List 到池中
        /// </summary>
        public static void Return<T>(List<T> list)
        {
            ListPool<T>.Return(list);
        }

        #endregion

        #region Dictionary Pool

        /// <summary>
        /// 获取池化的 Dictionary（自动归还版本）
        /// 用法: using var dict = ZeroGC.GetDictionary&lt;string, int&gt;();
        /// </summary>
        public static PooledDictionary<TKey, TValue> GetDictionary<TKey, TValue>(int capacity = 16)
        {
            return new PooledDictionary<TKey, TValue>(DictionaryPool<TKey, TValue>.Get(capacity));
        }

        /// <summary>
        /// 获取原始 Dictionary（需手动归还）
        /// </summary>
        public static Dictionary<TKey, TValue> GetDictionaryRaw<TKey, TValue>(int capacity = 16)
        {
            return DictionaryPool<TKey, TValue>.Get(capacity);
        }

        /// <summary>
        /// 归还 Dictionary 到池中
        /// </summary>
        public static void Return<TKey, TValue>(Dictionary<TKey, TValue> dict)
        {
            DictionaryPool<TKey, TValue>.Return(dict);
        }

        #endregion

        #region StringBuilder Pool

        /// <summary>
        /// 获取池化的 StringBuilder
        /// </summary>
        public static StringBuilder GetStringBuilder(int capacity = 256)
        {
            return StringBuilderPool.Get(capacity);
        }

        /// <summary>
        /// 归还 StringBuilder 到池中
        /// </summary>
        public static void Return(StringBuilder sb)
        {
            StringBuilderPool.Return(sb);
        }

        /// <summary>
        /// 使用 StringBuilder 构建字符串（自动获取和归还）
        /// </summary>
        public static string BuildString(Action<StringBuilder> action)
        {
            return StringBuilderPool.Build(action);
        }

        #endregion

        #region Caching

        /// <summary>
        /// 获取或创建格式化值缓存
        /// </summary>
        public static string CacheFormattedValue<T>(string key, T value, Func<T, string> formatter)
        {
            return FormattedValueCache.GetOrCreate(key, value, formatter);
        }

        /// <summary>
        /// 获取或创建格式化值缓存（使用格式字符串）
        /// </summary>
        public static string CacheFormattedValue<T>(string key, T value, string format)
        {
            return FormattedValueCache.GetOrCreate(key, value, format);
        }

        /// <summary>
        /// 使指定前缀的缓存失效
        /// </summary>
        public static void InvalidateCache(string keyPrefix)
        {
            FormattedValueCache.Invalidate(keyPrefix);
        }

        #endregion

        #region Warm Up

        /// <summary>
        /// 预热 List 池
        /// </summary>
        public static void WarmUpLists<T>(int count, int listCapacity = 16)
        {
            ListPool<T>.WarmUp(count, listCapacity);
        }

        /// <summary>
        /// 预热 Dictionary 池
        /// </summary>
        public static void WarmUpDictionaries<TKey, TValue>(int count, int capacity = 16)
        {
            DictionaryPool<TKey, TValue>.WarmUp(count, capacity);
        }

        /// <summary>
        /// 预热 StringBuilder 池
        /// </summary>
        public static void WarmUpStringBuilders(int count, int capacity = 256)
        {
            StringBuilderPool.WarmUp(count, capacity);
        }

        /// <summary>
        /// 预热常用集合
        /// </summary>
        public static void WarmUpCommon(int listCount = 8, int dictCount = 4, int sbCount = 4)
        {
            // 预热常用类型的 List
            ListPool<int>.WarmUp(listCount);
            ListPool<float>.WarmUp(listCount);
            ListPool<string>.WarmUp(listCount);
            ListPool<object>.WarmUp(listCount);

            // 预热常用类型的 Dictionary
            DictionaryPool<string, object>.WarmUp(dictCount);
            DictionaryPool<string, int>.WarmUp(dictCount);
            DictionaryPool<int, object>.WarmUp(dictCount);

            // 预热 StringBuilder
            StringBuilderPool.WarmUp(sbCount);
        }

        #endregion

        #region Statistics

        /// <summary>
        /// 获取全局性能统计
        /// </summary>
        public static PerformanceStats GetStats()
        {
            return new PerformanceStats
            {
                ListPoolStats = GetListPoolStats(),
                DictionaryPoolStats = GetDictionaryPoolStats(),
                StringBuilderPoolStats = StringBuilderPool.GetStats(),
                CacheHitRate = FormattedValueCache.HitRate,
                CacheEntryCount = FormattedValueCache.Count
            };
        }

        /// <summary>
        /// 获取 List 池统计（聚合所有类型）
        /// </summary>
        private static PoolStats GetListPoolStats()
        {
            // 由于泛型池是独立的，这里只能返回常用类型的聚合
            var intStats = ListPool<int>.GetStats();
            var floatStats = ListPool<float>.GetStats();
            var stringStats = ListPool<string>.GetStats();
            var objectStats = ListPool<object>.GetStats();

            return new PoolStats
            {
                PooledCount = intStats.PooledCount + floatStats.PooledCount + stringStats.PooledCount + objectStats.PooledCount,
                TotalCreated = intStats.TotalCreated + floatStats.TotalCreated + stringStats.TotalCreated + objectStats.TotalCreated,
                GetCount = intStats.GetCount + floatStats.GetCount + stringStats.GetCount + objectStats.GetCount,
                ReturnCount = intStats.ReturnCount + floatStats.ReturnCount + stringStats.ReturnCount + objectStats.ReturnCount
            };
        }

        /// <summary>
        /// 获取 Dictionary 池统计（聚合所有类型）
        /// </summary>
        private static PoolStats GetDictionaryPoolStats()
        {
            var strObjStats = DictionaryPool<string, object>.GetStats();
            var strIntStats = DictionaryPool<string, int>.GetStats();
            var intObjStats = DictionaryPool<int, object>.GetStats();

            return new PoolStats
            {
                PooledCount = strObjStats.PooledCount + strIntStats.PooledCount + intObjStats.PooledCount,
                TotalCreated = strObjStats.TotalCreated + strIntStats.TotalCreated + intObjStats.TotalCreated,
                GetCount = strObjStats.GetCount + strIntStats.GetCount + intObjStats.GetCount,
                ReturnCount = strObjStats.ReturnCount + strIntStats.ReturnCount + intObjStats.ReturnCount
            };
        }

        /// <summary>
        /// 重置所有统计
        /// </summary>
        public static void ResetStats()
        {
            ListPool<int>.ResetStats();
            ListPool<float>.ResetStats();
            ListPool<string>.ResetStats();
            ListPool<object>.ResetStats();

            FormattedValueCache.Clear();
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理所有池和缓存
        /// </summary>
        public static void ClearAll()
        {
            ListPool<int>.Clear();
            ListPool<float>.Clear();
            ListPool<string>.Clear();
            ListPool<object>.Clear();

            DictionaryPool<string, object>.Clear();
            DictionaryPool<string, int>.Clear();
            DictionaryPool<int, object>.Clear();

            StringBuilderPool.Clear();

            FormattedValueCache.Clear();
        }

        #endregion
    }
}
