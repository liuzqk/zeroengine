# ZeroEngine.Command API 文档

> **用途**: 本文档面向AI助手，提供Command（命令模式）模块的快速参考。
> **版本**: v1.0.0+
> **最后更新**: 2025-12-31

---

## 目录结构

```
Command/
├── CommandManager.cs   # 命令管理器（单例）
├── ICommand.cs         # 命令接口
└── WaitCommand.cs      # 等待命令示例
```

---

## ICommand.cs

**用途**: 命令接口，支持异步执行和撤销

```csharp
public interface ICommand
{
#if UNITASK
    UniTask Execute();
    UniTask Undo();
#else
    Task Execute();
    Task Undo();
#endif
}
```

---

## CommandManager.cs

**用途**: 命令管理器，支持 Undo/Redo

```csharp
public class CommandManager : Singleton<CommandManager>
{
    bool IsExecuting { get; }  // 是否正在执行命令
    
#if UNITASK
    async UniTask ExecuteCommand(ICommand command);  // 执行命令
    async UniTask Undo();                             // 撤销
    async UniTask Redo();                             // 重做
#else
    async Task ExecuteCommand(ICommand command);
    async Task Undo();
    async Task Redo();
#endif
    
    void ClearHistory();  // 清除历史
}
```

---

## 编译宏

| 宏定义 | 效果 |
|--------|------|
| `UNITASK` | 使用 UniTask 替代 Task |

---

## 使用示例

```csharp
// 1. 定义命令
public class MoveCommand : ICommand
{
    private Transform _target;
    private Vector3 _from;
    private Vector3 _to;
    
    public MoveCommand(Transform target, Vector3 to)
    {
        _target = target;
        _from = target.position;
        _to = to;
    }
    
    public async Task Execute()
    {
        _target.position = _to;
        await Task.CompletedTask;
    }
    
    public async Task Undo()
    {
        _target.position = _from;
        await Task.CompletedTask;
    }
}

// 2. 执行命令
await CommandManager.Instance.ExecuteCommand(new MoveCommand(player, newPos));

// 3. 撤销/重做
await CommandManager.Instance.Undo();
await CommandManager.Instance.Redo();
```

---

## 设计说明

- **执行新命令时清除Redo历史**: 防止分支历史
- **异步支持**: 支持耗时操作（动画、网络请求等）
- **执行锁**: `IsExecuting` 防止并发执行
