# ZeroEngine.InputSystem API 文档

> **用途**: 本文档面向AI助手，提供InputSystem模块的快速参考。
> **版本**: v1.0.0+
> **最后更新**: 2025-12-31

---

## 目录结构

```
InputSystem/
└── InputManager.cs   # 输入管理器（单例）
```

---

## 依赖

- **Unity Input System Package**: 需要安装 `com.unity.inputsystem`

---

## InputManager.cs

**用途**: 全局输入管理器，管理 InputActionAsset 和 Action Maps

```csharp
public class InputManager : Singleton<InputManager>
{
    [SerializeField] InputActionAsset _inputActionAsset;
    
    // Action Maps
    public InputActionMap PlayerActions { get; }  // 游戏操作
    public InputActionMap UIActions { get; }      // UI操作
    
    // 启用/禁用
    void EnableAllActions();
    void DisableAllActions();
    
    // 模式切换
    void SwitchToGameplayMode();  // 禁用UI，启用Player
    void SwitchToUIMode();        // 禁用Player，启用UI
}
```

---

## InputActionAsset 配置

需要在 InputActionAsset 中创建以下 Action Maps：

| Action Map | 用途 |
|------------|------|
| `Player` | 游戏角色控制（移动、攻击等） |
| `UI` | 界面操作（导航、确认等） |

---

## 使用示例

```csharp
// 1. 获取Action并绑定
var moveAction = InputManager.Instance.PlayerActions.FindAction("Move");
moveAction.performed += ctx => {
    Vector2 input = ctx.ReadValue<Vector2>();
    // 处理移动
};

// 2. 切换输入模式
InputManager.Instance.SwitchToUIMode();    // 打开菜单时
InputManager.Instance.SwitchToGameplayMode();  // 关闭菜单时

// 3. 禁用所有输入（过场动画等）
InputManager.Instance.DisableAllActions();
```

---

## 设计说明

- **Action Maps分离**: Player和UI互斥，避免输入冲突
- **自动启用**: OnEnable时自动启用所有Actions
- **Inspector配置**: InputActionAsset通过Inspector分配
