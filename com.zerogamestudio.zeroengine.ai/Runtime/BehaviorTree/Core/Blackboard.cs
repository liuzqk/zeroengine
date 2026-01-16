using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.BehaviorTree
{
    /// <summary>
    /// 黑板数据共享实现，支持类型安全的数据存取
    /// 提供类型化访问器避免值类型装箱
    /// </summary>
    public class Blackboard : IBlackboard
    {
        // 通用存储（引用类型和非常用值类型）
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();

        // 类型化存储，避免装箱
        private readonly Dictionary<string, int> _intData = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _floatData = new Dictionary<string, float>();
        private readonly Dictionary<string, bool> _boolData = new Dictionary<string, bool>();
        private readonly Dictionary<string, Vector3> _vector3Data = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, Vector2> _vector2Data = new Dictionary<string, Vector2>();

        /// <inheritdoc/>
        public event Action<string, object> OnValueChanged;

        #region 类型化访问器（零装箱）

        /// <summary>设置 int 值（零装箱）</summary>
        public void SetInt(string key, int value)
        {
            _intData[key] = value;
        }

        /// <summary>获取 int 值（零装箱）</summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            return _intData.TryGetValue(key, out var val) ? val : defaultValue;
        }

        /// <summary>设置 float 值（零装箱）</summary>
        public void SetFloat(string key, float value)
        {
            _floatData[key] = value;
        }

        /// <summary>获取 float 值（零装箱）</summary>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            return _floatData.TryGetValue(key, out var val) ? val : defaultValue;
        }

        /// <summary>设置 bool 值（零装箱）</summary>
        public void SetBool(string key, bool value)
        {
            _boolData[key] = value;
        }

        /// <summary>获取 bool 值（零装箱）</summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            return _boolData.TryGetValue(key, out var val) ? val : defaultValue;
        }

        /// <summary>设置 Vector3 值（零装箱）</summary>
        public void SetVector3(string key, Vector3 value)
        {
            _vector3Data[key] = value;
        }

        /// <summary>获取 Vector3 值（零装箱）</summary>
        public Vector3 GetVector3(string key, Vector3 defaultValue = default)
        {
            return _vector3Data.TryGetValue(key, out var val) ? val : defaultValue;
        }

        /// <summary>设置 Vector2 值（零装箱）</summary>
        public void SetVector2(string key, Vector2 value)
        {
            _vector2Data[key] = value;
        }

        /// <summary>获取 Vector2 值（零装箱）</summary>
        public Vector2 GetVector2(string key, Vector2 defaultValue = default)
        {
            return _vector2Data.TryGetValue(key, out var val) ? val : defaultValue;
        }

        #endregion

        #region IBlackboard 通用接口（可能装箱）

        /// <inheritdoc/>
        public void SetValue<T>(string key, T value)
        {
            // 检测常见值类型，使用类型化存储
            if (typeof(T) == typeof(int))
            {
                _intData[key] = (int)(object)value;
                return;
            }
            if (typeof(T) == typeof(float))
            {
                _floatData[key] = (float)(object)value;
                return;
            }
            if (typeof(T) == typeof(bool))
            {
                _boolData[key] = (bool)(object)value;
                return;
            }
            if (typeof(T) == typeof(Vector3))
            {
                _vector3Data[key] = (Vector3)(object)value;
                return;
            }
            if (typeof(T) == typeof(Vector2))
            {
                _vector2Data[key] = (Vector2)(object)value;
                return;
            }

            // 其他类型使用通用存储
            _data[key] = value;
            OnValueChanged?.Invoke(key, value);
        }

        /// <inheritdoc/>
        public T GetValue<T>(string key)
        {
            // 检测常见值类型，从类型化存储获取
            if (typeof(T) == typeof(int))
            {
                if (_intData.TryGetValue(key, out var intVal))
                    return (T)(object)intVal;
                return default;
            }
            if (typeof(T) == typeof(float))
            {
                if (_floatData.TryGetValue(key, out var floatVal))
                    return (T)(object)floatVal;
                return default;
            }
            if (typeof(T) == typeof(bool))
            {
                if (_boolData.TryGetValue(key, out var boolVal))
                    return (T)(object)boolVal;
                return default;
            }
            if (typeof(T) == typeof(Vector3))
            {
                if (_vector3Data.TryGetValue(key, out var v3Val))
                    return (T)(object)v3Val;
                return default;
            }
            if (typeof(T) == typeof(Vector2))
            {
                if (_vector2Data.TryGetValue(key, out var v2Val))
                    return (T)(object)v2Val;
                return default;
            }

            // 通用存储
            if (_data.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            // 检测常见值类型
            if (typeof(T) == typeof(int))
            {
                if (_intData.TryGetValue(key, out var intVal))
                {
                    value = (T)(object)intVal;
                    return true;
                }
                value = default;
                return false;
            }
            if (typeof(T) == typeof(float))
            {
                if (_floatData.TryGetValue(key, out var floatVal))
                {
                    value = (T)(object)floatVal;
                    return true;
                }
                value = default;
                return false;
            }
            if (typeof(T) == typeof(bool))
            {
                if (_boolData.TryGetValue(key, out var boolVal))
                {
                    value = (T)(object)boolVal;
                    return true;
                }
                value = default;
                return false;
            }

            // 通用存储
            if (_data.TryGetValue(key, out var rawValue) && rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }
            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool HasKey(string key)
        {
            return _data.ContainsKey(key) ||
                   _intData.ContainsKey(key) ||
                   _floatData.ContainsKey(key) ||
                   _boolData.ContainsKey(key) ||
                   _vector3Data.ContainsKey(key) ||
                   _vector2Data.ContainsKey(key);
        }

        /// <inheritdoc/>
        public bool RemoveKey(string key)
        {
            bool removed = false;
            removed |= _data.Remove(key);
            removed |= _intData.Remove(key);
            removed |= _floatData.Remove(key);
            removed |= _boolData.Remove(key);
            removed |= _vector3Data.Remove(key);
            removed |= _vector2Data.Remove(key);
            return removed;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _data.Clear();
            _intData.Clear();
            _floatData.Clear();
            _boolData.Clear();
            _vector3Data.Clear();
            _vector2Data.Clear();
        }

        #endregion
    }
}
