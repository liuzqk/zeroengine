using System;
using System.Collections.Generic;
using System.Threading;

namespace ZeroEngine.Performance.Collections
{
    /// <summary>
    /// 高性能 Dictionary 对象池
    /// 线程安全设计
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    public static class DictionaryPool<TKey, TValue>
    {
        private static readonly Stack<Dictionary<TKey, TValue>> _pool = new Stack<Dictionary<TKey, TValue>>(16);
        private static readonly object _lock = new object();

        private const int DefaultCapacity = 16;
        private const int MaxPoolSize = 32;
        private const int MaxDictCapacity = 512;

        // 统计
        private static int _totalCreated;
        private static int _getCount;
        private static int _returnCount;

        /// <summary>池中可用对象数</summary>
        public static int PooledCount
        {
            get
            {
                lock (_lock)
                {
                    return _pool.Count;
                }
            }
        }

        /// <summary>总创建数</summary>
        public static int TotalCreated => _totalCreated;

        /// <summary>
        /// 从池中获取 Dictionary
        /// </summary>
        /// <param name="capacity">初始容量（仅新创建时生效）</param>
        /// <returns>可用的 Dictionary 实例</returns>
        public static Dictionary<TKey, TValue> Get(int capacity = DefaultCapacity)
        {
            lock (_lock)
            {
                Interlocked.Increment(ref _getCount);

                if (_pool.Count > 0)
                {
                    return _pool.Pop();
                }
            }

            // 使用 Interlocked 确保原子性
            Interlocked.Increment(ref _totalCreated);
            return new Dictionary<TKey, TValue>(Math.Min(capacity, MaxDictCapacity));
        }

        /// <summary>
        /// 归还 Dictionary 到池中
        /// </summary>
        /// <param name="dict">要归还的 Dictionary</param>
        public static void Return(Dictionary<TKey, TValue> dict)
        {
            if (dict == null) return;

            // 先检查元素数（作为容量近似），再清空
            // Dictionary 没有暴露 Capacity 属性，使用 Count 作为近似判断
            int originalCount = dict.Count;
            dict.Clear();

            // 如果原始元素数超过阈值，说明内部数组已扩容，不放回池中
            if (originalCount > MaxDictCapacity)
            {
                return;
            }

            lock (_lock)
            {
                Interlocked.Increment(ref _returnCount);

                if (_pool.Count < MaxPoolSize)
                {
                    _pool.Push(dict);
                }
            }
        }

        /// <summary>
        /// 预热池
        /// </summary>
        public static void WarmUp(int count, int capacity = DefaultCapacity)
        {
            var dicts = new Dictionary<TKey, TValue>[count];
            for (int i = 0; i < count; i++)
            {
                dicts[i] = Get(capacity);
            }

            for (int i = 0; i < count; i++)
            {
                Return(dicts[i]);
            }
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _pool.Clear();
            }
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public static PoolStats GetStats()
        {
            return new PoolStats
            {
                PooledCount = PooledCount,
                ActiveCount = _getCount - _returnCount,
                TotalCreated = _totalCreated,
                GetCount = _getCount,
                ReturnCount = _returnCount
            };
        }
    }
}
