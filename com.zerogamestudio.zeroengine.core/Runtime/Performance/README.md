# ZeroEngine.Performance - 性能优化模块

> **版本**: v1.4.0+
> **最后更新**: 2026-01-01

---

## 概述

Performance 模块提供零 GC 的集合池和缓存系统，用于优化 Unity 游戏的内存分配和垃圾回收。

## 核心组件

| 组件 | 说明 |
|------|------|
| `ZeroGC` | 统一入口类，提供所有性能优化 API |
| `ListPool<T>` | List 对象池 |
| `DictionaryPool<K,V>` | Dictionary 对象池 |
| `StringBuilderPool` | StringBuilder 对象池 |
| `PooledList<T>` | 自动归还的 List 包装器 |
| `FormattedValueCache` | 格式化值缓存 |

---

## 快速开始

### 1. 使用池化 List（推荐）

```csharp
using ZeroEngine.Performance;

// 自动归还版本（推荐）
using var list = ZeroGC.GetList<int>();
list.Add(1);
list.Add(2);
list.Add(3);
// using 块结束时自动归还到池中

// 手动归还版本
var rawList = ZeroGC.GetListRaw<int>();
rawList.Add(1);
// 使用完毕后必须手动归还
ZeroGC.Return(rawList);
```

### 2. 使用池化 Dictionary

```csharp
using var dict = ZeroGC.GetDictionary<string, int>();
dict["hp"] = 100;
dict["mp"] = 50;
// 自动归还
```

### 3. 使用池化 StringBuilder

```csharp
// 方式 1: 手动管理
var sb = ZeroGC.GetStringBuilder();
sb.Append("Hello ");
sb.Append("World");
string result = sb.ToString();
ZeroGC.Return(sb);

// 方式 2: 自动管理（推荐）
string result = ZeroGC.BuildString(sb => {
    sb.Append("Hello ");
    sb.Append("World");
});
```

### 4. 使用格式化缓存

```csharp
// 缓存格式化结果，避免重复格式化
string formatted = ZeroGC.CacheFormattedValue("hp", 100, v => $"{v} HP");
// 再次调用相同值时直接返回缓存结果

// 使用格式字符串
string gold = ZeroGC.CacheFormattedValue("gold", 1234567, "{0:N0}");
// 结果: "1,234,567"
```

---

## 预热

在游戏启动时预热池，避免运行时分配：

```csharp
void Start()
{
    // 预热常用类型
    ZeroGC.WarmUpCommon(listCount: 16, dictCount: 8, sbCount: 8);

    // 预热特定类型
    ZeroGC.WarmUpLists<Vector3>(count: 10, listCapacity: 100);
    ZeroGC.WarmUpDictionaries<int, Enemy>(count: 5);
}
```

---

## 统计信息

```csharp
// 获取性能统计
var stats = ZeroGC.GetStats();

Debug.Log($"List Pool: {stats.ListPoolStats.PooledCount} pooled, " +
          $"{stats.ListPoolStats.TotalCreated} created, " +
          $"Hit Rate: {stats.ListPoolStats.HitRate:P0}");

Debug.Log($"Cache: {stats.CacheEntryCount} entries, " +
          $"Hit Rate: {stats.CacheHitRate:P0}");
```

---

## 最佳实践

### 1. 优先使用 using 语句

```csharp
// ✅ 推荐：自动归还
using var list = ZeroGC.GetList<int>();

// ⚠️ 谨慎：需要手动归还
var list = ZeroGC.GetListRaw<int>();
// 容易忘记归还，造成池泄漏
```

### 2. 避免在热路径中创建新集合

```csharp
// ❌ 错误：每帧创建新 List
void Update()
{
    var enemies = new List<Enemy>(); // GC 分配!
    // ...
}

// ✅ 正确：使用池化 List
void Update()
{
    using var enemies = ZeroGC.GetList<Enemy>();
    // ...
} // 自动归还
```

### 3. 对于超大容量，考虑不使用池

```csharp
// 池会限制最大容量，超大 List 不会放回池中
// 这是有意设计，避免内存浪费
var hugeList = ZeroGC.GetListRaw<int>(capacity: 10000);
// 归还时会被丢弃，不会放入池中
```

### 4. 格式化缓存键设计

```csharp
// 使用有意义的键，便于失效管理
string hp = ZeroGC.CacheFormattedValue("player.hp", health, v => $"{v}/{maxHealth}");
string mp = ZeroGC.CacheFormattedValue("player.mp", mana, v => $"{v}/{maxMana}");

// 一次性失效玩家相关缓存
ZeroGC.InvalidateCache("player.");
```

---

## 与现有系统集成

### 与 BindableProperty 集成

```csharp
public class OptimizedViewModel
{
    private readonly FormattedValueCache _cache = new(maxEntries: 100);

    public BindableProperty<int> Gold { get; } = new(0);

    public string FormattedGold =>
        _cache.GetOrCreate("gold", Gold.Value, v => $"{v:N0} G");
}
```

### 与 BehaviorTree 集成

```csharp
public class BTPerformanceHelper
{
    // 在 BT 节点中使用池化 List 收集目标
    public static NodeState FindTargets(BTContext ctx)
    {
        using var targets = ZeroGC.GetList<Transform>();

        // 收集目标...
        foreach (var collider in Physics.OverlapSphere(pos, radius))
        {
            targets.Add(collider.transform);
        }

        ctx.Blackboard.SetValue("targets", targets.ToArray());
        return NodeState.Success;
    }
}
```

---

## 配置

池的默认配置：

| 参数 | List | Dictionary | StringBuilder |
|------|------|------------|---------------|
| 池最大容量 | 64 | 32 | 32 |
| 对象最大容量 | 1024 | 512 | 8192 |
| 默认初始容量 | 16 | 16 | 256 |

缓存默认配置：

| 参数 | 默认值 |
|------|--------|
| 最大条目数 | 1000 |
| 过期时间 | 60 秒 |

---

## API 参考

### ZeroGC

| 方法 | 说明 |
|------|------|
| `GetList<T>()` | 获取自动归还的 PooledList |
| `GetListRaw<T>()` | 获取需手动归还的 List |
| `GetDictionary<K,V>()` | 获取自动归还的 PooledDictionary |
| `GetStringBuilder()` | 获取需手动归还的 StringBuilder |
| `BuildString(Action)` | 使用 StringBuilder 构建字符串 |
| `CacheFormattedValue<T>()` | 获取或缓存格式化值 |
| `WarmUpCommon()` | 预热常用类型池 |
| `GetStats()` | 获取性能统计 |
| `ClearAll()` | 清空所有池和缓存 |
