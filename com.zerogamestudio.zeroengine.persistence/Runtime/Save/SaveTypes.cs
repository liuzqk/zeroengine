using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Save
{
    /// <summary>
    /// 可存档系统接口
    /// 实现此接口的系统可被 SaveSlotManager 自动管理
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// 系统唯一标识符
        /// </summary>
        string SaveKey { get; }

        /// <summary>
        /// 导出存档数据
        /// </summary>
        object ExportSaveData();

        /// <summary>
        /// 导入存档数据
        /// </summary>
        void ImportSaveData(object data);

        /// <summary>
        /// 重置为初始状态
        /// </summary>
        void ResetToDefault();
    }

    /// <summary>
    /// 存档槽位元信息
    /// </summary>
    [Serializable]
    public class SaveSlotMeta
    {
        /// <summary>槽位索引 (0-7 为普通槽位, -1 为自动存档)</summary>
        public int SlotIndex;

        /// <summary>存档时间戳</summary>
        public long TimestampTicks;

        /// <summary>游戏时长 (秒)</summary>
        public float PlayTimeSeconds;

        /// <summary>当前场景名</summary>
        public string SceneName;

        /// <summary>玩家名称</summary>
        public string PlayerName;

        /// <summary>玩家等级</summary>
        public int PlayerLevel;

        /// <summary>存档版本 (用于迁移)</summary>
        public int SaveVersion = 1;

        /// <summary>是否有效</summary>
        public bool IsValid;

        /// <summary>自定义元数据</summary>
        public Dictionary<string, string> CustomMeta;

        /// <summary>存档时间</summary>
        public DateTime Timestamp
        {
            get => new DateTime(TimestampTicks);
            set => TimestampTicks = value.Ticks;
        }

        /// <summary>格式化的游戏时长</summary>
        public string FormattedPlayTime
        {
            get
            {
                var ts = TimeSpan.FromSeconds(PlayTimeSeconds);
                return ts.TotalHours >= 1
                    ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                    : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
            }
        }

        /// <summary>格式化的存档时间</summary>
        public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm");

        public SaveSlotMeta()
        {
            CustomMeta = new Dictionary<string, string>();
        }

        public SaveSlotMeta(int slotIndex) : this()
        {
            SlotIndex = slotIndex;
        }
    }

    /// <summary>
    /// 完整存档数据
    /// </summary>
    [Serializable]
    public class SaveSlotData
    {
        /// <summary>槽位元信息</summary>
        public SaveSlotMeta Meta;

        /// <summary>各系统存档数据</summary>
        public Dictionary<string, object> SystemData;

        /// <summary>截图数据 (PNG bytes)</summary>
        public byte[] ScreenshotData;

        public SaveSlotData()
        {
            Meta = new SaveSlotMeta();
            SystemData = new Dictionary<string, object>();
        }

        public SaveSlotData(int slotIndex) : this()
        {
            Meta = new SaveSlotMeta(slotIndex);
        }
    }

    /// <summary>
    /// 自动存档触发类型
    /// </summary>
    [Flags]
    public enum AutoSaveTrigger
    {
        None = 0,
        /// <summary>定时自动存档</summary>
        Interval = 1 << 0,
        /// <summary>场景切换时</summary>
        SceneChange = 1 << 1,
        /// <summary>任务完成时</summary>
        QuestComplete = 1 << 2,
        /// <summary>重要事件时</summary>
        ImportantEvent = 1 << 3,
        /// <summary>游戏暂停/退出时</summary>
        OnPause = 1 << 4,

        /// <summary>默认配置</summary>
        Default = Interval | SceneChange | OnPause
    }

    /// <summary>
    /// 存档系统配置
    /// </summary>
    [Serializable]
    public class SaveSystemConfig
    {
        /// <summary>最大槽位数 (不含自动存档)</summary>
        public int MaxSlots = 8;

        /// <summary>启用自动存档</summary>
        public bool EnableAutoSave = true;

        /// <summary>自动存档间隔 (秒)</summary>
        public float AutoSaveInterval = 300f; // 5 minutes

        /// <summary>自动存档触发条件</summary>
        public AutoSaveTrigger AutoSaveTriggers = AutoSaveTrigger.Default;

        /// <summary>截图宽度</summary>
        public int ScreenshotWidth = 320;

        /// <summary>截图高度</summary>
        public int ScreenshotHeight = 180;

        /// <summary>截图质量 (0-100)</summary>
        public int ScreenshotQuality = 75;

        /// <summary>存档文件前缀</summary>
        public string SaveFilePrefix = "SaveSlot";

        /// <summary>自动存档文件名</summary>
        public string AutoSaveFileName = "AutoSave.es3";

        /// <summary>获取槽位文件名</summary>
        public string GetSlotFileName(int slotIndex)
        {
            return slotIndex < 0
                ? AutoSaveFileName
                : $"{SaveFilePrefix}_{slotIndex}.es3";
        }
    }

    /// <summary>
    /// 存档事件参数
    /// </summary>
    public struct SaveEventArgs
    {
        public int SlotIndex;
        public SaveSlotMeta Meta;
        public bool Success;
        public string ErrorMessage;

        public SaveEventArgs(int slotIndex, SaveSlotMeta meta, bool success, string error = null)
        {
            SlotIndex = slotIndex;
            Meta = meta;
            Success = success;
            ErrorMessage = error;
        }
    }
}
