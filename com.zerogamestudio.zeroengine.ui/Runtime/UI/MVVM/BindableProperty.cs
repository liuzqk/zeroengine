using System;
using System.Collections.Generic;

namespace ZeroEngine.UI.MVVM
{
    /// <summary>
    /// 属性验证结果
    /// </summary>
    public struct ValidationResult
    {
        /// <summary>是否验证通过</summary>
        public bool IsValid;
        /// <summary>错误消息（验证失败时）</summary>
        public string ErrorMessage;

        /// <summary>验证通过</summary>
        public static ValidationResult Valid => new() { IsValid = true, ErrorMessage = null };
        /// <summary>验证失败</summary>
        public static ValidationResult Invalid(string message) => new() { IsValid = false, ErrorMessage = message };
    }

    /// <summary>
    /// 可绑定属性 - MVVM数据绑定核心
    /// 当值变化时自动通知所有监听者
    /// 支持双向绑定、格式化和验证
    /// </summary>
    public class BindableProperty<T>
    {
        private T _value;
        private event Action<T> _onValueChanged;
        private Func<T, string> _formatter;
        private Func<T, ValidationResult> _validator;
        private ValidationResult _lastValidation = ValidationResult.Valid;

        /// <summary>值变化事件</summary>
        public event Action<T> OnValueChanged
        {
            add => _onValueChanged += value;
            remove => _onValueChanged -= value;
        }

        /// <summary>验证状态变化事件</summary>
        public event Action<ValidationResult> OnValidationChanged;

        /// <summary>当前验证状态</summary>
        public ValidationResult ValidationState => _lastValidation;

