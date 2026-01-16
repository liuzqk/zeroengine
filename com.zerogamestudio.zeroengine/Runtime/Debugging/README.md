# ZeroEngine.Debugging - 运行时调试模块

> **版本**: v1.4.0+
> **最后更新**: 2026-01-01

---

## 概述

Debugging 模块提供运行时调试和监控功能，用于追踪 BehaviorTree、FSM、Buff、Stat 等系统的实时状态。

## 核心组件

| 组件 | 说明 |
|------|------|
| `DebugManager` | 调试系统入口，管理所有调试模块 |
| `IDebugModule` | 调试模块接口 |
| `BTDebugger` | BehaviorTree 调试器 |
| `FSMDebugger` | StateMachine 调试器 |
| `BuffMonitor` | Buff 系统监控 |
| `StatMonitor` | Stat 系统监控 |
| `PoolProfiler` | 对象池性能分析 |

---

## 快速开始

### 1. 启用调试系统

```csharp
using ZeroEngine.Debugging;

void Start()
{
    // 启用调试系统
    DebugManager.IsEnabled = true;

    // 设置更新间隔（可选，默认 0.1 秒）
    DebugManager.UpdateInterval = 0.1f;
}

void Update()
{
    // 在游戏循环中更新
    DebugManager.Update();
}
```

### 2. 追踪 BehaviorTree

```csharp
// 创建并注册调试器
var btDebugger = new BTDebugger();
DebugManager.RegisterModule(btDebugger);

// 追踪行为树
btDebugger.TrackTree(myBehaviorTree);

// 订阅节点执行事件
btDebugger.OnNodeExecuted += data =>
{
    Debug.Log($"Node: {data.NodeName}, State: {data.State}");
};
```

### 3. 追踪 FSM

```csharp
var fsmDebugger = new FSMDebugger();
DebugManager.RegisterModule(fsmDebugger);

// 追踪状态机
fsmDebugger.TrackFSM(myStateMachine, "PlayerFSM");

// 订阅状态转换事件
fsmDebugger.OnStateTransition += record =>
{
    Debug.Log($"Transition: {record.FromState} → {record.ToState}");
};
```

### 4. 监控 Buff 系统

```csharp
var buffMonitor = new BuffMonitor();
DebugManager.RegisterModule(buffMonitor);

// 追踪 BuffReceiver
buffMonitor.TrackReceiver(playerBuffReceiver);

// 订阅 Buff 事件
buffMonitor.OnBuffEvent += (data, eventType) =>
{
    Debug.Log($"Buff {data.BuffName}: {eventType}");
};
```

### 5. 监控 Stat 系统

```csharp
var statMonitor = new StatMonitor();
DebugManager.RegisterModule(statMonitor);

// 追踪 StatController
statMonitor.TrackController(playerStatController);

// 订阅 Stat 变化事件
statMonitor.OnStatChanged += (data, oldValue, newValue) =>
{
    Debug.Log($"{data.StatName}: {oldValue} → {newValue}");
};
```

### 6. 对象池分析

```csharp
var poolProfiler = new PoolProfiler();
DebugManager.RegisterModule(poolProfiler);

// 获取统计信息
var stats = poolProfiler.LastStats;
Debug.Log($"Cache Hit Rate: {poolProfiler.CacheHitRate:P0}");
```

---

## 获取调试信息

### 获取所有模块摘要

```csharp
string summary = DebugManager.GetAllSummaries();
Debug.Log(summary);
```

### 获取特定模块

```csharp
var btDebugger = DebugManager.GetModule<BTDebugger>(BTDebugger.MODULE_NAME);
var nodeData = btDebugger.GetNodeData();

foreach (var node in nodeData)
{
    Debug.Log(node.ToString());
}
```

### 遍历所有模块

```csharp
foreach (var module in DebugManager.GetAllModules())
{
    Debug.Log($"[{module.ModuleName}] {module.GetSummary()}");
}
```

---

## 调试数据结构

### BTNodeDebugData

```csharp
public struct BTNodeDebugData
{
    public string NodeName;      // 节点名称
    public string NodeType;      // 节点类型 (Composite/Decorator/Leaf)
    public NodeState State;      // 当前状态
    public int Depth;            // 树深度
    public bool IsActive;        // 是否活跃
}
```

### FSMTransitionRecord

```csharp
public struct FSMTransitionRecord
{
    public string FromState;     // 来源状态
    public string ToState;       // 目标状态
    public float Timestamp;      // 时间戳
    public string Trigger;       // FSM 名称
}
```

### BuffDebugData

```csharp
public struct BuffDebugData
{
    public string BuffId;        // Buff ID
    public string BuffName;      // Buff 名称
    public int CurrentStacks;    // 当前层数
    public int MaxStacks;        // 最大层数
    public float RemainingTime;  // 剩余时间
    public float Duration;       // 总时长
}
```

### StatDebugData

```csharp
public struct StatDebugData
{
    public string StatName;       // 属性名称
    public float BaseValue;       // 基础值
    public float CurrentValue;    // 当前值
    public int ModifierCount;     // 修饰器数量
    public List<StatModifierDebugData> Modifiers;
}
```

### PoolDebugData

```csharp
public struct PoolDebugData
{
    public string PoolName;       // 池名称
    public int PooledCount;       // 池中数量
    public int ActiveCount;       // 活跃数量
    public int TotalCreated;      // 总创建数
    public float HitRate;         // 命中率
}
```

---

## 最佳实践

### 1. 条件编译

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    DebugManager.IsEnabled = true;
    // 注册调试模块...
#endif
```

### 2. 按需追踪

```csharp
// 只追踪需要调试的对象
if (isDebuggingPlayer)
{
    btDebugger.TrackTree(playerBT);
    buffMonitor.TrackReceiver(playerBuffReceiver);
}
```

### 3. 清理资源

```csharp
void OnDestroy()
{
    btDebugger.UntrackTree(myTree);
    fsmDebugger.UntrackFSM(myFSM);
    buffMonitor.UntrackReceiver(myReceiver);
    statMonitor.UntrackController(myController);
}
```

### 4. 自定义 UI

```csharp
void OnGUI()
{
    if (!DebugManager.IsEnabled) return;

    GUILayout.BeginArea(new Rect(10, 10, 300, 500));
    GUILayout.Label("=== Debug Panel ===");

    foreach (var module in DebugManager.GetAllModules())
    {
        GUILayout.Label($"[{module.ModuleName}]");
        foreach (var entry in module.GetEntries())
        {
            GUILayout.Label($"  {entry.Label}: {entry.Value}");
        }
    }

    GUILayout.EndArea();
}
```

---

## API 参考

### DebugManager

| 方法/属性 | 说明 |
|-----------|------|
| `IsEnabled` | 调试系统开关 |
| `UpdateInterval` | 更新间隔 |
| `RegisterModule(module)` | 注册调试模块 |
| `UnregisterModule(name)` | 注销调试模块 |
| `GetModule<T>(name)` | 获取调试模块 |
| `GetAllModules()` | 获取所有模块 |
| `Update()` | 更新所有模块 |
| `GetAllSummaries()` | 获取所有摘要 |
| `ClearAll()` | 清空所有数据 |
| `Reset()` | 重置调试系统 |

### IDebugModule

| 方法/属性 | 说明 |
|-----------|------|
| `ModuleName` | 模块名称 |
| `IsEnabled` | 模块开关 |
| `Update()` | 更新数据 |
| `GetSummary()` | 获取摘要 |
| `GetEntries()` | 获取详细条目 |
| `Clear()` | 清空数据 |
