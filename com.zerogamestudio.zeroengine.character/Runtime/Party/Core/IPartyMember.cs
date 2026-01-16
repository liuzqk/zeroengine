using System;
using UnityEngine;

namespace ZeroEngine.Party
{
    /// <summary>
    /// 队伍成员接口
    /// 任何可加入队伍的实体都应实现此接口
    /// </summary>
    public interface IPartyMember
    {
        /// <summary>
        /// 成员唯一标识符
        /// </summary>
        string MemberId { get; }

        /// <summary>
        /// 显示名称
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 成员类型 (玩家角色/NPC队友/召唤物等)
        /// </summary>
        PartyMemberType MemberType { get; }

        /// <summary>
        /// 是否存活
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// 是否可以行动 (非眩晕/石化等状态)
        /// </summary>
        bool CanAct { get; }

        /// <summary>
        /// 是否为玩家控制
        /// </summary>
        bool IsPlayerControlled { get; }

        /// <summary>
        /// 当前所在队伍索引 (出战/后备)
        /// -1 表示不在队伍中
        /// </summary>
        int PartySlotIndex { get; set; }

        /// <summary>
        /// 成员等级 (用于队伍平均等级计算)
        /// </summary>
        int Level { get; }

        /// <summary>
        /// Transform 引用 (用于阵型定位)
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// 加入队伍时调用
        /// </summary>
        void OnJoinParty(int slotIndex);

        /// <summary>
        /// 离开队伍时调用
        /// </summary>
        void OnLeaveParty();

        /// <summary>
        /// 切换到前排/后备时调用
        /// </summary>
        void OnSlotChanged(int oldSlot, int newSlot);
    }

    /// <summary>
    /// 队伍成员类型
    /// </summary>
    public enum PartyMemberType
    {
        /// <summary>玩家角色</summary>
        Player,

        /// <summary>NPC 队友</summary>
        Companion,

        /// <summary>雇佣兵</summary>
        Mercenary,

        /// <summary>召唤物</summary>
        Summon,

        /// <summary>临时成员 (任务NPC)</summary>
        Temporary,

        /// <summary>宠物</summary>
        Pet
    }

    /// <summary>
    /// 队伍成员基类 - 提供默认实现
    /// </summary>
    public abstract class PartyMemberBase : MonoBehaviour, IPartyMember
    {
        [Header("成员信息")]
        [SerializeField] protected string _memberId;
        [SerializeField] protected string _displayName;
        [SerializeField] protected PartyMemberType _memberType = PartyMemberType.Companion;
        [SerializeField] protected bool _isPlayerControlled;
        [SerializeField] protected int _level = 1;

        protected int _partySlotIndex = -1;

        public virtual string MemberId => _memberId;
        public virtual string DisplayName => _displayName;
        public virtual PartyMemberType MemberType => _memberType;
        public virtual bool IsAlive => true;
        public virtual bool CanAct => IsAlive;
        public virtual bool IsPlayerControlled => _isPlayerControlled;
        public virtual int Level => _level;
        public Transform Transform => transform;

        public int PartySlotIndex
        {
            get => _partySlotIndex;
            set => _partySlotIndex = value;
        }

        public virtual void OnJoinParty(int slotIndex)
        {
            _partySlotIndex = slotIndex;
        }

        public virtual void OnLeaveParty()
        {
            _partySlotIndex = -1;
        }

        public virtual void OnSlotChanged(int oldSlot, int newSlot)
        {
            _partySlotIndex = newSlot;
        }

        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(_memberId))
            {
                _memberId = System.Guid.NewGuid().ToString();
            }
        }
    }
}
