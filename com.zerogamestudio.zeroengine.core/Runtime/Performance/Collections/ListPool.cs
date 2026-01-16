using System;
using System.Collections.Generic;
using System.Threading;

namespace ZeroEngine.Performance.Collections
{
    /// <summary>
    /// 高性能 List 对象池，支持预分配和自动收缩
    /// 线程安全设计
    /// </summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    public static class ListPool<T>
    {
        private static readonly Stack<List<T>> _pool = new Stack<List<T>>(32);
        private static readonly object _lock = new object();

        private const int DefaultCapacity = 16;
        private const int MaxPoolSize = 64;
        private const int MaxListCapacity = 1024;

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

        /// <summary>获取次数</summary>
        public static int GetCount => _getCount;

        /// <summary>归还次数</summary>
        public static int ReturnCount => _returnCount;

        /// <summary>
        /// 从池中获取 List
        /// </summary>
        /// <param name="capacity">初始容量（仅新创建时生效）</param>
        /// <returns>可用的 List 实例</returns>
        public static List<T> Get(int capacity = DefaultCapacity)
        {
            lock (_lock)
            {
                Interlocked.Increment(ref _getCount);

                if (_pool.Count > 0)
                {
                    var list = _pool.Pop();
                    return list;
                }
            }

            // 创建新实例（在锁外）- 使用 Interlocked 确保原子性
            Interlocked.Increment(ref _totalCreated);
            return new List<T>(Math.Min(capacity, MaxListCapacity));
        }

        /// <summary>
        /// 归还 List 到池中
        /// </summary>
        /// <param name="list">要归还的 List</param>
        public static void Return(List<T> list)
        {
            if (list == null) return;

            // 清空但保留容量
            list.Clear();

            // 容量过大时不放回池中，避免内存浪费
            if (list.Capacity > MaxListCapacity)
            {
                return;
            }

            lock (_lock)
            {
                Interlocked.Increment(ref _returnCount);

                // 池满时不再放入
                if (_pool.Count < MaxPoolSize)
                {
                    _pool.Push(list);
                }
            }
        }

        /// <summary>
        /// 预热池
        /// </summary>
        /// <param name="count">预创建数量</param>
        /// <param name="listCapacity">每个 List 的初始容量</param>
        public static void WarmUp(int count, int listCapacity = DefaultCapacity)
        {
            var lists = new List<T>[count];
            for (int i = 0; i < count; i++)
            {
                lists[i] = Get(listCapacity);
            }

            for (int i = 0; i < count; i++)
            {
                Return(lists[i]);
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
        /// 重置统计
        /// </summary>
        public static void ResetStats()
        {
            Interlocked.Exchange(ref _totalCreated, 0);
            Interlocked.Exchange(ref _getCount, 0);
            Interlocked.Exchange(ref _returnCount, 0);
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
