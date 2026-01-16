using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.AI.NPCSchedule
{
    /// <summary>
    /// 日程条目 - 定义 NPC 在特定时间的行为
    /// </summary>
    [Serializable]
    public class ScheduleEntry
    {
        #region Serialized Fields

        [SerializeField] private string _entryId = "";
        [SerializeField] private string _description = "";

        [Header("Time")]
        [SerializeField] private float _startHour = 8f;
        [SerializeField] private float _endHour = 12f;
        [SerializeField] private int _priority = 0;

        [Header("Day Filter")]
        [SerializeField] private DayOfWeekMask _activeDays = DayOfWeekMask.All;
        [SerializeField] private bool _useSeasons = false;
        [SerializeField] private SeasonMask _activeSeasons = SeasonMask.All;

        [Header("Action")]
        [SerializeReference]
        private ScheduleAction _action;

        [Header("Conditions")]
        [SerializeReference]
        private List<ScheduleCondition> _conditions = new();

        [Header("Location")]
        [SerializeField] private string _locationId = "";
        [SerializeField] private Vector3 _targetPosition;
        [SerializeField] private bool _useTransformReference = false;
        [SerializeField] private Transform _targetTransform;

        #endregion

        #region Properties

        /// <summary>条目 ID</summary>
        public string EntryId => _entryId;

        /// <summary>描述</summary>
        public string Description => _description;

        /// <summary>开始时间 (小时 0-24)</summary>
        public float StartHour => _startHour;

        /// <summary>结束时间 (小时 0-24)</summary>
        public float EndHour => _endHour;

        /// <summary>优先级 (高优先级覆盖低优先级)</summary>
        public int Priority => _priority;

        /// <summary>行动</summary>
        public ScheduleAction Action => _action;

        /// <summary>条件列表</summary>
        public IReadOnlyList<ScheduleCondition> Conditions => _conditions;

        /// <summary>目标位置</summary>
        public Vector3 TargetPosition =>
            _useTransformReference && _targetTransform != null ?
            _targetTransform.position : _targetPosition;

        /// <summary>位置 ID</summary>
        public string LocationId => _locationId;

        #endregion

        #region Time Check

        /// <summary>
        /// 检查当前时间是否在此条目的时间范围内
        /// </summary>
        public bool IsActiveAtTime(float hour)
        {
            // 处理跨午夜的情况
            if (_startHour <= _endHour)
            {
                return hour >= _startHour && hour < _endHour;
            }
            else
            {
                // 跨午夜: 如 22:00 - 6:00
                return hour >= _startHour || hour < _endHour;
            }
        }

        /// <summary>
        /// 检查是否在指定日期激活
        /// </summary>
        public bool IsActiveOnDay(DayOfWeek day)
        {
            DayOfWeekMask mask = day switch
            {
                DayOfWeek.Sunday => DayOfWeekMask.Sunday,
                DayOfWeek.Monday => DayOfWeekMask.Monday,
                DayOfWeek.Tuesday => DayOfWeekMask.Tuesday,
                DayOfWeek.Wednesday => DayOfWeekMask.Wednesday,
                DayOfWeek.Thursday => DayOfWeekMask.Thursday,
                DayOfWeek.Friday => DayOfWeekMask.Friday,
                DayOfWeek.Saturday => DayOfWeekMask.Saturday,
                _ => DayOfWeekMask.None
            };

            return (_activeDays & mask) != DayOfWeekMask.None;
        }

        /// <summary>
        /// 检查是否在指定季节激活
        /// </summary>
        public bool IsActiveInSeason(Season season)
        {
            if (!_useSeasons) return true;

            SeasonMask mask = season switch
            {
                Season.Spring => SeasonMask.Spring,
                Season.Summer => SeasonMask.Summer,
                Season.Autumn => SeasonMask.Autumn,
                Season.Winter => SeasonMask.Winter,
                _ => SeasonMask.None
            };

            return (_activeSeasons & mask) != SeasonMask.None;
        }

        #endregion

        #region Condition Check

        /// <summary>
        /// 检查所有条件是否满足
        /// </summary>
        public bool CheckConditions(AIContext context)
        {
            if (_conditions.Count == 0) return true;

            foreach (var condition in _conditions)
            {
                if (condition != null && !condition.IsMet(context))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Utility

        /// <summary>
        /// 获取到开始时间的剩余小时数
        /// </summary>
        public float GetHoursUntilStart(float currentHour)
        {
            if (currentHour < _startHour)
            {
                return _startHour - currentHour;
            }
            else
            {
                return (24f - currentHour) + _startHour;
            }
        }

        /// <summary>
        /// 获取剩余时长 (小时)
        /// </summary>
        public float GetRemainingDuration(float currentHour)
        {
            if (!IsActiveAtTime(currentHour)) return 0f;

            if (_startHour <= _endHour)
            {
                return _endHour - currentHour;
            }
            else
            {
                if (currentHour >= _startHour)
                {
                    return (24f - currentHour) + _endHour;
                }
                else
                {
                    return _endHour - currentHour;
                }
            }
        }

        public override string ToString()
        {
            return $"{_entryId}: {_startHour:00}:00 - {_endHour:00}:00 ({_description})";
        }

        #endregion
    }

    /// <summary>
    /// 星期掩码
    /// </summary>
    [Flags]
    public enum DayOfWeekMask
    {
        None = 0,
        Sunday = 1 << 0,
        Monday = 1 << 1,
        Tuesday = 1 << 2,
        Wednesday = 1 << 3,
        Thursday = 1 << 4,
        Friday = 1 << 5,
        Saturday = 1 << 6,
        Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
        Weekend = Saturday | Sunday,
        All = Weekdays | Weekend
    }

    /// <summary>
    /// 季节
    /// </summary>
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    /// <summary>
    /// 季节掩码
    /// </summary>
    [Flags]
    public enum SeasonMask
    {
        None = 0,
        Spring = 1 << 0,
        Summer = 1 << 1,
        Autumn = 1 << 2,
        Winter = 1 << 3,
        All = Spring | Summer | Autumn | Winter
    }
}
