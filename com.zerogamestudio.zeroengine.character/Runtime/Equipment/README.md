# Equipment System API 文档

> **用途**: 本文档面向AI助手，提供装备系统的快速参考。

---

## 目录结构

| 文件 | 说明 |
|------|------|
| `EquipmentEnums.cs` | 枚举定义 (EnhanceResult, EnchantmentType) |
| `EquipmentSlotType.cs` | 装备槽位类型 (ScriptableObject) |
| `EquipmentEvents.cs` | 事件参数类 |
| `EquipmentDataSO.cs` | 装备数据定义 (继承 InventoryItemSO) |
| `EquipmentInstance.cs` | 运行时装备实例 |
| `EquipmentSetSO.cs` | 套装定义 |
| `EquipmentManager.cs` | 装备管理器 (MonoSingleton + ISaveable) |

---

## EquipmentSlotType.cs

**用途**: 可配置的装备槽位类型定义

### Public API

```csharp
namespace ZeroEngine.Equipment
{
    [CreateAssetMenu(fileName = "NewSlotType", menuName = "ZeroEngine/Equipment/Slot Type")]
    public class EquipmentSlotType : ScriptableObject
    {
        public string SlotId { get; }
        public string DisplayName { get; }
        public Sprite Icon { get; }
    }
}
```

**使用示例**:
```csharp
// 创建槽位类型资产: ZeroEngine/Equipment/Slot Type
// 配置: Weapon, Helmet, Armor, Boots, Ring1, Ring2, Necklace 等
```

---

## EquipmentDataSO.cs

**用途**: 装备数据定义，继承自 InventoryItemSO

### Public API

```csharp
namespace ZeroEngine.Equipment
{
    [CreateAssetMenu(fileName = "NewEquipment", menuName = "ZeroEngine/Equipment/Equipment Data")]
    public class EquipmentDataSO : InventoryItemSO
    {
        public EquipmentSlotType SlotType { get; }
        public List<StatModifier> BaseStats { get; }
        public EquipmentSetSO BelongsToSet { get; }
        public int MaxEnhanceLevel { get; }
        public int MaxRefineLevel { get; }
        public int GemSlotCount { get; }
        public List<EnchantmentData> EnchantmentPool { get; }
    }
}
```

---

## EquipmentInstance.cs

**用途**: 运行时装备实例，包含强化/精炼/附魔/宝石状态

### Public API

```csharp
namespace ZeroEngine.Equipment
{
    [Serializable]
    public class EquipmentInstance
    {
        // 属性
        public EquipmentDataSO Data { get; }
        public int EnhanceLevel { get; }
        public int RefineLevel { get; }
        public List<EnchantmentData> Enchantments { get; }
        public InventoryItemSO[] GemSlots { get; }

        // 方法
        public IReadOnlyList<StatModifier> GetAllModifiers();
        public EnhanceResult Enhance(int levels = 1);
        public EnhanceResult Refine(int levels = 1);
        public bool AddEnchantment(EnchantmentData enchantment);
        public bool SocketGem(int slotIndex, InventoryItemSO gem);
        public bool RemoveGem(int slotIndex);
    }
}
```

**使用示例**:
```csharp
// 获取装备实例
var equipped = EquipmentManager.Instance.GetEquipped(weaponSlot);

// 强化
var result = equipped.Enhance(1);
if (result == EnhanceResult.Success)
{
    Debug.Log($"强化成功! 等级: {equipped.EnhanceLevel}");
}

// 获取所有属性修饰器
var modifiers = equipped.GetAllModifiers();
```

---

## EquipmentSetSO.cs

**用途**: 套装定义，包含阈值效果

### Public API

