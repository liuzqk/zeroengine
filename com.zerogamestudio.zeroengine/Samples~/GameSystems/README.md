# ZeroEngine Game Systems Examples (v1.11.0)

This sample package demonstrates the four major game systems added in ZeroEngine v1.11.0:

## Systems Overview

### 1. Loot Table System (`LootTableExample.cs`)
- Weighted drop tables with pity/guarantee system
- Multiple drop modes: Weight, Pity, Layered
- Polymorphic conditions for conditional drops
- Event-driven architecture

**Key Classes:**
- `LootTableSO` - Drop table definition
- `LootTableManager` - Singleton manager
- `LootCondition` - Base class for conditions

### 2. Achievement System (`AchievementExample.cs`)
- Event-driven achievement tracking
- Polymorphic conditions (Counter, State, Event, Composite)
- Multiple reward types (Item, Currency, Exp, Unlock, etc.)
- Steam/platform integration support

**Key Classes:**
- `AchievementSO` - Achievement definition
- `AchievementManager` - Singleton manager
- `AchievementCondition` - Base class for conditions
- `AchievementReward` - Base class for rewards

### 3. Crafting System (`CraftingExample.cs`)
- Recipe-based crafting with unlock system
- Skill experience and level progression
- Instant and delayed crafting support
- Batch crafting capability

**Key Classes:**
- `CraftingRecipeSO` - Recipe definition
- `CraftingManager` - Singleton manager
- `RecipeBookSO` - Recipe collection

### 4. Relationship System (`RelationshipExample.cs`)
- NPC relationship/affinity tracking
- Gift preference system
- Daily interaction limits
- Relationship events and triggers

**Key Classes:**
- `RelationshipDataSO` - NPC relationship data
- `RelationshipManager` - Singleton manager
- `RelationshipGroupSO` - NPC grouping

## Setup Instructions

1. **Import the Sample**
   - In Unity, go to Package Manager
   - Find ZeroEngine package
   - Import "Game Systems Examples"

2. **Create Test Scene**
   - Create a new scene
   - Add empty GameObject with desired example script

3. **Create Test Data**
   - Use Create menu (ZeroEngine submenu) to create ScriptableObject assets
   - Configure the data according to your needs
   - Assign to example script references

## Key Patterns Used

### MonoSingleton + ISaveable
All managers inherit from `MonoSingleton<T>` and implement `ISaveable`:
```csharp
public class MyManager : MonoSingleton<MyManager>, ISaveable
{
    public string SaveKey => "MyManager";
    public object ExportSaveData() { ... }
    public void ImportSaveData(object data) { ... }
}
```

### Polymorphic Serialization
Conditions and rewards use `[SerializeReference]` for type polymorphism:
```csharp
[SerializeReference]
public List<AchievementCondition> Conditions;
```

### Event-Driven Architecture
All systems expose events for external integration:
```csharp
LootTableManager.Instance.OnLootEvent += (args) => { ... };
AchievementManager.Instance.OnAchievementEvent += (args) => { ... };
CraftingManager.Instance.OnCraftingEvent += (args) => { ... };
RelationshipManager.Instance.OnRelationshipEvent += (args) => { ... };
```

## Cross-System Integration

```
Achievement <-> Crafting
- Crafting triggers "Craft" achievement events
- Achievements can unlock recipes

Achievement <-> Relationship
- Gift/talk triggers achievement events
- Achievement rewards can affect relationship

Crafting <-> Relationship
- Relationship level can unlock recipes

All Systems <-> Inventory
- Items as materials/rewards/gifts
```

## Controls Summary

| Key | Action |
|-----|--------|
| **Loot** | |
| 1-3 | Roll different loot tables |
| R | Reset pity counters |
| **Achievement** | |
| K | Trigger Kill event |
| C | Trigger Collect event |
| R | Claim all rewards |
| P | Show progress |
| **Crafting** | |
| C | Craft test recipe |
| U | Unlock test recipe |
| A | Add test materials |
| L | List recipes |
| **Relationship** | |
| T | Talk to NPC |
| 1-3 | Give gifts |
| G | Add test gifts |
| S | Show status |