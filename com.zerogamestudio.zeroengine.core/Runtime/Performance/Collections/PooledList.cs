using System;
using System.Collections;
using System.Collections.Generic;

namespace ZeroEngine.Performance.Collections
{
    /// <summary>
    /// 自动归还的 List 包装器，配合 using 语句使用
    /// 用法: using var list = ZeroGC.GetList&lt;int&gt;();
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public struct PooledList<T> : IDisposable, IList<T>, IReadOnlyList<T>
    {
        private List<T> _inner;
        private bool _disposed;

        internal PooledList(List<T> list)
        {
            _inner = list;
            _disposed = false;
        }

        /// <summary>
        /// 释放资源，将 List 归还到池中
        /// </summary>
        public void Dispose()
        {
            if (!_disposed && _inner != null)
            {
                ListPool<T>.Return(_inner);
                _inner = null;
                _disposed = true;
            }
        }

        /// <summary>获取内部 List（谨慎使用）</summary>
        public List<T> Inner => _inner;

        #region IList<T> Implementation

        public T this[int index]
        {
            get => _inner[index];
            set => _inner[index] = value;
        }

        public int Count => _inner?.Count ?? 0;

        public bool IsReadOnly => false;

        public void Add(T item) => _inner.Add(item);

        public void AddRange(IEnumerable<T> collection) => _inner.AddRange(collection);

        public void Clear() => _inner.Clear();

        public bool Contains(T item) => _inner.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();

        public int IndexOf(T item) => _inner.IndexOf(item);

        public void Insert(int index, T item) => _inner.Insert(index, item);

        public bool Remove(T item) => _inner.Remove(item);

        public void RemoveAt(int index) => _inner.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Extended Methods

        public void Sort() => _inner.Sort();

        public void Sort(IComparer<T> comparer) => _inner.Sort(comparer);

        public void Sort(Comparison<T> comparison) => _inner.Sort(comparison);

        public T Find(Predicate<T> match) => _inner.Find(match);

        public List<T> FindAll(Predicate<T> match) => _inner.FindAll(match);

        public int FindIndex(Predicate<T> match) => _inner.FindIndex(match);

        public void ForEach(Action<T> action) => _inner.ForEach(action);

        public T[] ToArray() => _inner.ToArray();

        #endregion
    }

    /// <summary>
    /// 自动归还的 Dictionary 包装器
    /// </summary>
    public struct PooledDictionary<TKey, TValue> : IDisposable, IDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> _inner;
        private bool _disposed;

        internal PooledDictionary(Dictionary<TKey, TValue> dict)
        {
            _inner = dict;
            _disposed = false;
        }

        public void Dispose()
        {
            if (!_disposed && _inner != null)
            {
                DictionaryPool<TKey, TValue>.Return(_inner);
                _inner = null;
                _disposed = true;
            }
        }

        /// <summary>获取内部 Dictionary（谨慎使用）</summary>
        public Dictionary<TKey, TValue> Inner => _inner;

        #region IDictionary Implementation

        public TValue this[TKey key]
        {
            get => _inner[key];
            set => _inner[key] = value;
        }

        public ICollection<TKey> Keys => _inner.Keys;
        public ICollection<TValue> Values => _inner.Values;
        public int Count => _inner?.Count ?? 0;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value) => _inner.Add(key, value);
        public void Add(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).Add(item);
        public void Clear() => _inner.Clear();
        public bool Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).Contains(item);
        public bool ContainsKey(TKey key) => _inner.ContainsKey(key);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _inner.GetEnumerator();
        public bool Remove(TKey key) => _inner.Remove(key);
        public bool Remove(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).Remove(item);
        public bool TryGetValue(TKey key, out TValue value) => _inner.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}
