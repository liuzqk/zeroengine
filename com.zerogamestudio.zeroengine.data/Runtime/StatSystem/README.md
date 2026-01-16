# ZeroEngine.StatSystem API 文档

> **用途**: 本文档面向AI助手，提供StatSystem模块的快速参考。

---

## 目录结构

```
StatSystem/
├── Stat.cs              # 属性类和修饰器
├── StatController.cs    # 属性控制器
├── StatType.cs          # 属性类型枚举
└── Formula/
    └── MathFormula.cs   # 公式计算
```

---

## Stat.cs

**用途**: 单个属性及其修饰器

### StatModifier

```csharp
[Serializable]
public class StatModifier
{
    public float Value;
    public StatModType ModType;    // Flat, PercentAdd, PercentMult
    public int Order;              // 计算顺序
    public object Source;          // 来源（用于追踪/移除）
    public MathFormula Formula;    // 可选公式
    
    StatModifier(float value, StatModType type, int order = default, object source = null);
    float GetValue(MathContext ctx = null);  // 支持公式计算
}
```

### StatChangedEventArgs (v1.1.0+)

```csharp
public struct StatChangedEventArgs
{
    public float OldValue;
    public float NewValue;
    public float Delta { get; }  // NewValue - OldValue
}
```

### Stat

```csharp
[Serializable]
public class Stat
{
    public float BaseValue;
    public float MinValue = float.MinValue;  // 最终值下限
    public float MaxValue = float.MaxValue;  // 最终值上限
    public float Value { get; }  // 计算后的最终值（缓存，受上下限约束）

    // 事件 (v1.1.0+)
    event Action<StatChangedEventArgs> OnValueChanged;  // 值变化时触发

    Stat();
    Stat(float baseValue);
    Stat(float baseValue, float minValue, float maxValue);  // 带上下限构造

    void AddModifier(StatModifier mod);
    bool RemoveModifier(StatModifier mod);
    void RemoveAllModifiersFromSource(object source);

    // 新增 (v1.1.0+)
    void ForceRecalculate();      // 强制重新计算并触发事件
    void ClearEventListeners();   // 清理事件订阅（销毁时调用）
}
```

**计算公式**: `Clamp((Base + Flat) × (1 + PercentAdd) × PercentMult, MinValue, MaxValue)`

---

## StatModType

```csharp
public enum StatModType
{
    Flat = 100,        // 加法
    PercentAdd = 200,  // 百分比加法（累加后乘）
    PercentMult = 300  // 百分比乘法（连乘）
}
```

**示例**:
```
Base = 100
+20 Flat → 120
+0.5 PercentAdd (+50%) → 180
×1.2 PercentMult → 216
```

---

## StatController.cs

**用途**: 管理实体的所有属性

### StatControllerChangedEventArgs (v1.1.0+)

```csharp
public struct StatControllerChangedEventArgs
{
    public StatType StatType;
    public float OldValue;
    public float NewValue;
    public float Delta { get; }  // NewValue - OldValue
}
```

### StatController

```csharp
public class StatController : MonoBehaviour, IStatProvider
{
    // 事件 (v1.1.0+)
    event Action<StatControllerChangedEventArgs> OnAnyStatChanged;  // 任意属性变化时触发

    // 初始化 (v1.1.0+ 重载)
    void InitStat(StatType type, float baseValue);
    void InitStat(StatType type, float baseValue, float minValue, float maxValue);  // 带约束

    // 获取属性
    Stat GetStat(StatType type);
    float GetStatValue(StatType type);

    // 修饰器操作
    void AddModifier(StatType type, StatModifier mod);
    void RemoveModifier(StatType type, StatModifier mod);

    // 刷新 (v1.1.0+)
    void RefreshAllStats();  // 强制重新计算所有属性并触发事件
}
```

---

## StatType（示例）

```csharp
public enum StatType
{
    MaxHealth,
    MaxMana,
    Attack,
    Defense,
    Speed,
    CritRate,
    CritDamage,
    // 项目可扩展
}
```

---

## MathFormula.cs

**用途**: ScriptableObject公式，支持动态计算

```csharp
[CreateAssetMenu(menuName = "ZeroEngine/Stat System/Math Formula")]
public class MathFormula : ScriptableObject
{
    public string Expression;  // 如 "base * 1.5 + level * 10"
    
    float Evaluate(MathContext ctx);
}

public class MathContext
{
    Dictionary<string, float> Variables;
    
    void Set(string name, float value);
    float Get(string name);
}
```

---

## 使用示例

```csharp
// 添加修饰器
var attackBuff = new StatModifier(50, StatModType.Flat, source: buffHandler);
statController.AddModifier(StatType.Attack, attackBuff);

// 获取最终值
float damage = statController.GetStatValue(StatType.Attack);

// 移除某来源的所有修饰器
statController.RemoveAllModifiersFromSource(buffHandler);

// 使用上下限防止数值溢出
var critRate = new Stat(0.05f, 0f, 1f);  // 暴击率: 0% ~ 100%
critRate.AddModifier(new StatModifier(2.0f, StatModType.Flat));
Debug.Log(critRate.Value);  // 输出 1.0，被限制在最大值

var defense = new Stat(100) { MinValue = 0, MaxValue = 9999 };  // 防御: 0 ~ 9999

// v1.1.0+ 事件驱动示例
var health = new Stat(100);
health.OnValueChanged += args =>
{
    Debug.Log($"Health: {args.OldValue} -> {args.NewValue} (Delta: {args.Delta})");
    if (args.NewValue <= 0) OnDeath();
};

// 控制器级别事件
statController.OnAnyStatChanged += args =>
{
    Debug.Log($"{args.StatType} changed by {args.Delta}");
    RefreshUI(args.StatType);
};

// 初始化时带约束
statController.InitStat(StatType.CritRate, 0.1f, 0f, 1f);  // 暴击率限制在0-100%
```
