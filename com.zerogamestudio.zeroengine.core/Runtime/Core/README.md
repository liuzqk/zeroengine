# ZeroEngine.Core API 文档

> **用途**: 本文档面向AI助手，提供Core模块的快速参考。
> **版本**: v1.0.0+ (事件 v1.9.0+)
> **最后更新**: 2026-01-03

---

## 目录结构

```
Core/
├── Singleton.cs       # 单例基类（普通/持久）
├── EventManager.cs    # 事件系统
├── ObjectPool.cs      # 对象池（如有）
└── ...
```

---

## Singleton.cs

**用途**: MonoBehaviour单例基类

### Singleton<T> (非持久)

```csharp
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; }  // 自动创建或查找
    
    protected virtual void Awake();     // 子类可重写
    protected virtual void OnDestroy();
}
```

**使用示例**:
```csharp
public class GameManager : Singleton<GameManager>
{
    protected override void Awake()
    {
        base.Awake();
        // 初始化逻辑
    }
}

// 访问
GameManager.Instance.DoSomething();
```

### PersistentSingleton<T> (跨场景持久)

```csharp
public class PersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; }  // 自动DontDestroyOnLoad
}
```

**使用示例**:
```csharp
public class AudioManager : PersistentSingleton<AudioManager>
{
    // 跨场景保留
}
```

---

## EventManager.cs

**用途**: 解耦模块间通信的静态事件系统

```csharp
public static class EventManager
{
    // 无参数事件
    void Subscribe(string eventName, Action listener);
    void Unsubscribe(string eventName, Action listener);
    void Trigger(string eventName);
    
    // 单参数事件
    void Subscribe<T>(string eventName, Action<T> listener);
    void Unsubscribe<T>(string eventName, Action<T> listener);
    void Trigger<T>(string eventName, T arg);
    
    // 双参数事件
    void Subscribe<T1, T2>(string eventName, Action<T1, T2> listener);
    void Unsubscribe<T1, T2>(string eventName, Action<T1, T2> listener);
    void Trigger<T1, T2>(string eventName, T1 arg1, T2 arg2);
    
    void Clear();  // 清除所有事件（场景切换时调用）
}
```

**使用示例**:
```csharp
// 订阅
EventManager.Subscribe<int>(GameEvents.ExpGained, OnExpGained);

// 触发
EventManager.Trigger(GameEvents.ExpGained, 100);

// 取消订阅
EventManager.Unsubscribe<int>(GameEvents.ExpGained, OnExpGained);
```

---

## GameEvents

**用途**: 预定义的事件名常量

```csharp
public static class GameEvents
{
    // Core
    public const string GameStarted = "Game.Started";
    public const string GamePaused = "Game.Paused";
    public const string GameResumed = "Game.Resumed";
    public const string SceneLoaded = "System.SceneLoaded";

    // Battle
    public const string BattleStart = "Battle.Start";
    public const string BattleEnd = "Battle.End";
    public const string TurnStart = "Battle.TurnStart";
    public const string TurnEnd = "Battle.TurnEnd";
    
    // Character
    public const string PlayerSpawned = "Character.PlayerSpawned";
    public const string CharacterDied = "Character.Died";
    public const string ExpGained = "Character.ExpGained";

    // Quest
    public const string QuestAccepted = "Quest.Accepted";
    public const string QuestCompleted = "Quest.Completed";
    public const string QuestFailed = "Quest.Failed";
    public const string QuestProgressChanged = "Quest.ProgressChanged";

    // Inventory
    public const string ItemObtained = "Item.Obtained";
    public const string ItemRemoved = "Item.Removed";
    public const string InventoryUpdated = "Inventory.Updated";

    // Currency (v1.9.0+)
    public const string CurrencyGained = "Currency.Gained";
    public const string CurrencySpent = "Currency.Spent";

    // Network
    public const string NetworkConnected = "Network.Connected";
    public const string NetworkDisconnected = "Network.Disconnected";
    public const string NetworkPlayerJoined = "Network.PlayerJoined";
    public const string NetworkPlayerLeft = "Network.PlayerLeft";
}
```
