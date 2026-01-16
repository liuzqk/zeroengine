using System;
using System.Collections.Generic;

namespace ZeroEngine.Debugging
{
    /// <summary>
    /// 调试模块接口
    /// </summary>
    public interface IDebugModule
    {
        /// <summary>模块名称</summary>
        string ModuleName { get; }

        /// <summary>是否启用</summary>
        bool IsEnabled { get; set; }

        /// <summary>更新调试数据</summary>
        void Update();

        /// <summary>获取调试信息摘要</summary>
        string GetSummary();

        /// <summary>获取详细调试数据</summary>
        IEnumerable<DebugEntry> GetEntries();

        /// <summary>清空历史数据</summary>
        void Clear();
    }

    /// <summary>
    /// 调试条目
    /// </summary>
    public struct DebugEntry
    {
        public string Label;
        public string Value;
        public DebugEntryType Type;
        public float Timestamp;

        public DebugEntry(string label, string value, DebugEntryType type = DebugEntryType.Info)
        {
            Label = label;
            Value = value;
            Type = type;
            Timestamp = UnityEngine.Time.unscaledTime;
        }

        public static DebugEntry Info(string label, string value) =>
            new DebugEntry(label, value, DebugEntryType.Info);

        public static DebugEntry Warning(string label, string value) =>
            new DebugEntry(label, value, DebugEntryType.Warning);

        public static DebugEntry Error(string label, string value) =>
            new DebugEntry(label, value, DebugEntryType.Error);

        public static DebugEntry Success(string label, string value) =>
            new DebugEntry(label, value, DebugEntryType.Success);
    }

    /// <summary>
    /// 调试条目类型
    /// </summary>
    public enum DebugEntryType
    {
        Info,
        Warning,
        Error,
        Success
    }
}