        /// <summary>是否验证通过</summary>
        public bool IsValid => _lastValidation.IsValid;

        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value))
                    return;

                // 验证新值
                if (_validator != null)
                {
                    var result = _validator(value);
                    if (!result.IsValid)
                    {
                        _lastValidation = result;
                        OnValidationChanged?.Invoke(result);
                        return; // 验证失败，不更新值
                    }
                }

                _value = value;
                _lastValidation = ValidationResult.Valid;
                OnValidationChanged?.Invoke(_lastValidation);
                _onValueChanged?.Invoke(_value);
            }
        }

        /// <summary>强制设置值（跳过验证）</summary>
        public void SetValueWithoutValidation(T value)
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
                return;

            _value = value;
            _onValueChanged?.Invoke(_value);
        }

        /// <summary>获取格式化后的值</summary>
        public string FormattedValue => _formatter != null ? _formatter(_value) : _value?.ToString() ?? string.Empty;

        public BindableProperty(T defaultValue = default)
        {
            _value = defaultValue;
        }

        /// <summary>
        /// 设置格式化器
        /// </summary>
        /// <param name="formatter">格式化函数</param>
        /// <returns>返回自身以支持链式调用</returns>
        public BindableProperty<T> WithFormat(Func<T, string> formatter)
        {
            _formatter = formatter;
            return this;
        }

        /// <summary>
        /// 设置格式化字符串
        /// </summary>
        /// <param name="format">格式化字符串，如 "{0:F2}" 或 "{0}%"</param>
        /// <returns>返回自身以支持链式调用</returns>
        public BindableProperty<T> WithFormat(string format)
        {
            _formatter = v => string.Format(format, v);
            return this;
        }

        /// <summary>
        /// 设置验证器
        /// </summary>
        /// <param name="validator">验证函数</param>
        /// <returns>返回自身以支持链式调用</returns>
        public BindableProperty<T> WithValidation(Func<T, ValidationResult> validator)
        {
            _validator = validator;
            return this;
        }

        /// <summary>
        /// 设置简单验证条件
        /// </summary>
        /// <param name="condition">验证条件</param>
        /// <param name="errorMessage">失败消息</param>
        /// <returns>返回自身以支持链式调用</returns>
        public BindableProperty<T> WithValidation(Func<T, bool> condition, string errorMessage)
        {
            _validator = v => condition(v) ? ValidationResult.Valid : ValidationResult.Invalid(errorMessage);
            return this;
        }

        /// <summary>
        /// 注册值变化监听
        /// </summary>
        public void Register(Action<T> callback)
        {
            _onValueChanged += callback;
        }

        /// <summary>
        /// 注册并立即触发一次
        /// </summary>
        public void RegisterAndInvoke(Action<T> callback)
        {
            _onValueChanged += callback;
            callback?.Invoke(_value);
        }

        /// <summary>
        /// 注销监听
        /// </summary>
        public void Unregister(Action<T> callback)
        {
            _onValueChanged -= callback;
        }

        /// <summary>
        /// 清除所有监听
        /// </summary>
        public void ClearListeners()
        {
            _onValueChanged = null;
        }

        /// <summary>
        /// 强制触发通知（即使值没变）
        /// </summary>
        public void NotifyValueChanged()
        {
            _onValueChanged?.Invoke(_value);
        }

        // 隐式转换
        public static implicit operator T(BindableProperty<T> property) => property.Value;

        public override string ToString() => _value?.ToString() ?? "null";
    }

    /// <summary>
    /// 可绑定集合 - 列表数据变化通知
    /// </summary>
    public class BindableList<T>
    {
        private List<T> _list = new();

        public event Action OnListChanged;
        public event Action<int, T> OnItemAdded;
        public event Action<int, T> OnItemRemoved;
        public event Action<int, T> OnItemChanged;

        public int Count => _list.Count;

        public T this[int index]
        {
            get => _list[index];
            set
            {
                if (index < 0 || index >= _list.Count) return;
                _list[index] = value;
                OnItemChanged?.Invoke(index, value);
                OnListChanged?.Invoke();
            }
        }

        public void Add(T item)
        {
            _list.Add(item);
            OnItemAdded?.Invoke(_list.Count - 1, item);
            OnListChanged?.Invoke();
        }

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
            OnItemAdded?.Invoke(index, item);
            OnListChanged?.Invoke();
        }

        public bool Remove(T item)
        {
            int index = _list.IndexOf(item);
            if (index < 0) return false;

            _list.RemoveAt(index);
            OnItemRemoved?.Invoke(index, item);
            OnListChanged?.Invoke();
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _list.Count) return;

            var item = _list[index];
            _list.RemoveAt(index);
            OnItemRemoved?.Invoke(index, item);
            OnListChanged?.Invoke();
        }

        public void Clear()
        {
            _list.Clear();
            OnListChanged?.Invoke();
        }

        public bool Contains(T item) => _list.Contains(item);
        public int IndexOf(T item) => _list.IndexOf(item);
        public List<T> ToList() => new(_list);

        public void ClearListeners()
        {
            OnListChanged = null;
            OnItemAdded = null;
            OnItemRemoved = null;
            OnItemChanged = null;
        }
    }

    /// <summary>
    /// 可绑定字典 - 字典数据变化通知
    /// </summary>
    public class BindableDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> _dict = new();

        public event Action OnDictionaryChanged;
        public event Action<TKey, TValue> OnItemAdded;
        public event Action<TKey, TValue> OnItemRemoved;
        public event Action<TKey, TValue> OnItemChanged;

        public int Count => _dict.Count;

        public TValue this[TKey key]
        {
            get => _dict.TryGetValue(key, out var value) ? value : default;
            set
            {
                bool exists = _dict.ContainsKey(key);
                _dict[key] = value;

                if (exists)
                    OnItemChanged?.Invoke(key, value);
                else
                    OnItemAdded?.Invoke(key, value);

                OnDictionaryChanged?.Invoke();
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (_dict.ContainsKey(key)) return;
            _dict.Add(key, value);
            OnItemAdded?.Invoke(key, value);
            OnDictionaryChanged?.Invoke();
        }

        public bool Remove(TKey key)
        {
            if (!_dict.TryGetValue(key, out var value)) return false;

            _dict.Remove(key);
            OnItemRemoved?.Invoke(key, value);
            OnDictionaryChanged?.Invoke();
            return true;
        }

        public void Clear()
        {
            _dict.Clear();
            OnDictionaryChanged?.Invoke();
        }

        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);

        public Dictionary<TKey, TValue>.KeyCollection Keys => _dict.Keys;
        public Dictionary<TKey, TValue>.ValueCollection Values => _dict.Values;

        public void ClearListeners()
        {
            OnDictionaryChanged = null;
            OnItemAdded = null;
            OnItemRemoved = null;
            OnItemChanged = null;
        }
    }
}
