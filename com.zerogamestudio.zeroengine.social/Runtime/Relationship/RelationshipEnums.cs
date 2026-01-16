using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Relationship
{
    /// <summary>
    /// 好感度等级
    /// </summary>
    public enum RelationshipLevel
    {
        /// <summary>陌生人</summary>
        Stranger = 0,

        /// <summary>熟人</summary>
        Acquaintance = 1,

        /// <summary>朋友</summary>
        Friend = 2,

        /// <summary>好友</summary>
        CloseFriend = 3,

        /// <summary>挚友</summary>
        BestFriend = 4,

        /// <summary>恋人</summary>
        Lover = 5,

        /// <summary>伴侣</summary>
        Partner = 6
    }

    /// <summary>
    /// 好感度事件类型
    /// </summary>
    public enum RelationshipEventType
    {
        /// <summary>好感度变化</summary>
        PointsChanged,

        /// <summary>等级提升</summary>
        LevelUp,

        /// <summary>等级下降</summary>
        LevelDown,

        /// <summary>收到礼物</summary>
        GiftReceived,

        /// <summary>对话完成</summary>
        DialogueCompleted,

        /// <summary>触发特殊事件</summary>
        SpecialEvent
    }

    /// <summary>
    /// 礼物偏好
    /// </summary>
    public enum GiftPreference
    {
        /// <summary>最爱</summary>
        Loved = 3,

        /// <summary>喜欢</summary>
        Liked = 2,

        /// <summary>普通</summary>
        Neutral = 1,

        /// <summary>不喜欢</summary>
        Disliked = 0,

        /// <summary>讨厌</summary>
        Hated = -1
    }

    /// <summary>
    /// NPC类型
    /// </summary>
    public enum NpcType
    {
        /// <summary>普通NPC</summary>
        Normal,

        /// <summary>可攻略角色</summary>
        Romanceable,

        /// <summary>商人</summary>
        Merchant,

        /// <summary>任务NPC</summary>
        Quest,

        /// <summary>同伴</summary>
        Companion
    }

    /// <summary>
    /// 好感度进度数据
    /// </summary>
    [Serializable]
    public class RelationshipProgress
    {
        /// <summary>NPC ID</summary>
        public string NpcId;

        /// <summary>当前好感度点数</summary>
        public int Points;

        /// <summary>当前等级</summary>
        public RelationshipLevel Level;

        /// <summary>今日送礼次数</summary>
        public int GiftCountToday;

        /// <summary>今日对话次数</summary>
        public int TalkCountToday;

        /// <summary>最后互动日期</summary>
        public string LastInteractionDate;

        /// <summary>已触发事件</summary>
        public List<string> TriggeredEvents = new List<string>();

        /// <summary>自定义数据</summary>
        public Dictionary<string, string> CustomData = new Dictionary<string, string>();
    }

    /// <summary>
    /// 礼物数据
    /// </summary>
    [Serializable]
    public class GiftData
    {
        [Tooltip("物品")]
        public Inventory.InventoryItemSO Item;

        [Tooltip("偏好等级")]
        public GiftPreference Preference = GiftPreference.Neutral;

        [Tooltip("好感度变化")]
        public int PointsChange = 10;

        [Tooltip("特殊反应对话")]
        public string SpecialDialogueId;
    }

    /// <summary>
    /// 好感度等级阈值
    /// </summary>
    [Serializable]
    public class RelationshipThreshold
    {
        [Tooltip("等级")]
        public RelationshipLevel Level;

        [Tooltip("所需点数")]
        public int RequiredPoints;

        [Tooltip("解锁内容")]
        public List<string> UnlockIds = new List<string>();

        [Tooltip("解锁对话")]
        public List<string> UnlockDialogueIds = new List<string>();
    }

    /// <summary>
    /// 好感度事件参数
    /// </summary>
    public struct RelationshipEventArgs
    {
        public RelationshipEventType EventType;
        public string NpcId;
        public string NpcName;
        public int OldPoints;
        public int NewPoints;
        public RelationshipLevel OldLevel;
        public RelationshipLevel NewLevel;
        public Inventory.InventoryItemSO GiftItem;
        public GiftPreference GiftPreference;

        public static RelationshipEventArgs PointsChanged(string npcId, string npcName, int oldPoints, int newPoints)
        {
            return new RelationshipEventArgs
            {
                EventType = RelationshipEventType.PointsChanged,
                NpcId = npcId,
                NpcName = npcName,
                OldPoints = oldPoints,
                NewPoints = newPoints
            };
        }

        public static RelationshipEventArgs LevelUp(string npcId, string npcName, RelationshipLevel oldLevel, RelationshipLevel newLevel)
        {
            return new RelationshipEventArgs
            {
                EventType = RelationshipEventType.LevelUp,
                NpcId = npcId,
                NpcName = npcName,
                OldLevel = oldLevel,
                NewLevel = newLevel
            };
        }

        public static RelationshipEventArgs LevelDown(string npcId, string npcName, RelationshipLevel oldLevel, RelationshipLevel newLevel)
        {
            return new RelationshipEventArgs
            {
                EventType = RelationshipEventType.LevelDown,
                NpcId = npcId,
                NpcName = npcName,
                OldLevel = oldLevel,
                NewLevel = newLevel
            };
        }

        public static RelationshipEventArgs GiftReceived(string npcId, string npcName, Inventory.InventoryItemSO item, GiftPreference preference, int pointsChange)
        {
            return new RelationshipEventArgs
            {
                EventType = RelationshipEventType.GiftReceived,
                NpcId = npcId,
                NpcName = npcName,
                GiftItem = item,
                GiftPreference = preference,
                NewPoints = pointsChange
            };
        }
    }

    /// <summary>
    /// 对话选项影响
    /// </summary>
    [Serializable]
    public class DialogueEffect
    {
        [Tooltip("NPC ID")]
        public string NpcId;

        [Tooltip("好感度变化")]
        public int PointsChange;

        [Tooltip("触发事件ID")]
        public string TriggerEventId;
    }

    /// <summary>
    /// 特殊事件定义
    /// </summary>
    [Serializable]
    public class RelationshipEvent
    {
        [Tooltip("事件ID")]
        public string EventId;

        [Tooltip("事件名称")]
        public string DisplayName;

        [Tooltip("需要的等级")]
        public RelationshipLevel RequiredLevel;

        [Tooltip("需要的点数")]
        public int RequiredPoints;

        [Tooltip("是否一次性")]
        public bool OneTime = true;

        [Tooltip("触发的对话ID")]
        public string DialogueId;

        [Tooltip("解锁的内容")]
        public List<string> UnlockIds = new List<string>();
    }
}