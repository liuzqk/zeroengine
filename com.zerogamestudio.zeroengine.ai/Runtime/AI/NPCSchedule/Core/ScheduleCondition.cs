using System;
using UnityEngine;

namespace ZeroEngine.AI.NPCSchedule
{
    /// <summary>
    /// 日程条件基类
    /// 用于确定日程条目是否可以激活
    /// </summary>
    [Serializable]
    public abstract class ScheduleCondition
    {
        #region Serialized Fields

        [SerializeField] protected string _conditionName = "";
        [SerializeField] protected bool _invert = false;

        #endregion

        #region Properties

        /// <summary>条件名称</summary>
        public string ConditionName => string.IsNullOrEmpty(_conditionName) ? GetType().Name : _conditionName;

        /// <summary>是否反转结果</summary>
        public bool Invert => _invert;

        #endregion

        #region Abstract Methods

        /// <summary>
        /// 检查条件是否满足
        /// </summary>
        protected abstract bool Check(AIContext context);

        #endregion

        #region Public Methods

        /// <summary>
        /// 检查条件是否满足 (考虑反转)
        /// </summary>
        public bool IsMet(AIContext context)
        {
            bool result = Check(context);
            return _invert ? !result : result;
        }

        #endregion
    }

    /// <summary>
    /// 天气条件
    /// </summary>
    [Serializable]
    public class WeatherCondition : ScheduleCondition
    {
        [SerializeField] private WeatherType _requiredWeather = WeatherType.Clear;
        [SerializeField] private bool _anyOfThese = false;
        [SerializeField] private WeatherType[] _acceptedWeathers;

        public enum WeatherType
        {
            Clear,
            Cloudy,
            Rain,
            Snow,
            Storm,
            Fog
        }

        public WeatherCondition()
        {
            _conditionName = "Weather";
        }

        protected override bool Check(AIContext context)
        {
            // 从黑板获取当前天气
            string weatherStr = context?.Blackboard?.GetString("CurrentWeather", "Clear");

            if (Enum.TryParse<WeatherType>(weatherStr, out var currentWeather))
            {
                if (_anyOfThese && _acceptedWeathers != null)
                {
                    foreach (var weather in _acceptedWeathers)
                    {
                        if (currentWeather == weather) return true;
                    }
                    return false;
                }

                return currentWeather == _requiredWeather;
            }

            return false;
        }
    }

    /// <summary>
    /// 好感度条件 - 与 Relationship 系统集成
    /// </summary>
    [Serializable]
    public class RelationshipCondition : ScheduleCondition
    {
        [SerializeField] private string _targetNpcId = "";
        [SerializeField] private int _minLevel = 0;
        [SerializeField] private ComparisonOperator _comparison = ComparisonOperator.GreaterOrEqual;

        public RelationshipCondition()
        {
            _conditionName = "Relationship";
        }

        protected override bool Check(AIContext context)
        {
            if (context?.Blackboard == null) return false;

            // 从黑板获取好感度等级
            string key = $"Relationship_{_targetNpcId}";
            int level = context.Blackboard.GetInt(key, 0);

            return _comparison switch
            {
                ComparisonOperator.Equal => level == _minLevel,
                ComparisonOperator.NotEqual => level != _minLevel,
                ComparisonOperator.LessThan => level < _minLevel,
                ComparisonOperator.LessOrEqual => level <= _minLevel,
                ComparisonOperator.GreaterThan => level > _minLevel,
                ComparisonOperator.GreaterOrEqual => level >= _minLevel,
                _ => false
            };
        }
    }

    /// <summary>
    /// 任务条件 - 与 Quest 系统集成
    /// </summary>
    [Serializable]
    public class QuestCondition : ScheduleCondition
    {
        [SerializeField] private string _questId = "";
        [SerializeField] private QuestCheckType _checkType = QuestCheckType.IsCompleted;

        public enum QuestCheckType
        {
            IsActive,
            IsCompleted,
            IsFailed,
            NotStarted
        }

        public QuestCondition()
        {
            _conditionName = "Quest";
        }

        protected override bool Check(AIContext context)
        {
            if (context?.Blackboard == null || string.IsNullOrEmpty(_questId))
            {
                return false;
            }

            // 从黑板获取任务状态
            string statusKey = $"Quest_{_questId}_Status";
            string status = context.Blackboard.GetString(statusKey, "NotStarted");

            return _checkType switch
            {
                QuestCheckType.IsActive => status == "Active",
                QuestCheckType.IsCompleted => status == "Completed",
                QuestCheckType.IsFailed => status == "Failed",
                QuestCheckType.NotStarted => status == "NotStarted",
                _ => false
            };
        }
    }

    /// <summary>
    /// 物品条件 - 与 Inventory 系统集成
    /// </summary>
    [Serializable]
    public class ItemCondition : ScheduleCondition
    {
        [SerializeField] private string _itemId = "";
        [SerializeField] private int _requiredAmount = 1;
        [SerializeField] private ComparisonOperator _comparison = ComparisonOperator.GreaterOrEqual;

        public ItemCondition()
        {
            _conditionName = "Item";
        }

