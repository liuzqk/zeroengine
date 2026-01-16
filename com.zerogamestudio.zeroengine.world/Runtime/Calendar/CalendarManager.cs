using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Core;
using ZeroEngine.Save;

namespace ZeroEngine.Calendar
{
    /// <summary>
    /// 日历系统管理器
    /// 管理游戏内时间、日期、季节和事件
    /// </summary>
    public class CalendarManager : MonoSingleton<CalendarManager>, ISaveable
    {
        [Header("时间配置")]
        [SerializeField] private float _realSecondsPerGameMinute = 1f;
        [SerializeField] private bool _timeFlows = true;
        [SerializeField] private GameDate _startDate = new GameDate(1, 1, 1);
        [SerializeField] private GameTime _startTime = new GameTime(8, 0);

        [Header("事件配置")]
        [SerializeField] private List<CalendarEventSO> _calendarEvents = new List<CalendarEventSO>();

        [Header("调试")]
        [SerializeField] private bool _debugMode;

        // 当前时间
        private GameDate _currentDate;
        private GameTime _currentTime;
        private float _minuteAccumulator;

        // 事件状态
        private readonly Dictionary<string, CalendarEventState> _eventStates = new Dictionary<string, CalendarEventState>();
        private readonly Dictionary<string, CalendarEventSO> _eventLookup = new Dictionary<string, CalendarEventSO>();

        // 当前活跃事件
        private readonly List<CalendarEventData> _activeEvents = new List<CalendarEventData>();
        private readonly List<CalendarEventData> _tempEvents = new List<CalendarEventData>();

        #region Events

        public event Action<CalendarSystemEventArgs> OnCalendarEvent;

        #endregion

        #region Properties

        public GameDate CurrentDate => _currentDate;
        public GameTime CurrentTime => _currentTime;
        public Season CurrentSeason => _currentDate.Season;
        public int CurrentYear => _currentDate.Year;
        public int CurrentMonth => _currentDate.Month;
        public int CurrentDay => _currentDate.Day;
        public int CurrentHour => _currentTime.Hour;
        public bool IsTimeFlowing => _timeFlows;
        public IReadOnlyList<CalendarEventData> ActiveEvents => _activeEvents;

        #endregion

        #region ISaveable

        public string SaveKey => "CalendarManager";

        public void Register() => SaveSlotManager.Instance?.Register(this);
        public void Unregister() => SaveSlotManager.Instance?.Unregister(this);

        public object ExportSaveData()
        {
            return new CalendarSaveData
            {
                CurrentDate = _currentDate,
                CurrentTime = _currentTime,
                EventStates = new Dictionary<string, CalendarEventState>(_eventStates)
            };
        }

        public void ImportSaveData(object data)
        {
            if (data is not CalendarSaveData saveData) return;

            _currentDate = saveData.CurrentDate;
            _currentTime = saveData.CurrentTime;
            _eventStates.Clear();

            if (saveData.EventStates != null)
            {
                foreach (var kvp in saveData.EventStates)
                    _eventStates[kvp.Key] = kvp.Value;
            }

            RefreshActiveEvents();
        }

        public void ResetToDefault()
        {
            _currentDate = _startDate;
            _currentTime = _startTime;
            _eventStates.Clear();
            _minuteAccumulator = 0;
            RefreshActiveEvents();
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            BuildEventLookup();
            _currentDate = _startDate;
            _currentTime = _startTime;
        }

        private void Start()
        {
            Register();
            RefreshActiveEvents();
        }

        private void Update()
        {
            if (_timeFlows)
            {
                UpdateTime();
            }
        }

        protected override void OnDestroy()
        {
            Unregister();
            base.OnDestroy();
        }

        #endregion

        #region Public API - Time Control

        /// <summary>设置时间流逝</summary>
        public void SetTimeFlow(bool flowing) => _timeFlows = flowing;

        /// <summary>暂停时间</summary>
        public void PauseTime() => _timeFlows = false;

        /// <summary>恢复时间</summary>
        public void ResumeTime() => _timeFlows = true;

        /// <summary>设置时间流速</summary>
        public void SetTimeScale(float realSecondsPerGameMinute)
        {
            _realSecondsPerGameMinute = Mathf.Max(0.01f, realSecondsPerGameMinute);
        }

        /// <summary>推进时间</summary>
        public void AdvanceTime(int minutes)
        {
            for (int i = 0; i < minutes; i++)
                AdvanceMinute();
        }

        /// <summary>推进天数</summary>
        public void AdvanceDays(int days)
        {
            for (int i = 0; i < days; i++)
                AdvanceDay();
        }

        /// <summary>设置日期</summary>
        public void SetDate(GameDate date)
        {
            var oldDate = _currentDate;
            _currentDate = date;

            if (oldDate != _currentDate)
            {
                OnDayChanged(oldDate, _currentDate);
            }
        }

        /// <summary>设置时间</summary>
        public void SetTime(GameTime time)
        {
            _currentTime = time;
        }

        #endregion

        #region Public API - Events

        /// <summary>获取指定日期的事件</summary>
        public void GetEventsOnDate(GameDate date, List<CalendarEventData> results)
        {
            results.Clear();
            foreach (var evt in _calendarEvents)
            {
                if (evt == null) continue;
                if (evt.EventData.IsActiveOn(date))
                    results.Add(evt.EventData);
            }
        }

        /// <summary>获取即将开始的事件</summary>
        public void GetUpcomingEvents(int daysAhead, List<CalendarEventData> results)
        {
            results.Clear();
            var targetDate = _currentDate.AddDays(daysAhead);

            foreach (var evt in _calendarEvents)
            {
                if (evt == null) continue;
                var data = evt.EventData;

                if (data.StartDate > _currentDate && data.StartDate <= targetDate)
                {
                    results.Add(data);
                }
            }
        }

