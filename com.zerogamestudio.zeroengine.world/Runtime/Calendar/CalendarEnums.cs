using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Calendar
{
    /// <summary>事件类型</summary>
    public enum CalendarEventType
    {
        OneTime,        // 一次性事件
        Daily,          // 每日事件
        Weekly,         // 每周事件
        Monthly,        // 每月事件
        Yearly,         // 每年事件
        Seasonal,       // 季节性事件
        Custom          // 自定义周期
    }

    /// <summary>季节</summary>
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    /// <summary>事件状态</summary>
    public enum EventState
    {
        Upcoming,       // 即将开始
        Active,         // 进行中
        Ended,          // 已结束
        Missed          // 已错过
    }

    /// <summary>日历系统事件类型</summary>
    public enum CalendarSystemEventType
    {
        DayChanged,
        WeekChanged,
        MonthChanged,
        SeasonChanged,
        YearChanged,
        EventStarted,
        EventEnded,
        EventReminder
    }

    /// <summary>游戏内日期</summary>
    [Serializable]
    public struct GameDate : IEquatable<GameDate>, IComparable<GameDate>
    {
        public int Year;
        public int Month;
        public int Day;

        public GameDate(int year, int month, int day)
        {
            Year = year;
            Month = Mathf.Clamp(month, 1, 12);
            Day = Mathf.Clamp(day, 1, 30);
        }

        public int TotalDays => Year * 360 + (Month - 1) * 30 + Day;

        public Season Season => (Season)((Month - 1) / 3);

        public int DayOfWeek => (TotalDays - 1) % 7;

        public GameDate AddDays(int days)
        {
            int total = TotalDays + days;
            int y = total / 360;
            int remaining = total % 360;
            int m = remaining / 30 + 1;
            int d = remaining % 30;
            if (d == 0) { d = 30; m--; }
            if (m == 0) { m = 12; y--; }
            return new GameDate(y, m, d);
        }

        public bool Equals(GameDate other) => Year == other.Year && Month == other.Month && Day == other.Day;
        public override bool Equals(object obj) => obj is GameDate other && Equals(other);
        public override int GetHashCode() => TotalDays;
        public int CompareTo(GameDate other) => TotalDays.CompareTo(other.TotalDays);

        public static bool operator ==(GameDate a, GameDate b) => a.Equals(b);
        public static bool operator !=(GameDate a, GameDate b) => !a.Equals(b);
        public static bool operator <(GameDate a, GameDate b) => a.TotalDays < b.TotalDays;
        public static bool operator >(GameDate a, GameDate b) => a.TotalDays > b.TotalDays;
        public static bool operator <=(GameDate a, GameDate b) => a.TotalDays <= b.TotalDays;
        public static bool operator >=(GameDate a, GameDate b) => a.TotalDays >= b.TotalDays;

        public override string ToString() => $"{Year}/{Month:D2}/{Day:D2}";
    }

    /// <summary>游戏内时间</summary>
    [Serializable]
    public struct GameTime
    {
        public int Hour;
        public int Minute;

        public GameTime(int hour, int minute)
        {
            Hour = Mathf.Clamp(hour, 0, 23);
            Minute = Mathf.Clamp(minute, 0, 59);
        }

        public int TotalMinutes => Hour * 60 + Minute;

        public override string ToString() => $"{Hour:D2}:{Minute:D2}";
    }

    /// <summary>日历事件数据</summary>
    [Serializable]
    public class CalendarEventData
    {
        public string EventId;
        public string DisplayName;
        [TextArea] public string Description;
        public Sprite Icon;

        public CalendarEventType Type = CalendarEventType.OneTime;
        public GameDate StartDate;
        public GameDate EndDate;
        public GameTime StartTime;
        public GameTime EndTime;

        // 周期配置
        public int RecurrenceInterval = 1;
        public List<int> RecurrenceDays = new List<int>(); // 周几 (0-6)

        // 条件
        public int RequiredLevel;
        public string RequiredQuestId;

        // 奖励
        public string RewardId;

        // 提醒
        public int ReminderDaysBefore = 1;

        public bool IsActiveOn(GameDate date)
        {
            if (date < StartDate) return false;

            switch (Type)
            {
                case CalendarEventType.OneTime:
                    return date >= StartDate && date <= EndDate;

                case CalendarEventType.Daily:
                    return true;

                case CalendarEventType.Weekly:
                    return RecurrenceDays.Contains(date.DayOfWeek);

                case CalendarEventType.Monthly:
                    return date.Day == StartDate.Day;

                case CalendarEventType.Yearly:
                    return date.Month == StartDate.Month && date.Day == StartDate.Day;

                case CalendarEventType.Seasonal:
                    return date.Season == StartDate.Season;

                default:
                    return false;
            }
        }
    }

    /// <summary>事件运行时状态</summary>
    [Serializable]
    public class CalendarEventState
    {
        public string EventId;
        public EventState State;
        public int TimesTriggered;
        public long LastTriggeredTimestamp;
        public bool RewardClaimed;
    }

    /// <summary>日历系统事件参数</summary>
    public class CalendarSystemEventArgs
    {
        public CalendarSystemEventType Type { get; private set; }
        public GameDate Date { get; private set; }
        public GameDate OldDate { get; private set; }
        public Season Season { get; private set; }
        public Season OldSeason { get; private set; }
        public CalendarEventData Event { get; private set; }

        public static CalendarSystemEventArgs DayChanged(GameDate oldDate, GameDate newDate)
            => new() { Type = CalendarSystemEventType.DayChanged, OldDate = oldDate, Date = newDate };

        public static CalendarSystemEventArgs SeasonChanged(Season oldSeason, Season newSeason, GameDate date)
            => new() { Type = CalendarSystemEventType.SeasonChanged, OldSeason = oldSeason, Season = newSeason, Date = date };

        public static CalendarSystemEventArgs EventStarted(CalendarEventData evt, GameDate date)
            => new() { Type = CalendarSystemEventType.EventStarted, Event = evt, Date = date };

        public static CalendarSystemEventArgs EventEnded(CalendarEventData evt, GameDate date)
            => new() { Type = CalendarSystemEventType.EventEnded, Event = evt, Date = date };

        public static CalendarSystemEventArgs Reminder(CalendarEventData evt, GameDate date)
            => new() { Type = CalendarSystemEventType.EventReminder, Event = evt, Date = date };
    }
}