        protected override bool Check(AIContext context)
        {
            if (context?.Blackboard == null || string.IsNullOrEmpty(_itemId))
            {
                return false;
            }

            // 从黑板获取物品数量
            string key = $"Item_{_itemId}_Count";
            int count = context.Blackboard.GetInt(key, 0);

            return _comparison switch
            {
                ComparisonOperator.Equal => count == _requiredAmount,
                ComparisonOperator.NotEqual => count != _requiredAmount,
                ComparisonOperator.LessThan => count < _requiredAmount,
                ComparisonOperator.LessOrEqual => count <= _requiredAmount,
                ComparisonOperator.GreaterThan => count > _requiredAmount,
                ComparisonOperator.GreaterOrEqual => count >= _requiredAmount,
                _ => false
            };
        }
    }

    /// <summary>
    /// 黑板值条件
    /// </summary>
    [Serializable]
    public class BlackboardCondition : ScheduleCondition
    {
        [SerializeField] private string _key = "";
        [SerializeField] private BlackboardValueType _valueType = BlackboardValueType.Bool;
        [SerializeField] private bool _boolValue = true;
        [SerializeField] private int _intValue = 0;
        [SerializeField] private float _floatValue = 0f;
        [SerializeField] private string _stringValue = "";
        [SerializeField] private ComparisonOperator _comparison = ComparisonOperator.Equal;

        public enum BlackboardValueType
        {
            Bool,
            Int,
            Float,
            String,
            Exists
        }

        public BlackboardCondition()
        {
            _conditionName = "Blackboard";
        }

        protected override bool Check(AIContext context)
        {
            if (context?.Blackboard == null || string.IsNullOrEmpty(_key))
            {
                return false;
            }

            switch (_valueType)
            {
                case BlackboardValueType.Bool:
                    return context.Blackboard.GetBool(_key) == _boolValue;

                case BlackboardValueType.Int:
                    int intVal = context.Blackboard.GetInt(_key);
                    return Compare(intVal, _intValue, _comparison);

                case BlackboardValueType.Float:
                    float floatVal = context.Blackboard.GetFloat(_key);
                    return Compare(floatVal, _floatValue, _comparison);

                case BlackboardValueType.String:
                    string strVal = context.Blackboard.GetString(_key);
                    return _comparison == ComparisonOperator.Equal ?
                        strVal == _stringValue : strVal != _stringValue;

                case BlackboardValueType.Exists:
                    return context.Blackboard.Contains(_key);

                default:
                    return false;
            }
        }

        private static bool Compare<T>(T a, T b, ComparisonOperator op) where T : IComparable<T>
        {
            int result = a.CompareTo(b);
            return op switch
            {
                ComparisonOperator.Equal => result == 0,
                ComparisonOperator.NotEqual => result != 0,
                ComparisonOperator.LessThan => result < 0,
                ComparisonOperator.LessOrEqual => result <= 0,
                ComparisonOperator.GreaterThan => result > 0,
                ComparisonOperator.GreaterOrEqual => result >= 0,
                _ => false
            };
        }
    }

    /// <summary>
    /// 概率条件
    /// </summary>
    [Serializable]
    public class ProbabilityCondition : ScheduleCondition
    {
        [SerializeField] [Range(0f, 1f)]
        private float _probability = 0.5f;

        [SerializeField] private bool _evaluateOnce = true;

        private bool _hasEvaluated;
        private bool _cachedResult;

        public ProbabilityCondition()
        {
            _conditionName = "Probability";
        }

        protected override bool Check(AIContext context)
        {
            if (_evaluateOnce && _hasEvaluated)
            {
                return _cachedResult;
            }

            _cachedResult = UnityEngine.Random.value <= _probability;
            _hasEvaluated = true;

            return _cachedResult;
        }

        /// <summary>
        /// 重置评估状态
        /// </summary>
        public void ResetEvaluation()
        {
            _hasEvaluated = false;
        }
    }

    /// <summary>
    /// 复合条件
    /// </summary>
    [Serializable]
    public class CompositeCondition : ScheduleCondition
    {
        [SerializeField] private CompositeMode _mode = CompositeMode.All;

        [SerializeReference]
        private ScheduleCondition[] _conditions = Array.Empty<ScheduleCondition>();

        public enum CompositeMode
        {
            All,  // AND
            Any,  // OR
            None  // NOR
        }

        public CompositeCondition()
        {
            _conditionName = "Composite";
        }

        protected override bool Check(AIContext context)
        {
            if (_conditions.Length == 0) return true;

            switch (_mode)
            {
                case CompositeMode.All:
                    foreach (var condition in _conditions)
                    {
                        if (condition != null && !condition.IsMet(context))
                        {
                            return false;
                        }
                    }
                    return true;

                case CompositeMode.Any:
                    foreach (var condition in _conditions)
                    {
                        if (condition != null && condition.IsMet(context))
                        {
                            return true;
                        }
                    }
                    return false;

                case CompositeMode.None:
                    foreach (var condition in _conditions)
                    {
                        if (condition != null && condition.IsMet(context))
                        {
                            return false;
                        }
                    }
                    return true;

                default:
                    return true;
            }
        }
    }
}
