# ZeroEngine 单元测试

## 目录结构

```
Tests/Editor/
├── ZeroEngine.Tests.Editor.asmdef  # 测试程序集定义
├── TestHelpers.cs                   # 测试辅助工具
├── README.md                        # 本文档
│
├── BehaviorTree/
│   ├── BlackboardTests.cs           # 黑板数据存储测试
│   └── BehaviorTreeTests.cs         # 行为树节点测试
│
├── FSM/
│   └── StateMachineTests.cs         # 状态机测试
│
└── MVVM/
    ├── BindablePropertyTests.cs     # 可绑定属性测试
    └── ValidatorsTests.cs           # 验证器测试
```

## 运行测试

### 在 Unity 编辑器中

1. 打开 Unity 编辑器
2. 菜单 `Window` → `General` → `Test Runner`
3. 选择 `EditMode` 标签
4. 点击 `Run All` 或选择特定测试运行

### 命令行运行

```bash
Unity.exe -runTests -testPlatform EditMode -projectPath "<项目路径>" -testResults Logs/test-results.xml
```

## 测试覆盖范围

### BehaviorTree

- **BlackboardTests**: 数据存储、类型化访问器（零装箱）、事件通知
- **BehaviorTreeTests**: ActionNode、Sequence、Selector、Parallel、装饰器节点

### FSM

- **StateMachineTests**: 状态添加、状态切换、生命周期回调、黑板数据

### MVVM

- **BindablePropertyTests**: 值变更通知、格式化、验证、双向绑定
- **ValidatorsTests**: 字符串验证、数值范围验证、验证器组合

## 编写新测试

```csharp
using NUnit.Framework;
using ZeroEngine.YourNamespace;

namespace ZeroEngine.Tests.YourNamespace
{
    [TestFixture]
    public class YourClassTests
    {
        [SetUp]
        public void SetUp()
        {
            // 每个测试前执行
        }

        [TearDown]
        public void TearDown()
        {
            // 每个测试后执行
        }

        [Test]
        public void MethodName_Scenario_ExpectedBehavior()
        {
            // Arrange
            var sut = new YourClass();

            // Act
            var result = sut.Method();

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}
```

## 测试辅助工具

```csharp
// 事件计数器
var counter = TestHelpers.CreateEventCounter();
property.OnValueChanged += counter.Increment;
counter.AssertCount(1);

// 浮点数近似比较
TestHelpers.AssertApproximatelyEqual(expected, actual, tolerance);

// 异常断言
TestHelpers.AssertThrows<ArgumentException>(() => sut.InvalidOperation());
```
