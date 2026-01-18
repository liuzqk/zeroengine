using System;

namespace ZeroEngine.Dungeon.Events
{
    /// <summary>
    /// 事件节点基类
    /// </summary>
    public abstract class EventNodeBase
    {
        /// <summary>
        /// 事件ID
        /// </summary>
        public string EventId { get; }

        /// <summary>
        /// 事件标题
        /// </summary>
        public string Title { get; protected set; }

        /// <summary>
        /// 事件描述
        /// </summary>
        public string Description { get; protected set; }

        /// <summary>
        /// 事件选项
        /// </summary>
        public EventOption[] Options { get; protected set; }

        protected EventNodeBase(string eventId)
        {
            EventId = eventId;
        }

        /// <summary>
        /// 执行选项
        /// </summary>
        public abstract EventResult ExecuteOption(int optionIndex, object context);
    }

    /// <summary>
    /// 事件选项
    /// </summary>
    [Serializable]
    public class EventOption
    {
        /// <summary>
        /// 选项文本
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 选项描述/提示
        /// </summary>
        public string Hint { get; set; }

        /// <summary>
        /// 是否需要特定条件
        /// </summary>
        public bool RequiresCondition { get; set; }

        /// <summary>
        /// 条件描述
        /// </summary>
        public string ConditionDescription { get; set; }
    }

    /// <summary>
    /// 事件结果
    /// </summary>
    [Serializable]
    public class EventResult
    {
        /// <summary>
        /// 结果类型
        /// </summary>
        public EventResultType Type { get; set; }

        /// <summary>
        /// 结果描述文本
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 金币变化
        /// </summary>
        public int GoldChange { get; set; }

        /// <summary>
        /// 生命值变化（百分比）
        /// </summary>
        public float HealthChange { get; set; }

        /// <summary>
        /// 获得的物品ID列表
        /// </summary>
        public string[] ItemIds { get; set; }

        /// <summary>
        /// 触发战斗
        /// </summary>
        public bool TriggerBattle { get; set; }

        /// <summary>
        /// 战斗敌人ID
        /// </summary>
        public string BattleEnemyId { get; set; }
    }

    /// <summary>
    /// 事件结果类型
    /// </summary>
    public enum EventResultType
    {
        /// <summary>
        /// 成功/正面
        /// </summary>
        Success,

        /// <summary>
        /// 失败/负面
        /// </summary>
        Failure,

        /// <summary>
        /// 中立
        /// </summary>
        Neutral,

        /// <summary>
        /// 触发战斗
        /// </summary>
        Battle
    }
}
