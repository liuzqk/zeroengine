# ZeroEngine.AI API 文档

> **用途**: 本文档面向 AI 助手，提供 AI & Behavior 系统的快速参考。

---

## 目录结构

```
AI/
├── Common/                    # 通用接口和组件
│   ├── IAIBrain.cs           # AI 大脑通用接口
│   ├── AIContext.cs          # AI 执行上下文
│   ├── AIBlackboard.cs       # AI 黑板数据共享
│   └── AIAgent.cs            # AI 代理组件
├── UtilityAI/                 # 效用 AI 系统 (自研)
│   ├── Core/
│   │   ├── ResponseCurve.cs  # 响应曲线
│   │   ├── Consideration.cs  # 考量因素基类
│   │   ├── UtilityAction.cs  # 效用行动基类
│   │   └── UtilityBrain.cs   # 效用 AI 大脑
│   ├── Considerations/       # 预设考量
│   │   └── ZeroEngineConsiderations.cs
│   └── Actions/              # 预设行动
│       └── ZeroEngineActions.cs
├── GOAP/                      # GOAP 适配器 (crashkonijn/GOAP)
│   ├── Adapters/
│   │   └── GOAPBridge.cs     # GOAP 桥接器
│   ├── Goals/                # 预设目标
│   │   └── ZeroEngineGOAPGoals.cs
│   └── Actions/              # 预设行动
│       └── ZeroEngineGOAPActions.cs
└── NPCSchedule/               # NPC 日程系统 (自研)
    ├── Core/
    │   ├── ScheduleEntry.cs          # 日程条目
    │   ├── ScheduleAction.cs         # 日程行动基类
    │   ├── ScheduleCondition.cs      # 日程条件基类
    │   └── NPCScheduleController.cs  # NPC 日程控制器
    ├── Data/
    │   └── NPCScheduleSO.cs          # 日程 ScriptableObject
    └── Actions/
        └── ScheduleActions.cs        # 预设日程行动
```

---

## Common (通用组件)

### IAIBrain.cs
**用途**: AI 大脑通用接口，所有 AI 决策系统都实现此接口

```csharp
public interface IAIBrain
{
    bool IsActive { get; set; }
    string CurrentActionName { get; }

    void Initialize(AIContext context);
    void Tick(float deltaTime);
    void ForceReevaluate();
    void StopCurrentAction();
    void Reset();
}

public enum AIBrainPriority { Low = 0, Normal = 50, High = 100, Critical = 200 }
public enum AIActionState { Idle, Running, Success, Failed, Cancelled }
public readonly struct AIActionResult { ... }
```

**使用示例**:
```csharp
IAIBrain brain = GetComponent<UtilityBrain>();
brain.Initialize(context);
brain.IsActive = true;

// 每帧更新 (通常由 AIAgent 自动调用)
brain.Tick(Time.deltaTime);

// 强制重新评估决策
brain.ForceReevaluate();
```

---

### AIContext.cs
**用途**: AI 执行上下文，提供 AI 决策所需的环境信息

```csharp
public class AIContext
{
    // 核心引用
    public GameObject Owner { get; }
    public Transform Transform { get; }
    public AIBlackboard Blackboard { get; }

    // 目标信息
    public Transform CurrentTarget { get; set; }
    public Vector3 TargetPosition { get; set; }
    public float DistanceToTarget { get; }

    // 状态标志
    public bool IsInCombat { get; set; }
    public bool IsMoving { get; set; }
    public bool IsAlerted { get; set; }

    // 组件缓存
    public T GetCachedComponent<T>() where T : Component;
}
```

---

### AIBlackboard.cs
**用途**: AI 黑板数据共享，用于 AI 模块间的数据传递

```csharp
public class AIBlackboard
{
    public void Set<T>(string key, T value);
    public T Get<T>(string key);
    public bool TryGet<T>(string key, out T value);
    public bool Has(string key);
    public void Remove(string key);
    public void Clear();

    // 比较操作
    public bool CompareInt(string key, int value, CompareOp op);
    public bool CompareFloat(string key, float value, CompareOp op);

    // 事件
    public event Action<string, object> OnValueChanged;
    public event Action<string> OnValueRemoved;
}
```

