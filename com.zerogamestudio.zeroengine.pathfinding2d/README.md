# ZeroEngine.Pathfinding2D

2D 平台寻路系统，为 2D 横版平台游戏提供智能跳跃寻路能力。

## 概述

该系统通过扫描场景中的平台碰撞体，自动生成平台节点网络（Point Graph），并计算跳跃链接（Jump Links），让 AI 角色能够智能地在多层平台之间跳跃寻路。

## 特性

- **自动平台检测**：扫描 Collider2D 生成平台节点
- **智能跳跃链接**：基于抛物线物理计算跳跃可达性
- **A* 寻路**：内置 A* 路径搜索算法
- **MoveCommand 系统**：将路径转换为具体移动指令
- **编辑器可视化**：在 Scene 视图实时预览节点和链接

## 核心组件

### PlatformGraphGenerator

扫描平台碰撞体，生成寻路节点网络。

```csharp
// 生成平台图
platformGraphGenerator.GeneratePlatformGraph();
```

### JumpLinkCalculator

计算节点之间的跳跃可达性，生成跳跃链接。

```csharp
// 生成跳跃链接
jumpLinkCalculator.GenerateJumpLinks();
```

### Platform2DPathfinder

执行寻路请求，生成 MoveCommand 序列。

```csharp
// 请求路径
pathfinder.RequestPath(start, end);

// 获取当前移动指令
var cmd = pathfinder.GetCurrentCommand();
```

### MoveCommand

移动指令数据结构，包含四种类型：
- `Walk`：行走到目标点
- `Jump`：跳跃到目标点（包含跳跃速度）
- `Fall`：自由落体
- `DropDown`：穿过单向平台下落

## 使用示例

### 在 PathfindingGraphUpdater 中集成

```csharp
// PathfindingGraphUpdater 会在房间初始化后自动调用
private void GeneratePlatformGraph()
{
    platformGraphGenerator.GeneratePlatformGraph();
    jumpLinkCalculator.GenerateJumpLinks();
}
```

### 在 AI 状态组件中使用

```csharp
public override void OnPhysicsUpdate()
{
    var cmd = summon.CurrentMoveCommand;
    if (cmd.HasValue)
    {
        switch (cmd.Value.CommandType)
        {
            case MoveCommandType.Walk:
                // 向目标移动
                float dir = Mathf.Sign(cmd.Value.Target.x - position.x);
                movement.SetVelocityX(speed * dir);
                break;

            case MoveCommandType.Jump:
                // 使用计算好的跳跃速度
                movement.SetVelocityY(cmd.Value.JumpVelocityY);
                movement.SetVelocityX(cmd.Value.JumpVelocityX);
                break;
        }
    }
}
```

## 配置

### PlatformGraphConfig

| 参数 | 说明 | 默认值 |
|------|------|--------|
| NodeSpacing | 节点间距 | 1.5m |
| EdgeInset | 边缘内缩 | 0.3m |
| GroundLayer | 地面层 | - |
| OneWayPlatformLayer | 单向平台层 | - |

### JumpLinkConfig

| 参数 | 说明 | 默认值 |
|------|------|--------|
| MaxJumpVelocity | 最大跳跃速度 | 14 |
| MaxHorizontalDistance | 最大水平跳跃距离 | 6m |
| MaxJumpHeight | 最大跳跃高度 | 4m |
| GravityScale | 重力缩放 | 3 |

## 依赖

- ZeroEngine.Core >= 2.0.0

## 版本

- 1.0.0 - 初始版本
