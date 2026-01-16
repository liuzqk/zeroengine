# ZeroEngine.BehaviorTree API 文档

> **用途**: 本文档面向AI助手，提供行为树模块的快速参考。
> **版本**: v1.3.0+
> **最后更新**: 2025-12-31

---

## 目录结构

```
BehaviorTree/
├── Core/
│   ├── NodeState.cs        # 节点状态枚举
│   ├── AbortMode.cs        # 中断模式枚举
│   ├── IBlackboard.cs      # 黑板接口
│   ├── Blackboard.cs       # 黑板实现
│   ├── BTContext.cs        # 执行上下文
│   ├── IBTNode.cs          # 节点接口
│   ├── BTNode.cs           # 节点基类
│   └── BehaviorTree.cs     # 行为树控制器
├── Composites/
│   ├── BTComposite.cs      # 组合节点基类
│   ├── Sequence.cs         # 顺序节点
│   ├── Selector.cs         # 选择节点
│   └── Parallel.cs         # 并行节点
├── Decorators/
│   ├── BTDecorator.cs      # 装饰节点基类
│   ├── Repeater.cs         # 重复节点
│   ├── Inverter.cs         # 反转节点
│   ├── Conditional.cs      # 条件节点
│   ├── AlwaysSucceed.cs    # 始终成功
│   └── AlwaysFail.cs       # 始终失败
├── Leaves/
│   ├── BTLeaf.cs           # 叶节点基类
│   ├── ActionNode.cs       # 动作节点
│   ├── WaitNode.cs         # 等待节点
│   └── LogNode.cs          # 日志节点
└── Integration/            # FSM 集成 (v1.3.0+)
    ├── FSMBlackboardAdapter.cs
    ├── RunFSMNode.cs
    └── BTStateNode.cs
```

---

## 核心类型

### NodeState (枚举)

```csharp
public enum NodeState
{
    Running,   // 执行中，下帧继续
    Success,   // 执行成功
    Failure    // 执行失败
}
```

### AbortMode (枚举)

```csharp
[Flags]
public enum AbortMode
{
    None = 0,           // 不中断
    Self = 1,           // 中断自身
    LowerPriority = 2,  // 中断低优先级兄弟
    Both = 3            // 两者都中断
}
```

### IBlackboard (接口)

```csharp
public interface IBlackboard
{
    void SetValue<T>(string key, T value);
    T GetValue<T>(string key);
    bool TryGetValue<T>(string key, out T value);
    bool HasKey(string key);
    bool RemoveKey(string key);
    void Clear();
    event Action<string, object> OnValueChanged;
}
```

### BehaviorTree (控制器)

```csharp
public class BehaviorTree
{
    public object Owner { get; }
    public IBlackboard Blackboard { get; }
    public bool IsRunning { get; }
    public NodeState CurrentState { get; }

    BehaviorTree(object owner, IBlackboard blackboard = null);
    BehaviorTree SetRoot(IBTNode root);
    void Start();
    void Stop();
    void Tick(float deltaTime);
    void Tick();  // 使用 Time.deltaTime
    void Restart();
}
```

---

## 节点类型

### 组合节点 (Composite)

| 节点 | 行为 |
|------|------|
| `Sequence` | 顺序执行，全部成功则成功，任一失败则失败 |
| `Selector` | 顺序尝试，任一成功则成功，全部失败则失败 |
| `Parallel` | 并行执行，按策略判定结果 |

```csharp
// Fluent API
new Sequence()
    .AddChild(node1)
    .AddChild(node2)
    .AddChildren(node3, node4);
```

### 装饰节点 (Decorator)

| 节点 | 行为 |
|------|------|
| `Repeater(n)` | 重复 n 次，-1 表示无限 |
| `Inverter` | 反转结果 (Success↔Failure) |
| `Conditional` | 条件判断，支持中断 |
| `AlwaysSucceed` | 始终返回成功 |
| `AlwaysFail` | 始终返回失败 |

```csharp
// Fluent API
new Conditional(ctx => ctx.Blackboard.GetValue<bool>("HasTarget"), AbortMode.LowerPriority)
    .SetChild(attackSequence);
```

### 叶节点 (Leaf)

