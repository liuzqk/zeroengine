namespace ZeroEngine.Inventory
{
    public enum InventoryItemType
    {
        Equip,
        Consumable,
        Material,
        Quest,
        Other
    }

    /// <summary>
    /// 物品分类 (v1.2.0+)
    /// </summary>
    public enum ItemCategory
    {
        None = 0,
        Weapon,
        Armor,
        Accessory,
        Potion,
        Food,
        CraftingMaterial,
        QuestItem,
        Currency,
        Miscellaneous
    }

    /// <summary>
    /// 物品稀有度 (v1.2.0+)
    /// </summary>
    public enum ItemRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
        Mythic = 5
    }

    /// <summary>
    /// 背包事件类型 (v1.2.0+)
    /// </summary>
    public enum InventoryEventType
    {
        ItemAdded,      // 物品添加
        ItemRemoved,    // 物品移除
        ItemUsed,       // 物品使用
        SlotChanged,    // 槽位变化
        SlotSwapped,    // 槽位交换
        InventoryFull   // 背包已满
    }
}