        /// <summary>检查事件是否活跃</summary>
        public bool IsEventActive(string eventId)
        {
            foreach (var evt in _activeEvents)
            {
                if (evt.EventId == eventId)
                    return true;
            }
            return false;
        }

        /// <summary>领取事件奖励</summary>
        public bool ClaimEventReward(string eventId)
        {
            if (!_eventStates.TryGetValue(eventId, out var state))
                return false;

            if (state.RewardClaimed)
                return false;

            state.RewardClaimed = true;

            // TODO: 发放奖励
            Log($"领取事件奖励: {eventId}");

            return true;
        }

        #endregion

        #region Internal

        private void BuildEventLookup()
        {
            _eventLookup.Clear();
            foreach (var evt in _calendarEvents)
            {
                if (evt != null)
                    _eventLookup[evt.EventData.EventId] = evt;
            }
        }

        private void UpdateTime()
        {
            _minuteAccumulator += Time.deltaTime;

            while (_minuteAccumulator >= _realSecondsPerGameMinute)
            {
                _minuteAccumulator -= _realSecondsPerGameMinute;
                AdvanceMinute();
            }
        }

        private void AdvanceMinute()
        {
            _currentTime.Minute++;

            if (_currentTime.Minute >= 60)
            {
                _currentTime.Minute = 0;
                _currentTime.Hour++;

                if (_currentTime.Hour >= 24)
                {
                    _currentTime.Hour = 0;
                    AdvanceDay();
                }
            }
        }

        private void AdvanceDay()
        {
            var oldDate = _currentDate;
            _currentDate = _currentDate.AddDays(1);
            OnDayChanged(oldDate, _currentDate);
        }

        private void OnDayChanged(GameDate oldDate, GameDate newDate)
        {
            Log($"日期变更: {oldDate} -> {newDate}");

            OnCalendarEvent?.Invoke(CalendarSystemEventArgs.DayChanged(oldDate, newDate));

            // 检查季节变化
            if (oldDate.Season != newDate.Season)
            {
                OnCalendarEvent?.Invoke(CalendarSystemEventArgs.SeasonChanged(oldDate.Season, newDate.Season, newDate));
                Log($"季节变更: {oldDate.Season} -> {newDate.Season}");
            }

            // 刷新活跃事件
            RefreshActiveEvents();

            // 检查事件提醒
            CheckEventReminders();

            // 触发成就
#if ZEROENGINE_NARRATIVE
            Achievement.AchievementManager.Instance?.TriggerEvent("DayPassed", newDate.TotalDays);
#endif
        }

        private void RefreshActiveEvents()
        {
            var previousActive = new HashSet<string>();
            foreach (var evt in _activeEvents)
                previousActive.Add(evt.EventId);

            _activeEvents.Clear();

            foreach (var evt in _calendarEvents)
            {
                if (evt == null) continue;
                var data = evt.EventData;

                if (data.IsActiveOn(_currentDate))
                {
                    _activeEvents.Add(data);

                    // 新激活的事件
                    if (!previousActive.Contains(data.EventId))
                    {
                        OnEventStarted(data);
                    }
                }
                else
                {
                    // 结束的事件
                    if (previousActive.Contains(data.EventId))
                    {
                        OnEventEnded(data);
                    }
                }
            }
        }

        private void OnEventStarted(CalendarEventData evt)
        {
            var state = GetOrCreateState(evt.EventId);
            state.State = EventState.Active;
            state.TimesTriggered++;
            state.LastTriggeredTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            OnCalendarEvent?.Invoke(CalendarSystemEventArgs.EventStarted(evt, _currentDate));
            Log($"事件开始: {evt.DisplayName}");
        }

        private void OnEventEnded(CalendarEventData evt)
        {
            var state = GetOrCreateState(evt.EventId);
            state.State = EventState.Ended;

            OnCalendarEvent?.Invoke(CalendarSystemEventArgs.EventEnded(evt, _currentDate));
            Log($"事件结束: {evt.DisplayName}");
        }

        private void CheckEventReminders()
        {
            foreach (var evt in _calendarEvents)
            {
                if (evt == null) continue;
                var data = evt.EventData;

                if (data.ReminderDaysBefore > 0)
                {
                    var reminderDate = _currentDate.AddDays(data.ReminderDaysBefore);
                    if (reminderDate == data.StartDate)
                    {
                        OnCalendarEvent?.Invoke(CalendarSystemEventArgs.Reminder(data, _currentDate));
                        Log($"事件提醒: {data.DisplayName} 将在 {data.ReminderDaysBefore} 天后开始");
                    }
                }
            }
        }

        private CalendarEventState GetOrCreateState(string eventId)
        {
            if (!_eventStates.TryGetValue(eventId, out var state))
            {
                state = new CalendarEventState { EventId = eventId };
                _eventStates[eventId] = state;
            }
            return state;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("ZEROENGINE_DEBUG")]
        private void Log(string message)
        {
            if (_debugMode) Debug.Log($"[Calendar] {message}");
        }

        #endregion
    }

    #region ScriptableObjects

    [CreateAssetMenu(fileName = "New Calendar Event", menuName = "ZeroEngine/Calendar/Calendar Event")]
    public class CalendarEventSO : ScriptableObject
    {
        public CalendarEventData EventData = new CalendarEventData();

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(EventData.EventId))
                EventData.EventId = name;
        }
    }

    #endregion

    #region Save Data

    [Serializable]
    public class CalendarSaveData
    {
        public GameDate CurrentDate;
        public GameTime CurrentTime;
        public Dictionary<string, CalendarEventState> EventStates;
    }

    #endregion
}