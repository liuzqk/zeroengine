# Talent Tree System API 文档

> **用途**: 本文档面向AI助手，提供天赋树系统的快速参考。

---

## 目录结构

| 文件 | 说明 |
|------|------|
| `TalentEnums.cs` | 枚举定义 (TalentNodeType) |
| `TalentEvents.cs` | 事件参数类 |
| `TalentNodeSO.cs` | 天赋节点定义 (ScriptableObject) |
| `TalentTreeSO.cs` | 天赋树定义 (ScriptableObject) |
| `TalentTreeController.cs` | 运行时控制器 (ISaveable) |
| `Effects/` | 多态效果实现目录 |

### Effects 目录

| 文件 | 说明 |
|------|------|
| `TalentEffect.cs` | 效果基类 (abstract) |
| `StatModifierEffect.cs` | 属性修改效果 |
| `BuffEffect.cs` | Buff 应用效果 |
| `UnlockAbilityEffect.cs` | 技能解锁效果 |
| `CustomEffect.cs` | 自定义回调效果 |

---

## TalentNodeSO.cs

**用途**: 天赋节点定义

### Public API

```csharp
namespace ZeroEngine.TalentTree
{
    [CreateAssetMenu(fileName = "NewTalentNode", menuName = "ZeroEngine/TalentTree/Talent Node")]
    public class TalentNodeSO : ScriptableObject
    {
        // 基础信息
        public string NodeId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite Icon { get; }

        // 节点类型
        public TalentNodeType NodeType { get; } // Normal, Keystone, Start, Branch

        // 等级配置
        public int MaxLevel { get; }
        public int PointCostPerLevel { get; }

        // 解锁条件
        public List<TalentNodeSO> Prerequisites { get; }
        public int PrerequisiteMinLevel { get; }
        public int RequiredCharacterLevel { get; }

        // 效果列表 ([SerializeReference] 多态)
        [SerializeReference]
        public List<TalentEffect> Effects { get; }

        // 编辑器位置
        public Vector2 EditorPosition { get; }
    }

    public enum TalentNodeType
    {
        Normal,     // 普通节点
        Keystone,   // 核心节点 (重要天赋)
        Start,      // 起始节点
        Branch      // 分支节点
    }
}
```

---

## TalentTreeSO.cs

**用途**: 天赋树定义

### Public API

```csharp
namespace ZeroEngine.TalentTree
{
    [CreateAssetMenu(fileName = "NewTalentTree", menuName = "ZeroEngine/TalentTree/Talent Tree")]
    public class TalentTreeSO : ScriptableObject
    {
        // 基础信息
        public string TreeId { get; }
        public string TreeName { get; }
        public string Description { get; }

        // 节点和连接
        public List<TalentNodeSO> Nodes { get; }
        public List<TalentConnection> Connections { get; }

        // 查询方法
        public TalentNodeSO GetNode(string nodeId);
        public IEnumerable<TalentNodeSO> GetStartNodes();
        public IEnumerable<TalentNodeSO> GetPrerequisites(TalentNodeSO node);
        public bool Validate(out List<string> errors);
    }

    [Serializable]
    public class TalentConnection
    {
        public TalentNodeSO From;
        public TalentNodeSO To;
    }
}
```

---

## TalentEffect.cs

**用途**: 天赋效果基类 (多态)

### Public API

```csharp
namespace ZeroEngine.TalentTree
{
    [Serializable]
    public abstract class TalentEffect
    {
        public abstract string EffectDescription { get; }
        public abstract void Apply(int level, TalentTreeController controller);
        public abstract void Remove(int level, TalentTreeController controller);
    }
}
```

### 内置效果类型

