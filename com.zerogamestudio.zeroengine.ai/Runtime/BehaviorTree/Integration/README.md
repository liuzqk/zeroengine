# ZeroEngine.BehaviorTree.Integration API 文档

> **用途**: BT 与 FSM 集成模块文档
> **版本**: v1.3.0+
> **最后更新**: 2025-12-31

---

## 目录结构

```
Integration/
├── FSMBlackboardAdapter.cs  # FSM 黑板适配器
├── RunFSMNode.cs            # BT 中运行 FSM 的节点
├── BTStateNode.cs           # FSM 中运行 BT 的状态
└── README.md                # 本文档
```

---

## 集成模式

### 模式 1: BT 中运行 FSM (RunFSMNode)

行为树作为主控制器，FSM 作为子系统执行具体状态逻辑。

```csharp
var tree = new BehaviorTree(this);

tree.SetRoot(
    new Selector()
        // 战斗模式：使用 FSM 管理战斗状态
        .AddChild(
            new Conditional(ctx => ctx.Blackboard.GetValue<bool>("InCombat"))
                .SetChild(
                    new RunFSMNode(
                        machine => {
                            machine.AddNode<CombatIdleState>();
                            machine.AddNode<AttackState>();
                            machine.AddNode<DodgeState>();
                            machine.Run<CombatIdleState>();
                        },
                        (ctx, machine) => !ctx.Blackboard.GetValue<bool>("InCombat")
                    )
                )
        )
        // 探索模式
        .AddChild(exploreSequence)
);
```

#### Builder 模式

```csharp
var fsmNode = new RunFSMNodeBuilder()
    .ConfigureMachine(machine => {
        machine.AddNode<PatrolState>();
        machine.AddNode<ChaseState>();
        machine.Run<PatrolState>();
    })
    .CompleteOnState<ChaseState>()  // 到达 ChaseState 时完成
    .ShareBlackboard(true)
    .Build();
```

---

### 模式 2: FSM 中运行 BT (BTStateNode)

FSM 状态内部使用行为树控制细节逻辑。

```csharp
// 巡逻状态使用行为树
public class PatrolBTState : BTStateNode
{
    protected override void ConfigureTree(BehaviorTree tree)
    {
        tree.SetRoot(
            new Repeater(-1).SetChild(
                new Sequence()
                    .AddChild(new ActionNode(MoveToNextPoint))
                    .AddChild(new WaitNode(1.5f))
                    .AddChild(new ActionNode(LookAround))
            )
        );
    }

    private NodeState MoveToNextPoint(BTContext ctx)
    {
        var points = ctx.Blackboard.GetValue<Vector3[]>("PatrolPoints");
        var index = ctx.Blackboard.GetValue<int>("PatrolIndex");

        // 移动逻辑...

        ctx.Blackboard.SetValue("PatrolIndex", (index + 1) % points.Length);
        return NodeState.Success;
    }

    private NodeState LookAround(BTContext ctx)
    {
        // 检查是否发现敌人
        bool enemyFound = CheckForEnemies();

        if (enemyFound)
        {
            ctx.Blackboard.SetValue("EnemyDetected", true);
            Machine.ChangeState<AlertState>();  // 切换 FSM 状态
        }

        return NodeState.Success;
    }

    protected override void OnTreeCompleted(NodeState finalState)
    {
        // 巡逻完一轮后自动重启
        Tree.Restart();
    }
}

// 使用
var fsm = new StateMachine(this);
fsm.AddNode(new PatrolBTState());
fsm.AddNode<AlertState>();
fsm.Run<PatrolBTState>();
```

---

### 模式 3: 共享 Blackboard

通过 `FSMBlackboardAdapter` 实现 BT 和 FSM 数据共享。

```csharp
// 手动创建共享适配器
var fsm = new StateMachine(this);
var sharedBlackboard = new FSMBlackboardAdapter(fsm);
var tree = new BehaviorTree(this, sharedBlackboard);

// BT 设置的值，FSM 可以读取
tree.Blackboard.SetValue("Target", enemy);

// FSM 设置的值，BT 可以读取
fsm.SetBlackboardValue("Health", 100f);
var health = tree.Blackboard.GetValue<float>("Health");
```

