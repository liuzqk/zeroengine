# ZeroEngine.Network API 文档

> **用途**: 本文档面向AI助手，提供Network模块的快速参考。

---

## 目录结构

```
Network/
├── ZeroNetworkManager.cs      # NGO 网络管理器封装
├── ZeroNetworkBehaviour.cs    # (v1.1.0+) NetworkBehaviour 便捷基类
├── ReconnectionHandler.cs     # (v1.1.0+) 断线重连处理器
├── LobbyManager.cs            # Unity Gaming Services 大厅管理
├── ChatManager.cs             # 网络聊天系统
├── Config/
│   └── ServerConfig.cs        # 服务器配置 ScriptableObject
└── Core/
    └── NetworkArgs.cs         # 命令行参数解析
```

---

## 编译宏

| 宏定义 | 依赖 |
|--------|------|
| `ZEROENGINE_NETCODE` | Netcode for GameObjects (NGO) |
| `ZEROENGINE_UGS` | Unity Gaming Services (Lobby/Relay) |

---

## ZeroNetworkManager.cs

**用途**: NGO 的 NetworkManager 封装，支持配置化启动

```csharp
public class ZeroNetworkManager : NetworkManager  // 或 MonoBehaviour（无NGO时）
{
    public static ZeroNetworkManager Instance { get; }
    
    // 状态
    public ConnectionState CurrentConnectionState { get; }  // Offline, Connecting, Connected, Failed
    public ushort CurrentPort { get; }
    public string CurrentIP { get; }
    
    // 连接方法
    void StartHostGame();      // 启动Host（主机+客户端）
    void StartClientGame();    // 启动Client
    void StartServerGame();    // 启动Dedicated Server
    void DisconnectGame();     // 断开连接
    
    // 玩家名称
    void SetPlayerName(ulong clientId, string name);
    string GetPlayerName(ulong clientId);
}
```

**配置优先级**: 命令行参数 > ServerConfig > 默认值

**命令行参数**:
- `-ip <address>`: 服务器IP
- `-port <number>`: 端口号
- `-server`: 以服务器模式启动

---

## LobbyManager.cs

**用途**: Unity Gaming Services 大厅和Relay集成

```csharp
public class LobbyManager : Singleton<LobbyManager>
{
    bool IsSignedIn { get; }
    
    event Action<List<Lobby>> OnLobbyListUpdated;
    
    Task InitializeAsync();                           // 初始化UGS并匿名登录
    Task CreateLobby(string name, int maxPlayers, bool isPrivate = false);
    Task RefreshLobbyList();                          // 刷新大厅列表
    Task JoinLobby(string lobbyId);                   // 加入大厅
    Task LeaveLobby();                                // 离开大厅
}
```

---

## ChatManager.cs

**用途**: 网络聊天系统

```csharp
public class ChatManager : NetworkBehaviour  // 或 MonoBehaviour（无NGO时）
{
    public static ChatManager Instance { get; }
    
    public static event Action<ulong, string, ChatChannel> OnMessageReceived;
    
    void SendMessage(string text, ChatChannel channel = ChatChannel.World);
}

public enum ChatChannel { World, Team, Private, System }
```

---

## ServerConfig（ScriptableObject）

**菜单**: ZeroEngine > Network > Server Config

```csharp
public class ServerConfig : ScriptableObject
{
    public string DefaultIP = "127.0.0.1";
    public ushort DefaultPort = 7777;
    public ServerEnvironment Environment;  // Development, Staging, Production
    
    // 性能设置
    public int TargetFrameRate = 60;
    public bool EnableVSync = true;
    public bool OptimizeForHeadless = true;
    public int HeadlessTargetFrameRate = 30;
}
```

---

## 使用示例

