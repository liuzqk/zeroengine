# ZeroEngine.FSM API 文档

> **用途**: 本文档面向AI助手，提供FSM（有限状态机）模块的快速参考。
> **版本**: v1.0.0+
> **最后更新**: 2025-12-31

---

## 目录结构

```
FSM/
├── StateMachine.cs    # 状态机核心
└── IStateNode.cs      # 状态节点接口
```

---

## IStateNode.cs

**用途**: 状态节点接口

```csharp
public interface IStateNode
{
    void OnCreate(StateMachine machine);  // 创建时调用
    void OnEnter();                       // 进入状态时调用
    void OnUpdate();                      // 每帧调用
    void OnExit();                        // 退出状态时调用
}
```

---

## StateMachine.cs

**用途**: 有限状态机实现

```csharp
public class StateMachine
{
    public System.Object Owner { get; }     // 状态机所有者
    public string CurrentNode { get; }      // 当前状态名
    public string PreviousNode { get; }     // 上一个状态名
    
    // 构造
    StateMachine(System.Object owner);
    
    // 状态管理
    void AddNode<TNode>() where TNode : IStateNode;
    void AddNode(IStateNode stateNode);
    
    // 状态切换
    void Run<TNode>() where TNode : IStateNode;     // 启动状态机
    void Run(Type entryNode);
    void Run(string entryNode);
    void ChangeState<TNode>() where TNode : IStateNode;
    void ChangeState(Type nodeType);
    void ChangeState(string nodeName);
    
    // 黑板数据
    void SetBlackboardValue(string key, System.Object value);
    System.Object GetBlackboardValue(string key);
    
    // 更新
    void Update();  // 需每帧调用
}
```

---

## 使用示例

```csharp
// 1. 定义状态
public class IdleState : IStateNode
{
    private StateMachine _machine;
    
    public void OnCreate(StateMachine machine) => _machine = machine;
    public void OnEnter() => Debug.Log("进入Idle状态");
    public void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            _machine.ChangeState<JumpState>();
    }
    public void OnExit() => Debug.Log("离开Idle状态");
}

// 2. 创建并运行状态机
var fsm = new StateMachine(this);
fsm.AddNode<IdleState>();
fsm.AddNode<JumpState>();
fsm.AddNode<FallState>();
fsm.Run<IdleState>();

// 3. 每帧更新
void Update() => fsm.Update();

// 4. 使用黑板共享数据
fsm.SetBlackboardValue("JumpForce", 10f);
var force = (float)fsm.GetBlackboardValue("JumpForce");
```

---

## 设计说明

- **使用类型全名作为Key**: 状态使用 `Type.FullName` 作为标识
- **黑板模式**: 状态间通过黑板共享数据，避免直接依赖
- **无自动Update**: 需要手动调用 `Update()` 以支持不同更新频率