---

## 类型参考

### FSMBlackboardAdapter

```csharp
public class FSMBlackboardAdapter : IBlackboard
{
    FSMBlackboardAdapter(StateMachine machine);

    // 实现 IBlackboard 接口
    void SetValue<T>(string key, T value);
    T GetValue<T>(string key);
    bool TryGetValue<T>(string key, out T value);
    bool HasKey(string key);
    bool RemoveKey(string key);
    void Clear();
    event Action<string, object> OnValueChanged;
}
```

### RunFSMNode

```csharp
public class RunFSMNode : BTLeaf
{
    StateMachine Machine { get; }  // 当前运行的状态机

    RunFSMNode(
        Action<StateMachine> machineBuilder,
        Func<BTContext, StateMachine, bool> completionCondition = null,
        bool useSharedBlackboard = true
    );
}
```

### BTStateNode

```csharp
public abstract class BTStateNode : IStateNode
{
    StateMachine Machine { get; }     // 所属状态机
    BehaviorTree Tree { get; }        // 行为树实例
    IBlackboard Blackboard { get; }   // 共享黑板

    // 子类必须实现
    abstract void ConfigureTree(BehaviorTree tree);

    // 可选重写
    virtual void OnTreeCompleted(NodeState finalState);
}
```

---

## 最佳实践

### 1. 选择合适的集成模式

| 场景 | 推荐模式 |
|------|----------|
| AI 决策树复杂，状态逻辑简单 | BT 为主，FSM 作为叶节点 |
| 状态逻辑复杂，需要细粒度控制 | FSM 为主，BT 作为状态内逻辑 |
| 高层状态切换 + 低层决策 | 混合使用，共享 Blackboard |

### 2. 黑板数据命名规范

```csharp
// 推荐：使用前缀区分数据来源
"BT_TargetPosition"   // BT 设置的数据
"FSM_CurrentState"    // FSM 设置的数据
"Shared_Health"       // 共享数据
```

### 3. 避免循环依赖

```csharp
// ❌ 错误：BT 和 FSM 互相等待
// BT: 等待 FSM 完成
// FSM: 等待 BT 信号

// ✅ 正确：单向数据流
// BT 做决策 → FSM 执行 → 通过 Blackboard 反馈结果
```

---

## 示例：完整 AI 控制器

```csharp
public class CompleteAIController : MonoBehaviour
{
    private BehaviorTree _mainTree;

    void Start()
    {
        _mainTree = new BehaviorTree(this);

        _mainTree.SetRoot(
            new Selector()
                // 紧急情况：血量低
                .AddChild(
                    new Conditional(
                        ctx => ctx.Blackboard.GetValue<float>("Health") < 20f,
                        AbortMode.LowerPriority
                    ).SetChild(
                        new ActionNode(ctx => {
                            Debug.Log("寻找掩体!");
                            return NodeState.Running;
                        })
                    )
                )
                // 战斗模式
                .AddChild(
                    new Conditional(ctx => ctx.Blackboard.GetValue<bool>("InCombat"))
                        .SetChild(
                            new RunFSMNodeBuilder()
                                .ConfigureMachine(ConfigureCombatFSM)
                                .CompleteWhen((ctx, m) => !ctx.Blackboard.GetValue<bool>("InCombat"))
                                .Build()
                        )
                )
                // 巡逻模式
                .AddChild(
                    new Repeater(-1).SetChild(
                        new Sequence()
                            .AddChild(new ActionNode(Patrol))
                            .AddChild(new WaitNode(2f))
                    )
                )
        );

        _mainTree.Start();
    }

    void ConfigureCombatFSM(StateMachine machine)
    {
        machine.AddNode<CombatIdleState>();
        machine.AddNode<AttackState>();
        machine.AddNode<DodgeState>();
        machine.Run<CombatIdleState>();
    }

    NodeState Patrol(BTContext ctx)
    {
        // 巡逻逻辑...
        return NodeState.Success;
    }

    void Update()
    {
        _mainTree.Tick();
    }
}
```
