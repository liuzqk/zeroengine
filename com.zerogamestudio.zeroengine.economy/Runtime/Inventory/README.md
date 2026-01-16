# ZeroEngine.Inventory API 文档

> **用途**: 本文档面向AI助手，提供Inventory模块的快速参考。
> **版本**: v1.2.0+
> **最后更新**: 2025-12-31

---

## 目录结构

```
Inventory/
├── InventoryEnums.cs    # 枚举定义
├── InventoryItemSO.cs   # 物品配置（ScriptableObject）
├── InventorySlot.cs     # 背包槽位
├── InventoryManager.cs  # 背包管理器（单例）
└── UI/
    ├── InventoryPanel.cs    # 背包面板
    └── InventorySlotUI.cs   # 槽位UI
```

---

## 枚举定义 (v1.2.0+)

```csharp
// 物品类型
public enum InventoryItemType { Equip, Consumable, Material, Quest, Other }

// 物品分类 (v1.2.0+)
public enum ItemCategory
{
    None, Weapon, Armor, Accessory, Potion, Food,
    CraftingMaterial, QuestItem, Currency, Miscellaneous
}

// 物品稀有度 (v1.2.0+)
public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }

// 事件类型 (v1.2.0+)
public enum InventoryEventType
{
    ItemAdded, ItemRemoved, ItemUsed, SlotChanged, SlotSwapped, InventoryFull
}
```

---

## InventoryItemSO.cs

**用途**: 物品配置数据

```csharp
[CreateAssetMenu(menuName = "ZeroEngine/Inventory/Item Data")]
public class InventoryItemSO : ScriptableObject
{
    // 基本信息
    public string Id;
    public string ItemName;
    public InventoryItemType Type;
    public Sprite Icon;
    public string Description;

    // 分类 (v1.2.0+)
    public ItemCategory Category;
    public ItemRarity Rarity;

    // 堆叠
    public int MaxStack = 99;
    public bool IsStackable => MaxStack > 1;  // v1.2.0+

    // 经济
    public int BuyPrice;
    public int SellPrice;

    // 方法
    virtual bool Use();                    // 使用物品
    Color GetRarityColor();                // 获取稀有度颜色 (v1.2.0+)
}
```

---

## InventorySlot.cs

**用途**: 单个背包槽位

```csharp
[Serializable]
public class InventorySlot
{
    public string ItemId;
    public int Amount;
    public InventoryItemSO ItemData;  // 运行时引用
    public int SlotIndex;             // v1.2.0+

    // 属性
    bool IsEmpty { get; }
    bool IsFull { get; }              // v1.2.0+
    int AvailableSpace { get; }       // v1.2.0+

    // 方法
    void Set(InventoryItemSO item, int amount);
    void AddAmount(int amount);
    void RemoveAmount(int amount);
    void Clear();

    // v1.2.0+ 新增
    InventorySlot Clone();
    void SwapWith(InventorySlot other);
    int TryMergeInto(InventorySlot target);  // 返回剩余数量
}
```

---

## SlotChangedEventArgs (v1.2.0+)

```csharp
public struct SlotChangedEventArgs
{
    public int SlotIndex;
    public InventorySlot Slot;
    public InventoryEventType EventType;
    public InventoryItemSO Item;
    public int OldAmount;
    public int NewAmount;
    public int DeltaAmount;
}
```

---

## InventoryManager.cs

**用途**: 背包管理单例

```csharp
public class InventoryManager : Singleton<InventoryManager>
{
    public const int MaxSlots = 30;
    public IReadOnlyList<InventorySlot> Slots { get; }
    public bool IsFull { get; }  // v1.2.0+

    // 事件 (v1.2.0+)
    event Action<SlotChangedEventArgs> OnSlotChanged;
    event Action<InventoryItemSO, int> OnItemAdded;
    event Action<InventoryItemSO, int> OnItemRemoved;
    event Action<InventoryItemSO, int> OnInventoryFull;

    // 数据库
    void InitializeDatabase(List<InventoryItemSO> allItems);
    void RegisterItem(InventoryItemSO item);   // v1.2.0+
    InventoryItemSO GetItemData(string id);    // v1.2.0+

    // 物品操作
    bool AddItem(InventoryItemSO item, int amount = 1);
    bool AddItem(string itemId, int amount = 1);  // v1.2.0+
    void RemoveItem(string itemId, int amount = 1);
    void RemoveItem(InventoryItemSO item, int amount = 1);  // v1.2.0+
    void ClearSlot(int index);   // v1.2.0+
    void ClearAll();             // v1.2.0+

    // 查询 (v1.2.0+ 增强)
    int GetItemCount(string itemId);
    int GetItemCount(InventoryItemSO item);
    bool HasItem(string itemId, int amount = 1);
    InventorySlot FindSlot(string itemId);
    InventorySlot GetSlot(int index);
    int GetEmptySlotCount();
    IEnumerable<InventorySlot> GetAllItems();

    // 分类查询 (v1.2.0+)
    IEnumerable<InventorySlot> GetItemsByType(InventoryItemType type);
    IEnumerable<InventorySlot> GetItemsByCategory(ItemCategory category);
    IEnumerable<InventorySlot> GetItemsByRarity(ItemRarity rarity);
    IEnumerable<InventorySlot> GetItemsByMinRarity(ItemRarity minRarity);

    // 槽位操作 (v1.2.0+)
    void SwapSlots(int indexA, int indexB);
    bool UseItem(int slotIndex);
    void Sort(Comparison<InventorySlot> comparison = null);
}
```

---

## 使用示例

### 基础用法

```csharp
// 初始化数据库
var allItems = Resources.LoadAll<InventoryItemSO>("Items").ToList();
InventoryManager.Instance.InitializeDatabase(allItems);

// 添加物品
InventoryManager.Instance.AddItem(healthPotionSO, 5);

// 检查数量
int count = InventoryManager.Instance.GetItemCount("health_potion");

// 监听 EventManager 事件
EventManager.Subscribe(GameEvents.InventoryUpdated, OnInventoryChanged);
```

### v1.2.0+ 事件系统

```csharp
// 监听槽位变化
InventoryManager.Instance.OnSlotChanged += args =>
{
    Debug.Log($"Slot {args.SlotIndex}: {args.EventType}, " +
              $"{args.OldAmount} -> {args.NewAmount}");
};

// 监听背包满
InventoryManager.Instance.OnInventoryFull += (item, overflow) =>
{
    Debug.LogWarning($"Cannot add {overflow}x {item.ItemName}!");
};
```

### v1.2.0+ 分类查询

```csharp
// 获取所有武器
var weapons = InventoryManager.Instance.GetItemsByCategory(ItemCategory.Weapon);

// 获取稀有以上物品
var rareItems = InventoryManager.Instance.GetItemsByMinRarity(ItemRarity.Rare);

// 排序背包
InventoryManager.Instance.Sort();
```

---

## 版本历史

| 版本 | 新增功能 |
|------|----------|
| v1.0.0 | 基础背包系统 |
| v1.2.0 | 事件系统、分类查询、稀有度、槽位操作 |
