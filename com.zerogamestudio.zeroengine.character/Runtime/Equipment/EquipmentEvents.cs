using System;

namespace ZeroEngine.Equipment
{
    /// <summary>
    /// 装备事件参数
    /// </summary>
    public struct EquipmentEventArgs
    {
        /// <summary>事件类型</summary>
        public EquipmentEventType EventType;

        /// <summary>相关槽位</summary>
        public EquipmentSlotType SlotType;

        /// <summary>装备实例（可能为 null）</summary>
        public EquipmentInstance Equipment;

        /// <summary>旧装备（换装时）</summary>
        public EquipmentInstance OldEquipment;

        public EquipmentEventArgs(EquipmentEventType eventType, EquipmentSlotType slotType,
            EquipmentInstance equipment, EquipmentInstance oldEquipment = null)
        {
            EventType = eventType;
            SlotType = slotType;
            Equipment = equipment;
            OldEquipment = oldEquipment;
        }

        public static EquipmentEventArgs Equipped(EquipmentSlotType slot, EquipmentInstance equipment, EquipmentInstance old = null)
            => new EquipmentEventArgs(EquipmentEventType.Equipped, slot, equipment, old);

        public static EquipmentEventArgs Unequipped(EquipmentSlotType slot, EquipmentInstance equipment)
            => new EquipmentEventArgs(EquipmentEventType.Unequipped, slot, equipment);

        public static EquipmentEventArgs Enhanced(EquipmentSlotType slot, EquipmentInstance equipment)
            => new EquipmentEventArgs(EquipmentEventType.Enhanced, slot, equipment);
    }

    /// <summary>
    /// 套装事件参数
    /// </summary>
    public struct SetEventArgs
    {
        /// <summary>套装定义</summary>
        public EquipmentSetSO Set;

        /// <summary>激活的件数阈值</summary>
        public int PieceThreshold;

        /// <summary>是否激活</summary>
        public bool Activated;

        public SetEventArgs(EquipmentSetSO set, int pieceThreshold, bool activated)
        {
            Set = set;
            PieceThreshold = pieceThreshold;
            Activated = activated;
        }
    }

    /// <summary>
    /// 强化事件参数
    /// </summary>
    public struct EnhanceEventArgs
    {
        /// <summary>装备实例</summary>
        public EquipmentInstance Equipment;

        /// <summary>强化结果</summary>
        public EnhanceResult Result;

        /// <summary>旧等级</summary>
        public int OldLevel;

        /// <summary>新等级</summary>
        public int NewLevel;

        public EnhanceEventArgs(EquipmentInstance equipment, EnhanceResult result, int oldLevel, int newLevel)
        {
            Equipment = equipment;
            Result = result;
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }
}