```csharp
namespace ZeroEngine.Equipment
{
    [CreateAssetMenu(fileName = "NewEquipmentSet", menuName = "ZeroEngine/Equipment/Equipment Set")]
    public class EquipmentSetSO : ScriptableObject
    {
        public string SetId { get; }
        public string SetName { get; }
        public Sprite Icon { get; }
        public List<EquipmentDataSO> Pieces { get; }
        public List<SetEffect> Effects { get; }

        public IEnumerable<SetEffect> GetActiveEffects(int equippedCount);
    }

    [Serializable]
    public class SetEffect
    {
        public int RequiredPieces;
        public List<StatModifier> Modifiers;
        public string Description;
    }
}
```

**使用示例**:
```csharp
// 套装效果配置示例
// 2件套: +10% 攻击力
// 4件套: +20% 暴击率
// 6件套: +50% 暴击伤害
```

---

## EquipmentManager.cs

**用途**: 装备管理器，处理装备/卸装、属性计算、套装效果

### Public API

```csharp
namespace ZeroEngine.Equipment
{
    public class EquipmentManager : MonoSingleton<EquipmentManager>, ISaveable
    {
        // ISaveable
        public string SaveKey => "Equipment";

        // 配置
        public List<EquipmentSlotType> ConfiguredSlots { get; }

        // 装备操作
        public bool Equip(EquipmentDataSO equipment);
        public bool Equip(EquipmentInstance instance);
        public EquipmentInstance Unequip(EquipmentSlotType slotType);
        public void UnequipAll();

        // 查询
        public EquipmentInstance GetEquipped(EquipmentSlotType slotType);
        public IReadOnlyDictionary<EquipmentSlotType, EquipmentInstance> GetAllEquipped();
        public bool IsSlotEmpty(EquipmentSlotType slotType);
        public bool CanEquip(EquipmentDataSO equipment);

        // 属性计算
        public IEnumerable<StatModifier> GetAllStatModifiers();
        public IEnumerable<(EquipmentSetSO set, List<StatModifier> modifiers)> GetActiveSetEffects();

        // Provider 集成
        public void RegisterStatProvider(IStatProvider provider);

        // 事件
        public event Action<EquipmentEventArgs> OnEquipped;
        public event Action<EquipmentEventArgs> OnUnequipped;
        public event Action<EnhanceEventArgs> OnEnhanced;
        public event Action<SetEventArgs> OnSetEffectActivated;

        // ISaveable
        public object ExportSaveData();
        public void ImportSaveData(object data);
        public void ResetToDefault();
    }
}
```

**使用示例**:
```csharp
using ZeroEngine.Equipment;

// 1. 装备物品
var weapon = myEquipmentData;
if (EquipmentManager.Instance.CanEquip(weapon))
{
    EquipmentManager.Instance.Equip(weapon);
}

// 2. 获取所有装备属性
var modifiers = EquipmentManager.Instance.GetAllStatModifiers();
foreach (var mod in modifiers)
{
    statController.AddModifier(mod.statType, mod);
}

// 3. 获取套装效果
var setEffects = EquipmentManager.Instance.GetActiveSetEffects();
foreach (var (set, mods) in setEffects)
{
    Debug.Log($"套装 {set.SetName} 激活，效果数量: {mods.Count}");
}

// 4. 监听事件
EquipmentManager.Instance.OnEquipped += args =>
{
    Debug.Log($"装备了 {args.Equipment.Data.name} 到 {args.Slot.DisplayName}");
};

// 5. 存档集成 (自动注册到 SaveSlotManager)
```

---

## 解耦接口

### IStatProvider

```csharp
namespace ZeroEngine.Equipment
{
    public interface IStatProvider
    {
        IEnumerable<StatModifier> GetStatModifiers();
    }
}
```

EquipmentManager 和 TalentTreeController 都实现此接口，可以统一收集属性修饰器。

---

## 编辑器工具

- **Equipment Editor**: `ZeroEngine > Equipment > Equipment Editor`
  - 三标签页: Equipment / Slot Types / Sets
  - 搜索和槽位筛选
  - 快速创建和管理资产

---

## 版本历史

- **(v1.10.0)** 初始版本