```csharp
// 属性修改效果
[Serializable]
public class StatModifierEffect : TalentEffect
{
    public StatType StatType;
    public StatModType ModType;
    public float ValuePerLevel;
}

// 多属性修改效果
[Serializable]
public class MultiStatModifierEffect : TalentEffect
{
    public List<StatModifierConfig> Modifiers;
}

// Buff 应用效果
[Serializable]
public class BuffEffect : TalentEffect
{
    public BuffData BuffToApply;
    public bool PermanentBuff;
}

// 技能解锁效果
[Serializable]
public class UnlockAbilityEffect : TalentEffect
{
    public string AbilityId;
}

// 自定义回调效果
[Serializable]
public class CustomEffect : TalentEffect
{
    public string EffectId;
    public string Parameters;
}
```

---

## TalentTreeController.cs

**用途**: 天赋树运行时控制器

### Public API

```csharp
namespace ZeroEngine.TalentTree
{
    public class TalentTreeController : MonoBehaviour, ISaveable
    {
        // ISaveable
        public string SaveKey => "TalentTree";

        // 绑定天赋树
        public void SetTree(TalentTreeSO tree);
        public TalentTreeSO CurrentTree { get; }

        // 点数管理
        public int AvailablePoints { get; }
        public int TotalPointsSpent { get; }
        public void AddPoints(int amount);
        public void SetPoints(int amount);

        // 分配点数
        public bool CanAllocate(TalentNodeSO node);
        public bool TryAllocatePoint(TalentNodeSO node);
        public bool TryDeallocatePoint(TalentNodeSO node);
        public int GetNodeLevel(TalentNodeSO node);

        // 重置
        public void Reset();

        // Provider 注册
        public void RegisterStatProvider(IStatProvider provider);
        public void RegisterBuffProvider(IBuffProvider provider);
        public void RegisterAbilityProvider(IAbilityProvider provider);

        // 属性获取
        public IEnumerable<StatModifier> GetAllStatModifiers();

        // 事件
        public event Action<TalentEventArgs> OnPointAllocated;
        public event Action<TalentEventArgs> OnPointDeallocated;
        public event Action OnTreeReset;

        // ISaveable
        public object ExportSaveData();
        public void ImportSaveData(object data);
        public void ResetToDefault();
    }
}
```

**使用示例**:
```csharp
using ZeroEngine.TalentTree;

// 1. 绑定天赋树
talentController.SetTree(myTalentTree);
talentController.AddPoints(10);

// 2. 分配点数
var node = myTalentTree.GetNode("fire_mastery");
if (talentController.CanAllocate(node))
{
    talentController.TryAllocatePoint(node);
}

// 3. 查询节点等级
int level = talentController.GetNodeLevel(node);
Debug.Log($"火焰精通等级: {level}");

// 4. 注册 Provider (效果应用)
talentController.RegisterStatProvider(statController);
talentController.RegisterBuffProvider(buffReceiver);
talentController.RegisterAbilityProvider(abilityHandler);

// 5. 获取所有属性修饰器
var mods = talentController.GetAllStatModifiers();
foreach (var mod in mods)
{
    statController.AddModifier(mod.statType, mod);
}

// 6. 监听事件
talentController.OnPointAllocated += args =>
{
    Debug.Log($"分配了 {args.Node.DisplayName}，当前等级: {args.NewLevel}");
};

// 7. 重置天赋
talentController.Reset(); // 返还所有点数
```

---

## 解耦接口

### IStatProvider

```csharp
public interface IStatProvider
{
    IEnumerable<StatModifier> GetStatModifiers();
}
```

### IBuffProvider

```csharp
public interface IBuffProvider
{
    void ApplyBuff(BuffData buff);
    void RemoveBuff(string buffId);
}
```

### IAbilityProvider

```csharp
public interface IAbilityProvider
{
    void UnlockAbility(string abilityId);
    void LockAbility(string abilityId);
}
```

---

## 编辑器工具

- **Talent Tree Editor**: `ZeroEngine > TalentTree > Talent Tree Editor`
  - GraphView 节点图编辑
  - 双击 TalentTreeSO 资产自动打开
  - 右键菜单添加节点
  - 节点颜色区分类型
  - 连线自动更新前置依赖
  - 右侧属性面板 (TalentNodeInspector)

---

## 版本历史

- **(v1.10.0)** 初始版本