| 节点 | 行为 |
|------|------|
| `ActionNode` | 执行委托并返回状态 |
| `WaitNode` | 等待指定秒数 |
| `LogNode` | 输出日志 (调试用) |

```csharp
new ActionNode(ctx => {
    // 执行逻辑
    return NodeState.Success;
});

new WaitNode(2.5f);  // 等待 2.5 秒
```

---

## 使用示例

### 基础用法

```csharp
using UnityEngine;
using ZeroEngine.BehaviorTree;

public class AIController : MonoBehaviour
{
    private BehaviorTree _tree;

    void Start()
    {
        _tree = new BehaviorTree(this);

        _tree.SetRoot(
            new Selector()
                .AddChild(
                    new Conditional(ctx => ctx.Blackboard.GetValue<bool>("EnemyVisible"))
                        .SetChild(new Sequence()
                            .AddChild(new ActionNode(ctx => {
                                Debug.Log("攻击敌人!");
                                return NodeState.Success;
                            }))
                        )
                )
                .AddChild(
                    new Repeater(-1).SetChild(
                        new Sequence()
                            .AddChild(new ActionNode(ctx => {
                                Debug.Log("巡逻中...");
                                return NodeState.Success;
                            }))
                            .AddChild(new WaitNode(2f))
                    )
                )
        );

        _tree.Start();
    }

    void Update()
    {
        _tree.Tick();

        // 模拟敌人检测
        if (Input.GetKeyDown(KeyCode.E))
        {
            _tree.Blackboard.SetValue("EnemyVisible", true);
        }
    }
}
```

### 条件中断

```csharp
// 高优先级条件会中断低优先级行为
new Selector()
    .AddChild(
        new Conditional(
            ctx => ctx.Blackboard.GetValue<float>("Health") < 20f,
            AbortMode.LowerPriority  // 血量低时中断其他行为
        ).SetChild(new ActionNode(ctx => {
            Debug.Log("逃跑!");
            return NodeState.Running;
        }))
    )
    .AddChild(
        // 正常战斗/巡逻逻辑
        normalBehavior
    );
```

### 并行执行

```csharp
// 同时播放动画和移动
new Parallel(ParallelPolicy.RequireAll, ParallelPolicy.RequireOne)
    .AddChild(new ActionNode(ctx => {
        // 播放动画
        return NodeState.Running;
    }))
    .AddChild(new ActionNode(ctx => {
        // 移动到目标
        return NodeState.Running;
    }));
```

---

## 自定义节点

### 自定义叶节点

```csharp
public class MoveToNode : BTLeaf
{
    private Transform _target;
    private float _speed;

    public MoveToNode(Transform target, float speed)
    {
        _target = target;
        _speed = speed;
        Name = "MoveTo";
    }

    protected override NodeState OnExecute(BTContext context)
    {
        var owner = context.Owner as MonoBehaviour;
        var direction = (_target.position - owner.transform.position).normalized;
        owner.transform.position += direction * _speed * context.DeltaTime;

        if (Vector3.Distance(owner.transform.position, _target.position) < 0.1f)
        {
            return NodeState.Success;
        }
        return NodeState.Running;
    }
}
```

### 自定义装饰节点

```csharp
public class CooldownDecorator : BTDecorator
{
    private float _cooldown;
    private float _lastExecuteTime;

    public CooldownDecorator(float cooldown)
    {
        _cooldown = cooldown;
        Name = $"Cooldown({cooldown}s)";
    }

    protected override NodeState OnExecute(BTContext context)
    {
        if (Time.time - _lastExecuteTime < _cooldown)
        {
            return NodeState.Failure;
        }

        var result = _child.Execute(context);

        if (result == NodeState.Success)
        {
            _lastExecuteTime = Time.time;
        }

        return result;
    }
}
```

---

## 与 FSM 集成 (v1.3.0+)

参见 `Integration/README.md`

---

## 调试技巧

1. 使用 `LogNode` 跟踪执行流程
2. 为节点设置 `Name` 属性便于调试
3. 监听 `Blackboard.OnValueChanged` 事件

```csharp
_tree.Blackboard.OnValueChanged += (key, value) => {
    Debug.Log($"[Blackboard] {key} = {value}");
};
```
