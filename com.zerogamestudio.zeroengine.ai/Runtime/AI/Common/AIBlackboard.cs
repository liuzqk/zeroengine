using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.AI
{
    /// <summary>
    /// AI 黑板 - 共享数据存储
    /// 用于 AI 系统间的数据传递和状态共享
    /// </summary>
    [Serializable]
    public class AIBlackboard
    {
        #region Storage

        private readonly Dictionary<string, object> _data = new();
        private readonly Dictionary<string, float> _lastModifiedTimes = new();

        #endregion

        #region Events

        /// <summary>值变更事件</summary>
        public event Action<string, object> OnValueChanged;

        /// <summary>值移除事件</summary>
        public event Action<string> OnValueRemoved;

        #endregion

        #region Basic Operations

        /// <summary>
        /// 设置值
        /// </summary>
        public void Set<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) return;

            _data[key] = value;
            _lastModifiedTimes[key] = Time.time;
            OnValueChanged?.Invoke(key, value);
        }

        /// <summary>
        /// 获取值
        /// </summary>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;

            if (_data.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }

                // 尝试类型转换
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// 尝试获取值
        /// </summary>
        public bool TryGet<T>(string key, out T value)
        {
            value = default;

            if (string.IsNullOrEmpty(key) || !_data.TryGetValue(key, out var obj))
            {
                return false;
            }

            if (obj is T typedValue)
            {
                value = typedValue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查是否包含键
        /// </summary>
        public bool Contains(string key)
        {
            return !string.IsNullOrEmpty(key) && _data.ContainsKey(key);
        }

        /// <summary>
        /// 移除值
        /// </summary>
        public bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            if (_data.Remove(key))
            {
                _lastModifiedTimes.Remove(key);
                OnValueRemoved?.Invoke(key);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void Clear()
        {
            _data.Clear();
            _lastModifiedTimes.Clear();
        }

        #endregion

        #region Type-Specific Getters

        public int GetInt(string key, int defaultValue = 0) => Get(key, defaultValue);
        public float GetFloat(string key, float defaultValue = 0f) => Get(key, defaultValue);
        public bool GetBool(string key, bool defaultValue = false) => Get(key, defaultValue);
        public string GetString(string key, string defaultValue = "") => Get(key, defaultValue);
        public Vector3 GetVector3(string key, Vector3 defaultValue = default) => Get(key, defaultValue);
        public GameObject GetGameObject(string key) => Get<GameObject>(key);
        public Transform GetTransform(string key) => Get<Transform>(key);

        #endregion

        #region Utility Methods

        /// <summary>
        /// 获取值的最后修改时间
        /// </summary>
        public float GetLastModifiedTime(string key)
        {
            return _lastModifiedTimes.TryGetValue(key, out var time) ? time : 0f;
        }

        /// <summary>
        /// 检查值是否在指定时间内被修改
        /// </summary>
        public bool WasModifiedWithin(string key, float seconds)
        {
            if (!_lastModifiedTimes.TryGetValue(key, out var time))
            {
                return false;
            }

            return Time.time - time <= seconds;
        }

        /// <summary>
        /// 增加数值
        /// </summary>
        public void Increment(string key, int amount = 1)
        {
            var current = GetInt(key, 0);
            Set(key, current + amount);
        }

        /// <summary>
        /// 增加浮点数值
        /// </summary>
        public void IncrementFloat(string key, float amount = 1f)
        {
            var current = GetFloat(key, 0f);
            Set(key, current + amount);
        }

        /// <summary>
        /// 获取所有键
        /// </summary>
        public IEnumerable<string> GetAllKeys()
        {
            return _data.Keys;
        }

        /// <summary>
        /// 获取数据数量
        /// </summary>
        public int Count => _data.Count;

        #endregion

        #region Comparison Operations

        /// <summary>
        /// 比较整数值
        /// </summary>
        public bool CompareInt(string key, int value, ComparisonOperator op)
        {
            var current = GetInt(key);
            return Compare(current, value, op);
        }

        /// <summary>
        /// 比较浮点值
        /// </summary>
        public bool CompareFloat(string key, float value, ComparisonOperator op)
        {
            var current = GetFloat(key);
            return Compare(current, value, op);
        }

        private static bool Compare<T>(T a, T b, ComparisonOperator op) where T : IComparable<T>
        {
            int comparison = a.CompareTo(b);

            return op switch
            {
                ComparisonOperator.Equal => comparison == 0,
                ComparisonOperator.NotEqual => comparison != 0,
                ComparisonOperator.LessThan => comparison < 0,
                ComparisonOperator.LessOrEqual => comparison <= 0,
                ComparisonOperator.GreaterThan => comparison > 0,
                ComparisonOperator.GreaterOrEqual => comparison >= 0,
                _ => false
            };
        }

        #endregion
    }

    /// <summary>
    /// 比较运算符
    /// </summary>
    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        LessThan,
        LessOrEqual,
        GreaterThan,
        GreaterOrEqual
    }

    /// <summary>
    /// 常用黑板键名
    /// </summary>
    public static class BlackboardKeys
    {
        // 目标相关
        public const string Target = "Target";
        public const string TargetPosition = "TargetPosition";
        public const string TargetDistance = "TargetDistance";
        public const string LastKnownTargetPosition = "LastKnownTargetPosition";

        // 状态相关
        public const string Health = "Health";
        public const string HealthPercent = "HealthPercent";
        public const string Mana = "Mana";
        public const string ManaPercent = "ManaPercent";
        public const string IsInCombat = "IsInCombat";
        public const string IsAlerted = "IsAlerted";

        // 移动相关
        public const string MoveDestination = "MoveDestination";
        public const string PatrolIndex = "PatrolIndex";
        public const string HomePosition = "HomePosition";

        // 时间相关
        public const string CurrentHour = "CurrentHour";
        public const string TimeOfDay = "TimeOfDay";

        // NPC 日程相关
        public const string CurrentSchedule = "CurrentSchedule";
        public const string ScheduleState = "ScheduleState";
    }
}
