using System;

namespace ZeroEngine.Inventory
{
    /// <summary>
    /// 槽位变化事件参数 (v1.2.0+)
    /// </summary>
    public struct SlotChangedEventArgs
    {
        public int SlotIndex;
        public InventorySlot Slot;
        public InventoryEventType EventType;
        public InventoryItemSO Item;
        public int OldAmount;
        public int NewAmount;
        public int DeltaAmount;

        public SlotChangedEventArgs(int index, InventorySlot slot, InventoryEventType type,
            InventoryItemSO item, int oldAmt, int newAmt)
        {
            SlotIndex = index;
            Slot = slot;
            EventType = type;
            Item = item;
            OldAmount = oldAmt;
            NewAmount = newAmt;
            DeltaAmount = newAmt - oldAmt;
        }
    }

    [Serializable]
    public class InventorySlot
    {
        public string ItemId;
        public int Amount;

        // Runtime cache data (not saved directly, usually re-linked on load)
        [NonSerialized] public InventoryItemSO ItemData;

        // v1.2.0+: Slot index (set by InventoryManager)
        [NonSerialized] public int SlotIndex;

        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Amount <= 0;

        /// <summary>
        /// 是否已满 (v1.2.0+)
        /// </summary>
        public bool IsFull => !IsEmpty && ItemData != null && Amount >= ItemData.MaxStack;

        /// <summary>
        /// 剩余可堆叠空间 (v1.2.0+)
        /// </summary>
        public int AvailableSpace => IsEmpty ? 0 : (ItemData != null ? ItemData.MaxStack - Amount : 0);

        public InventorySlot()
        {
            Clear();
        }

        public InventorySlot(InventoryItemSO item, int amount)
        {
            Set(item, amount);
        }

        public void Set(InventoryItemSO item, int amount)
        {
            ItemData = item;
            ItemId = item != null ? item.Id : string.Empty;
            Amount = amount;
        }

        public void AddAmount(int value)
        {
            Amount += value;
            if (ItemData != null && Amount > ItemData.MaxStack)
            {
                Amount = ItemData.MaxStack;
            }
        }

        public void RemoveAmount(int value)
        {
            Amount -= value;
            if (Amount <= 0) Clear();
        }

        public void Clear()
        {
            ItemId = string.Empty;
            Amount = 0;
            ItemData = null;
        }

        /// <summary>
        /// 复制槽位数据 (v1.2.0+)
        /// </summary>
        public InventorySlot Clone()
        {
            return new InventorySlot
            {
                ItemId = this.ItemId,
                Amount = this.Amount,
                ItemData = this.ItemData,
                SlotIndex = this.SlotIndex
            };
        }

        /// <summary>
        /// 与另一个槽位交换内容 (v1.2.0+)
        /// </summary>
        public void SwapWith(InventorySlot other)
        {
            var tempId = ItemId;
            var tempAmount = Amount;
            var tempData = ItemData;

            ItemId = other.ItemId;
            Amount = other.Amount;
            ItemData = other.ItemData;

            other.ItemId = tempId;
            other.Amount = tempAmount;
            other.ItemData = tempData;
        }

        /// <summary>
        /// 尝试合并到另一个槽位 (v1.2.0+)
        /// 返回剩余数量
        /// </summary>
        public int TryMergeInto(InventorySlot targetSlot)
        {
            if (IsEmpty || targetSlot == null) return Amount;
            if (targetSlot.IsEmpty)
            {
                targetSlot.Set(ItemData, Amount);
                Clear();
                return 0;
            }

            if (targetSlot.ItemId != ItemId) return Amount;
            if (targetSlot.IsFull) return Amount;

            int space = targetSlot.AvailableSpace;
            int toMove = Math.Min(space, Amount);

            targetSlot.AddAmount(toMove);
            RemoveAmount(toMove);

            return Amount;
        }
    }
}
