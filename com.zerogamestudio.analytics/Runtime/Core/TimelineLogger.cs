using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ZGS.Analytics
{
    /// <summary>
    /// Timeline 日志记录器 - 本地缓存游戏过程流水账
    /// 用于 Bug 复现和崩溃报告
    /// </summary>
    public static class TimelineLogger
    {
        private const int MaxEntries = 500;
        
        private static readonly List<TimelineEntry> _entries = new();
        private static readonly object _lock = new();

        /// <summary>
        /// 记录一条 Timeline 日志
        /// </summary>
        public static void Log(string eventName, Dictionary<string, object> data = null)
        {
            lock (_lock)
            {
                _entries.Add(new TimelineEntry
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Event = eventName,
                    // 复制 Dictionary，避免外部共享引用导致数据被覆盖
                    Data = data != null ? new Dictionary<string, object>(data) : null
                });

                // 防止内存溢出
                while (_entries.Count > MaxEntries)
                    _entries.RemoveAt(0);
            }
        }

        /// <summary>
        /// 清空 Timeline
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _entries.Clear();
            }
        }

        /// <summary>
        /// 获取 Timeline 条目数
        /// </summary>
        public static int Count
        {
            get
            {
                lock (_lock)
                {
                    return _entries.Count;
                }
            }
        }

        /// <summary>
        /// 导出为 JSON 数组
        /// </summary>
        public static string ToJson()
        {
            lock (_lock)
            {
                if (_entries.Count == 0) return "[]";
                
                var sb = new StringBuilder(4096);
                sb.Append("[");
                
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append(_entries[i].ToJson());
                }
                
                sb.Append("]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// 获取当前所有条目的副本（用于存档/快照）
        /// </summary>
        public static TimelineEntry[] GetSnapshot()
        {
            lock (_lock)
            {
                return _entries.ToArray();
            }
        }

        /// <summary>
        /// 还原快照
        /// </summary>
        public static void RestoreSnapshot(IEnumerable<TimelineEntry> snapshot)
        {
            if (snapshot == null) return;
            lock (_lock)
            {
                _entries.Clear();
                _entries.AddRange(snapshot);
            }
        }

        /// <summary>
        /// 导出为 List（用于直接附加到事件 props）
        /// </summary>
        public static List<Dictionary<string, object>> ToList()
        {
            lock (_lock)
            {
                var result = new List<Dictionary<string, object>>(_entries.Count);
                foreach (var entry in _entries)
                {
                    result.Add(entry.ToDictionary());
                }
                return result;
            }
        }

        /// <summary>
        /// Timeline 条目
        /// </summary>
        [Serializable]
        public class TimelineEntry
        {
            public long Timestamp;
            public string Event;
            public Dictionary<string, object> Data;

            public Dictionary<string, object> ToDictionary()
            {
                var dict = new Dictionary<string, object>
                {
                    ["ts"] = Timestamp,
                    ["event"] = Event
                };
                
                if (Data != null)
                {
                    foreach (var kv in Data)
                        dict[kv.Key] = kv.Value;
                }
                
                return dict;
            }

            public string ToJson()
            {
                var sb = new StringBuilder(256);
                sb.Append("{");
                sb.AppendFormat("\"ts\":{0},\"event\":\"{1}\"", Timestamp, Event);
                
                if (Data != null && Data.Count > 0)
                {
                    foreach (var kv in Data)
                    {
                        sb.Append(",");
                        sb.AppendFormat("\"{0}\":", kv.Key);
                        AppendValue(sb, kv.Value);
                    }
                }
                
                sb.Append("}");
                return sb.ToString();
            }

            private static void AppendValue(StringBuilder sb, object value)
            {
                switch (value)
                {
                    case string s:
                        sb.Append('"');
                        foreach (char c in s)
                        {
                            switch (c)
                            {
                                case '\\': sb.Append("\\\\"); break;
                                case '"': sb.Append("\\\""); break;
                                case '\n': sb.Append("\\n"); break;
                                case '\r': sb.Append("\\r"); break;
                                case '\t': sb.Append("\\t"); break;
                                case '\b': sb.Append("\\b"); break;
                                case '\f': sb.Append("\\f"); break;
                                default:
                                    if (c < 32)
                                        sb.AppendFormat("\\u{0:x4}", (int)c);
                                    else
                                        sb.Append(c);
                                    break;
                            }
                        }
                        sb.Append('"');
                        break;
                    case bool b:
                        sb.Append(b ? "true" : "false");
                        break;
                    case float f:
                        sb.Append(f.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    case double d:
                        sb.Append(d.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    case int i:
                        sb.Append(i);
                        break;
                    case long l:
                        sb.Append(l);
                        break;
                    default:
                        // 其他类型转字符串并转义
                        sb.Append('"');
                        string str = value.ToString();
                        foreach (char c in str)
                        {
                            switch (c)
                            {
                                case '\\': sb.Append("\\\\"); break;
                                case '"': sb.Append("\\\""); break;
                                case '\n': sb.Append("\\n"); break;
                                case '\r': sb.Append("\\r"); break;
                                default:
                                    if (c < 32)
                                        sb.AppendFormat("\\u{0:x4}", (int)c);
                                    else
                                        sb.Append(c);
                                    break;
                            }
                        }
                        sb.Append('"');
                        break;
                }
            }
        }
    }
}