---

### AIAgent.cs
**用途**: AI 代理组件，管理多个 AI 大脑的切换和更新

```csharp
public class AIAgent : MonoBehaviour
{
    // 大脑管理
    public void RegisterBrain<T>(T brain) where T : IAIBrain;
    public void SetActiveBrain<T>() where T : IAIBrain;
    public void SwitchToBrain<T>() where T : IAIBrain;
    public T GetBrain<T>() where T : IAIBrain;

    // 配置
    public float DecisionInterval { get; set; }
    public bool PauseWhenNotVisible { get; set; }
}
```

**使用示例**:
```csharp
var agent = GetComponent<AIAgent>();
agent.RegisterBrain(GetComponent<UtilityBrain>());
agent.RegisterBrain(GetComponent<NPCScheduleController>());

// 切换大脑
agent.SetActiveBrain<UtilityBrain>();
```

---

## UtilityAI (效用 AI)

### ResponseCurve.cs
**用途**: 响应曲线，将输入值映射到 0-1 的评分

```csharp
public class ResponseCurve
{
    public ResponseCurveType Type { get; }
    public float Slope { get; }
    public float Exponent { get; }
    public float Shift { get; }
    public bool Invert { get; }

    public float Evaluate(float input);

    // 工厂方法
    public static ResponseCurve Linear(float slope = 1f);
    public static ResponseCurve Quadratic(float slope = 1f);
    public static ResponseCurve Logistic(float slope = 1f);
    public static ResponseCurve Threshold(float threshold = 0.5f);
    public static ResponseCurve InverseLinear(float slope = 1f);
}

public enum ResponseCurveType
{
    Linear, Quadratic, Polynomial, Logistic, Logit,
    Sine, Cosine, Exponential, Logarithmic, Threshold, Custom
}
```

---

### Consideration.cs
**用途**: 考量因素基类，评估单一因素的重要性

```csharp
public abstract class Consideration
{
    public string Name { get; }
    public float Weight { get; set; }
    public ResponseCurve ResponseCurve { get; set; }
    public bool IsEnabled { get; set; }

    public float Evaluate(AIContext context);
    protected abstract float GetInput(AIContext context);
}
```

**预设 Considerations**:
- `FixedConsideration` - 固定值
- `BlackboardConsideration` - 黑板值
- `DistanceConsideration` - 距离目标
- `CooldownConsideration` - 冷却时间
- `RandomConsideration` - 随机值
- `StatConsideration` - StatSystem 属性值
- `HealthConsideration` - 生命值百分比
- `BuffConsideration` - Buff 状态
- `TimeConsideration` - TimeManager 时间
- `CombatStateConsideration` - 战斗状态
- `TargetCountConsideration` - 目标数量
- `ResourceConsideration` - 资源数量

---

### UtilityAction.cs
**用途**: 效用行动基类，定义可执行的 AI 行为

```csharp
public abstract class UtilityAction
{
    public string Name { get; }
    public bool IsEnabled { get; set; }
    public float Priority { get; set; }
    public float Cooldown { get; set; }
    public float LastScore { get; }
    public AIActionState State { get; }

    public float CalculateScore(AIContext context);
    public virtual bool CanExecute(AIContext context);
    public abstract void Start(AIContext context);
    public abstract AIActionResult Update(AIContext context, float deltaTime);
    public virtual void Stop(AIContext context);
    public void Reset();

    // 调试
    public Dictionary<string, float> GetDetailedScores();
}
```

**预设 Actions**:
- `IdleAction` / `WaitAction` - 空闲和等待
- `MoveToTargetAction` / `MoveToPositionAction` - 移动
- `PatrolAction` - 巡逻
- `FleeAction` - 逃跑
- `ReturnHomeAction` - 返回家
- `SetBlackboardAction` - 黑板操作

---

### UtilityBrain.cs
**用途**: 效用 AI 大脑，管理和评估所有行动

