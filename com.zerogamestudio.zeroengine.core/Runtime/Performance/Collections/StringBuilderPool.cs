using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ZeroEngine.Performance.Collections
{
    /// <summary>
    /// 高性能 StringBuilder 对象池
    /// 线程安全设计
    /// </summary>
    public static class StringBuilderPool
    {
        private static readonly Stack<StringBuilder> _pool = new Stack<StringBuilder>(16);
        private static readonly object _lock = new object();

        private const int DefaultCapacity = 256;
        private const int MaxPoolSize = 32;
        private const int MaxBuilderCapacity = 8192;

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

        /// <summary>
        /// 从池中获取 StringBuilder
        /// </summary>
        /// <param name="capacity">初始容量（仅新创建时生效）</param>
        /// <returns>可用的 StringBuilder 实例</returns>
        public static StringBuilder Get(int capacity = DefaultCapacity)
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
            return new StringBuilder(Math.Min(capacity, MaxBuilderCapacity));
        }

        /// <summary>
        /// 归还 StringBuilder 到池中
        /// </summary>
        /// <param name="sb">要归还的 StringBuilder</param>
        public static void Return(StringBuilder sb)
        {
            if (sb == null) return;

            // 清空内容
            sb.Clear();

            // 容量过大时不放回池中
            if (sb.Capacity > MaxBuilderCapacity)
            {
                return;
            }

            lock (_lock)
            {
                Interlocked.Increment(ref _returnCount);

                if (_pool.Count < MaxPoolSize)
                {
                    _pool.Push(sb);
                }
            }
        }

        /// <summary>
        /// 获取 StringBuilder，使用后自动归还并返回字符串
        /// </summary>
        /// <param name="action">使用 StringBuilder 的操作</param>
        /// <returns>构建的字符串</returns>
        public static string Build(Action<StringBuilder> action)
        {
            var sb = Get();
            try
            {
                action(sb);
                return sb.ToString();
            }
            finally
            {
                Return(sb);
            }
        }

        /// <summary>
        /// 预热池
        /// </summary>
        public static void WarmUp(int count, int capacity = DefaultCapacity)
        {
            var builders = new StringBuilder[count];
            for (int i = 0; i < count; i++)
            {
                builders[i] = Get(capacity);
            }

            for (int i = 0; i < count; i++)
            {
                Return(builders[i]);
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
