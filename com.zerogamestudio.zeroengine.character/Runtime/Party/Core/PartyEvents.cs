using System;

namespace ZeroEngine.Party
{
    /// <summary>
    /// 队伍事件参数基类
    /// </summary>
    public abstract class PartyEventArgs : EventArgs
    {
        /// <summary>
        /// 事件发生时间
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    /// <summary>
    /// 成员加入队伍事件
    /// </summary>
    public readonly struct MemberJoinedEventArgs
    {
        public readonly IPartyMember Member;
        public readonly int SlotIndex;
        public readonly PartySlotType SlotType;

        public MemberJoinedEventArgs(IPartyMember member, int slotIndex, PartySlotType slotType)
        {
            Member = member;
            SlotIndex = slotIndex;
            SlotType = slotType;
        }
    }

    /// <summary>
    /// 成员离开队伍事件
    /// </summary>
    public readonly struct MemberLeftEventArgs
    {
        public readonly IPartyMember Member;
        public readonly int PreviousSlotIndex;
        public readonly LeaveReason Reason;

        public MemberLeftEventArgs(IPartyMember member, int previousSlot, LeaveReason reason)
        {
            Member = member;
            PreviousSlotIndex = previousSlot;
            Reason = reason;
        }
    }

    /// <summary>
    /// 离开原因
    /// </summary>
    public enum LeaveReason
    {
        /// <summary>正常移除</summary>
        Removed,

        /// <summary>死亡</summary>
        Death,

        /// <summary>逃跑</summary>
        Fled,

        /// <summary>任务结束 (临时成员)</summary>
        QuestCompleted,

        /// <summary>召唤结束</summary>
        SummonExpired,

        /// <summary>解雇</summary>
        Dismissed
    }

    /// <summary>
    /// 成员槽位变化事件
    /// </summary>
    public readonly struct SlotChangedEventArgs
    {
        public readonly IPartyMember Member;
        public readonly int OldSlotIndex;
        public readonly int NewSlotIndex;
        public readonly PartySlotType OldSlotType;
        public readonly PartySlotType NewSlotType;

        public SlotChangedEventArgs(
            IPartyMember member,
            int oldSlot, int newSlot,
            PartySlotType oldType, PartySlotType newType)
        {
            Member = member;
            OldSlotIndex = oldSlot;
            NewSlotIndex = newSlot;
            OldSlotType = oldType;
            NewSlotType = newType;
        }

        /// <summary>
        /// 是否从后备切换到出战
        /// </summary>
        public bool IsActivated => OldSlotType == PartySlotType.Reserve && NewSlotType == PartySlotType.Active;

        /// <summary>
        /// 是否从出战切换到后备
        /// </summary>
        public bool IsDeactivated => OldSlotType == PartySlotType.Active && NewSlotType == PartySlotType.Reserve;
    }

    /// <summary>
    /// 成员交换事件
    /// </summary>
    public readonly struct MembersSwappedEventArgs
    {
        public readonly IPartyMember MemberA;
        public readonly IPartyMember MemberB;
        public readonly int SlotA;
        public readonly int SlotB;

        public MembersSwappedEventArgs(IPartyMember memberA, IPartyMember memberB, int slotA, int slotB)
        {
            MemberA = memberA;
            MemberB = memberB;
            SlotA = slotA;
            SlotB = slotB;
        }
    }

    /// <summary>
    /// 队伍领袖变更事件
    /// </summary>
    public readonly struct LeaderChangedEventArgs
    {
        public readonly IPartyMember PreviousLeader;
        public readonly IPartyMember NewLeader;

        public LeaderChangedEventArgs(IPartyMember previous, IPartyMember newLeader)
        {
            PreviousLeader = previous;
            NewLeader = newLeader;
        }
    }

    /// <summary>
    /// 队伍状态变化事件
    /// </summary>
    public readonly struct PartyStateChangedEventArgs
    {
        public readonly PartyState OldState;
        public readonly PartyState NewState;

        public PartyStateChangedEventArgs(PartyState oldState, PartyState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    /// <summary>
    /// 队伍状态
    /// </summary>
    public enum PartyState
    {
        /// <summary>空闲 (非战斗)</summary>
        Idle,

        /// <summary>探索中</summary>
        Exploring,

        /// <summary>战斗中</summary>
        InCombat,

        /// <summary>对话中</summary>
        InDialogue,

        /// <summary>切换中 (正在调整队伍)</summary>
        Rearranging
    }
}