```csharp
public class UtilityBrain : MonoBehaviour, IAIBrain
{
    // 配置
    public float ReevaluationInterval { get; set; }
    public float MinScoreThreshold { get; }
    public float InertiaBonus { get; }
    public float Hysteresis { get; }

    // 行动管理
    public void AddAction(UtilityAction action);
    public bool RemoveAction(UtilityAction action);
    public UtilityAction GetAction(string name);
    public T GetAction<T>() where T : UtilityAction;
    public void ClearActions();

    // 属性
    public UtilityAction CurrentAction { get; }
    public IReadOnlyList<UtilityAction> Actions { get; }

    // 事件
    public event Action<UtilityAction, UtilityAction> OnActionChanged;
    public event Action<UtilityAction, float> OnEvaluationComplete;

    // 调试
    public Dictionary<string, ActionScoreInfo> GetAllScores();
}
```

**使用示例**:
```csharp
var brain = GetComponent<UtilityBrain>();

// 添加行动
brain.AddAction(new IdleAction());
brain.AddAction(new PatrolAction { Priority = 0.5f });
brain.AddAction(new MoveToTargetAction { Priority = 0.8f });

// 监听事件
brain.OnActionChanged += (prev, curr) => {
    Debug.Log($"Action: {prev?.Name} -> {curr?.Name}");
};

// 调试评分
foreach (var (name, info) in brain.GetAllScores())
{
    Debug.Log($"{name}: {info.Score:F2} {(info.IsCurrentAction ? "*" : "")}");
}
```

---

## GOAP (目标导向行动规划)

> **依赖**: crashkonijn/GOAP (Apache-2.0)
> **安装**: `https://github.com/crashkonijn/GOAP.git?path=Package#3.0.0`
> **编译符号**: `CRASHKONIJN_GOAP`

### GOAPBridge.cs
**用途**: GOAP 桥接器，检查依赖和提供集成工具

```csharp
public static class GOAPBridge
{
    public static bool IsGOAPInstalled { get; }
    public static void LogInstallInstructions();
}
```

### ZeroGOAPAgent.cs
**用途**: ZeroEngine GOAP Agent 包装器

```csharp
public class ZeroGOAPAgent : MonoBehaviour, IAIBrain
{
    public AIContext Context { get; }
    public AIBlackboard Blackboard { get; }

    // 黑板同步
    public void SyncFromBlackboard();
    public void SyncToBlackboard();
}
```

**预设 Goals**: `SurviveGoal`, `AttackEnemyGoal`, `HealGoal`, `PatrolGoal`, `FleeGoal`, `IdleGoal`, `FollowScheduleGoal`, `ReturnHomeGoal`

**预设 Actions**: `GOAPMoveToTargetAction`, `GOAPAttackAction`, `GOAPUseHealItemAction`, `GOAPFleeAction`, `GOAPFindTargetAction`, `GOAPWaitAction`, `GOAPReturnHomeAction`

---

## NPCSchedule (NPC 日程系统)

### ScheduleEntry.cs
**用途**: 日程条目，定义 NPC 在特定时间的行为

```csharp
public class ScheduleEntry
{
    public string Name { get; }
    public float StartHour { get; }
    public float EndHour { get; }
    public DayOfWeekMask DayOfWeekMask { get; }
    public SeasonMask SeasonMask { get; }
    public int Priority { get; }
    public ScheduleAction Action { get; }
    public List<ScheduleCondition> Conditions { get; }

    public bool IsActiveAtTime(float hour);
    public bool IsActiveOnDay(DayOfWeek day);
    public bool IsActiveInSeason(Season season);
    public bool CheckConditions(AIContext context);
}

[Flags]
public enum DayOfWeekMask { None = 0, Monday = 1, Tuesday = 2, ... All = 127 }

[Flags]
public enum SeasonMask { None = 0, Spring = 1, Summer = 2, Autumn = 4, Winter = 8, All = 15 }
```

---

### ScheduleAction.cs
**用途**: 日程行动基类

```csharp
public abstract class ScheduleAction
{
    public string Name { get; }
    public bool IsInterruptible { get; }

    public virtual void Start(AIContext context);
    public abstract AIActionResult Update(AIContext context, float deltaTime);
    public virtual void End(AIContext context);
}
```

