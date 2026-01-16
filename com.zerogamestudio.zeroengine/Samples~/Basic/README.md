# ZeroEngine Basic Examples

基础示例，演示 ZeroEngine 核心模块的使用方法。

## 示例列表

### StatSystemExample
演示属性系统的基本用法：
- 初始化属性（生命、攻击等）
- 添加/移除修饰器
- 监听属性变化事件

### BuffSystemExample
演示 Buff 系统的基本用法：
- 添加/移除 Buff
- Buff 堆叠和持续时间
- 监听 Buff 事件
- 使用 BuffUtils 创建运行时 Buff

**快捷键：**
- `R` - 移除所有 Buff

### InventoryExample
演示背包系统的基本用法：
- 添加/移除物品
- 查询物品（按类型、稀有度）
- 背包排序
- 监听背包事件

**快捷键：**
- `A` - 添加测试物品
- `D` - 移除测试物品
- `S` - 排序背包
- `C` - 清空背包

### QuestSystemExample
演示任务系统的基本用法：
- 接受/提交/放弃任务
- v1.2.0+ 条件系统
- 进度查询
- 监听任务事件

**快捷键：**
- `K` - 模拟击杀事件
- `I` - 模拟收集事件
- `T` - 提交任务
- `X` - 放弃任务

## 使用方法

1. 在 Package Manager 中导入 "Basic Examples" 示例
2. 将示例脚本挂载到 GameObject 上
3. 配置必要的 ScriptableObject 资源
4. 运行场景测试

## 依赖组件

- `StatSystemExample`: 需要 `StatController` 组件
- `BuffSystemExample`: 需要 `BuffReceiver` 和 `StatController` 组件
- `InventoryExample`: 需要 `InventoryManager` 单例
- `QuestSystemExample`: 需要 `QuestManager` 单例