```csharp
// 1. 启动Host
ZeroNetworkManager.Instance.StartHostGame();

// 2. 监听连接状态
EventManager.Subscribe(GameEvents.NetworkConnected, () => Debug.Log("已连接"));
EventManager.Subscribe<ulong>(GameEvents.NetworkPlayerJoined, OnPlayerJoined);

// 3. 使用大厅系统
await LobbyManager.Instance.InitializeAsync();
await LobbyManager.Instance.CreateLobby("My Game", 4);

// 4. 发送聊天消息
ChatManager.Instance.SendMessage("Hello!", ChatChannel.World);
```

---

## ZeroNetworkBehaviour.cs (v1.1.0+)

**用途**: NetworkBehaviour 的便捷封装基类

```csharp
public abstract class ZeroNetworkBehaviour : NetworkBehaviour  // 或 MonoBehaviour（无NGO时）
{
    // 状态属性
    bool IsAuthoritative { get; }  // 是否为服务器/主机
    bool IsLocalOwner { get; }     // 是否为本地拥有者

    // 便捷方法 - 条件执行
    void ServerOnly(Action action);   // 仅在服务器执行
    void OwnerOnly(Action action);    // 仅在拥有者客户端执行
    void RemoteOnly(Action action);   // 仅在非拥有者客户端执行

    // 所有权
    void RequestOwnership();  // 请求获取所有权

    // 生命周期钩子
    virtual void OnNetworkReady();      // 网络生成后调用
    virtual void OnOwnershipGained();   // 获得所有权时调用
    virtual void OnOwnershipLost();     // 失去所有权时调用
}
```

**使用示例**:
```csharp
public class MyNetworkObject : ZeroNetworkBehaviour
{
    protected override void OnNetworkReady()
    {
        // 初始化逻辑
    }

    public void TakeDamage(int damage)
    {
        ServerOnly(() =>
        {
            // 仅在服务器执行伤害计算
            _health.Value -= damage;
        });
    }

    private void Update()
    {
        OwnerOnly(() =>
        {
            // 仅在拥有者客户端处理输入
            HandleInput();
        });
    }
}
```

---

## ReconnectionHandler.cs (v1.1.0+)

**用途**: 自动断线重连处理器，支持指数退避

### ReconnectionState

```csharp
public enum ReconnectionState
{
    Idle,        // 空闲
    Attempting,  // 正在尝试重连
    Success,     // 重连成功
    Failed       // 所有尝试均失败
}
```

### ReconnectionEventArgs

```csharp
public struct ReconnectionEventArgs
{
    public ReconnectionState State;
    public int AttemptNumber;     // 当前尝试次数
    public int MaxAttempts;       // 最大尝试次数
    public float NextAttemptIn;   // 距下次尝试的秒数
    public string FailReason;     // 失败原因
}
```

### ReconnectionHandler

```csharp
public class ReconnectionHandler : MonoBehaviour
{
    // 配置 (Inspector)
    bool EnableAutoReconnect;   // 是否启用自动重连
    int MaxAttempts = 5;        // 最大尝试次数
    float InitialDelay = 1f;    // 初始延迟（秒）
    float MaxDelay = 30f;       // 最大延迟（秒）
    float DelayMultiplier = 2f; // 延迟倍增因子

    // 状态
    ReconnectionState CurrentState { get; }

    // 事件
    event Action<ReconnectionEventArgs> OnReconnectionStateChanged;

    // 方法
    void StartReconnection();   // 手动开始重连
    void StopReconnection();    // 停止重连尝试
}
```

**使用示例**:
```csharp
// 监听重连状态
reconnectionHandler.OnReconnectionStateChanged += args =>
{
    switch (args.State)
    {
        case ReconnectionState.Attempting:
            ShowUI($"Reconnecting... ({args.AttemptNumber}/{args.MaxAttempts})");
            break;
        case ReconnectionState.Success:
            HideUI();
            break;
        case ReconnectionState.Failed:
            ShowUI("Connection lost. Please restart.");
            break;
    }
};

// 手动触发重连
reconnectionHandler.StartReconnection();
```
