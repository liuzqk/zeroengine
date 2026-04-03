using System;
using UnityEngine;

namespace ZeroEngine.Party
{
    /// <summary>
    /// 队伍槽位 - 定义队伍中的一个位置
    /// </summary>
    [Serializable]
    public class PartySlot
    {
        /// <summary>
        /// 槽位索引
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// 槽位类型
        /// </summary>
        public PartySlotType SlotType { get; private set; }

        /// <summary>
        /// 当前占用的成员
        /// </summary>
        public IPartyMember Member { get; private set; }

        /// <summary>
        /// 槽位是否空闲
        /// </summary>
        public bool IsEmpty => Member == null;

        /// <summary>
        /// 槽位是否被占用
        /// </summary>
        public bool IsOccupied => Member != null;

        /// <summary>
        /// 槽位是否锁定 (不可操作)
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PartySlot(int index, PartySlotType slotType)
        {
            Index = index;
            SlotType = slotType;
            Member = null;
            IsLocked = false;
        }

        /// <summary>
        /// 设置成员
        /// </summary>
        public bool SetMember(IPartyMember member)
        {
            if (IsLocked)
            {
                Debug.LogWarning($"[PartySlot] 槽位 {Index} 已锁定，无法设置成员");
                return false;
            }

            var oldMember = Member;
            Member = member;

            if (member != null)
            {
                member.PartySlotIndex = Index;
            }

            return true;
        }

        /// <summary>
        /// 清空槽位
        /// </summary>
        public IPartyMember Clear()
        {
            if (IsLocked)
            {
                Debug.LogWarning($"[PartySlot] 槽位 {Index} 已锁定，无法清空");
                return null;
            }

            var member = Member;
            Member = null;

            if (member != null)
            {
                member.PartySlotIndex = -1;
            }

            return member;
        }

        /// <summary>
        /// 与另一个槽位交换成员
        /// </summary>
        public bool SwapWith(PartySlot other)
        {
            if (other == null) return false;
            if (IsLocked || other.IsLocked)
            {
                Debug.LogWarning("[PartySlot] 无法交换锁定的槽位");
                return false;
            }

            var tempMember = Member;
            Member = other.Member;
            other.Member = tempMember;

            // 更新成员的槽位索引
            if (Member != null) Member.PartySlotIndex = Index;
            if (other.Member != null) other.Member.PartySlotIndex = other.Index;

            return true;
        }

        public override string ToString()
        {
            return $"[Slot {Index}] {SlotType}: {(IsEmpty ? "空" : Member.DisplayName)}";
        }
    }

    /// <summary>
    /// 队伍槽位类型
    /// </summary>
    public enum PartySlotType
    {
        /// <summary>前排 (出战)</summary>
        Active,

        /// <summary>后备 (替补)</summary>
        Reserve,

        /// <summary>临时 (战斗召唤)</summary>
        Temporary,

        /// <summary>宠物槽</summary>
        Pet
    }
}