**预设 Actions**:
- `IdleScheduleAction` / `WaitScheduleAction` - 空闲/等待
- `AnimationScheduleAction` - 播放动画
- `SequenceScheduleAction` - 顺序执行多个行动
- `RandomScheduleAction` - 随机选择行动
- `MoveToScheduleAction` - 移动到位置
- `SleepScheduleAction` - 睡眠恢复
- `WorkScheduleAction` - 工作
- `EatScheduleAction` - 进食
- `SocializeScheduleAction` - 社交
- `ShopkeeperScheduleAction` - 经营商店
- `PatrolScheduleAction` - 巡逻

---

### ScheduleCondition.cs
**用途**: 日程条件基类

```csharp
public abstract class ScheduleCondition
{
    public string Name { get; }
    public abstract bool Evaluate(AIContext context);
}
```

**预设 Conditions**:
- `WeatherCondition` - 天气条件
- `RelationshipCondition` - 好感度条件
- `QuestCondition` - 任务状态条件
- `ItemCondition` - 物品条件
- `BlackboardCondition` - 黑板条件
- `ProbabilityCondition` - 概率条件
- `CompositeCondition` - 组合条件 (AND/OR)

---

### NPCScheduleController.cs
**用途**: NPC 日程控制器

```csharp
public class NPCScheduleController : MonoBehaviour, IAIBrain
{
    public NPCScheduleSO Schedule { get; set; }
    public ScheduleEntry CurrentEntry { get; }
    public ScheduleAction CurrentAction { get; }

    // 覆盖控制
    public void SetOverride(ScheduleAction action, float duration);
    public void ClearOverride();
    public void ForceEntry(ScheduleEntry entry);

    // 事件
    public event Action<ScheduleEntry> OnScheduleChanged;
    public event Action<ScheduleAction> OnActionStarted;
    public event Action<ScheduleAction> OnActionEnded;
}
```

**使用示例**:
```csharp
var controller = GetComponent<NPCScheduleController>();
controller.Schedule = shopkeeperScheduleSO;

controller.OnScheduleChanged += entry => {
    Debug.Log($"Schedule: {entry.Name} ({entry.StartHour}:00 - {entry.EndHour}:00)");
};

// 战斗覆盖
if (isInCombat)
{
    controller.SetOverride(new FleeScheduleAction(), 30f);
}
```

---

### NPCScheduleSO.cs
**用途**: 日程 ScriptableObject

```csharp
[CreateAssetMenu(menuName = "ZeroEngine/AI/NPC Schedule")]
public class NPCScheduleSO : ScriptableObject
{
    public List<ScheduleEntry> Entries { get; }
    public ScheduleAction DefaultAction { get; }
    public SchedulePresetSO Preset { get; }

    public List<ScheduleEntry> GetActiveEntries(float hour, DayOfWeek day, Season season);
    public bool Validate(out List<string> errors);
}
```

---

## 技术决策

| AI 类型 | 方案 | 许可证 | 说明 |
|---------|------|--------|------|
| 行为树 (BT) | Unity Behavior | Unity | 推荐 Unity 6+ 官方包 |
| GOAP | crashkonijn/GOAP | Apache-2.0 | 通过桥接器集成 |
| UtilityAI | 自研 | ZeroEngine | 深度集成引擎系统 |
| NPCSchedule | 自研 | ZeroEngine | TimeManager 集成 |

---

## 性能说明

- **评分**: 7/10
- **已知问题**: `GetAllScores()` 方法每次调用会产生 GC 分配 (仅用于调试)
- **优化建议**:
  - 正式版本通过 `#if UNITY_EDITOR` 或条件编译禁用调试方法
  - UtilityBrain 的 `ReevaluationInterval` 默认 0.5s，避免每帧评估
  - NPCScheduleController 仅在时间变化时重新评估日程

---

## 版本历史

- **v1.17.0** (2026-01-05): 初始版本
  - AI/Common 通用接口和组件
  - AI/UtilityAI 效用 AI 系统 (自研)
  - AI/GOAP crashkonijn/GOAP 适配器
  - AI/NPCSchedule NPC 日程系统 (自研)
