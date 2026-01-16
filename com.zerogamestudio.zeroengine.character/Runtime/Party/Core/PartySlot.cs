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

    /// <summary>
    /// 队伍配置 - 定义队伍结构
    /// </summary>
    [CreateAssetMenu(fileName = "PartyConfig", menuName = "ZeroEngine/Party/Party Config")]
    public class PartyConfigSO : ScriptableObject
    {
        [Header("出战队伍")]
        [Tooltip("出战队伍最大人数")]
        [Range(1, 12)]
        public int MaxActiveMembers = 4;

        [Header("后备队伍")]
        [Tooltip("后备队伍最大人数")]
        [Range(0, 20)]
        public int MaxReserveMembers = 4;

        [Header("其他槽位")]
        [Tooltip("临时槽位数量 (战斗召唤等)")]
        [Range(0, 4)]
        public int MaxTemporarySlots = 2;

        [Tooltip("宠物槽位数量")]
        [Range(0, 4)]
        public int MaxPetSlots = 1;

        [Header("规则")]
        [Tooltip("是否允许战斗中切换")]
        public bool AllowSwitchInCombat = true;

        [Tooltip("战斗中切换消耗行动")]
        public bool SwitchCostsAction = true;

        [Tooltip("是否允许重复成员 (分身等)")]
        public bool AllowDuplicateMembers = false;

        /// <summary>
        /// 总槽位数
        /// </summary>
        public int TotalSlots => MaxActiveMembers + MaxReserveMembers + MaxTemporarySlots + MaxPetSlots;
    }
}
