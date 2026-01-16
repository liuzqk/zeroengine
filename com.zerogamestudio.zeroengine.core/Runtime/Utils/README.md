# ZeroEngine.Utils API 文档

> **用途**: 本文档面向AI助手，提供Utils（工具类）模块的快速参考。
> **版本**: v1.2.0+
> **最后更新**: 2025-12-31

---

## 目录结构

```
Utils/
├── DateUtils.cs       # 日期时间工具
├── UIUtils.cs         # UI 工具类
├── ZeroEase.cs        # 缓动曲线枚举
├── ZeroLog.cs         # 统一日志系统 (v1.2.0+)
├── DebugUtils.cs      # 调试工具类
└── DOTweenAdapter.cs  # DOTween 反射适配器（内部）
```

---

## DateUtils.cs

**用途**: 日期时间转换和格式化

```csharp
public static class DateUtils
{
    // 常量
    public const string Format_yyyy_MM_dd_HH_mm_ss = "yyyy-MM-dd HH:mm:ss";

    // 获取时间戳
    static long GetCurrentTimeMillis();   // 13位毫秒时间戳
    static long GetCurrentTimeSeconds();  // 10位秒时间戳
    static int GetUTC0TimeStamp();        // UTC0 秒时间戳

    // 时间戳转换
    static DateTime Unix2DateTime(long timestampSeconds);  // Unix -> DateTime
    static int DateTime2Unix(DateTime time);               // DateTime -> Unix

    // 格式化
    static string FormatSecondsToHHMMSS(long seconds);  // 3661 -> "01:01:01"
    static string FormatSecondsToMMSS(long seconds);    // 125 -> "02:05"

    // 判断
    static bool IsToday(DateTime dt);
    static long GetDayRemainingSeconds();  // 今日剩余秒数
}
```

**示例**:
```csharp
// 获取当前时间戳
long timestamp = DateUtils.GetCurrentTimeSeconds();

// 转换时间戳
DateTime dt = DateUtils.Unix2DateTime(1704067200);

// 格式化倒计时
string countdown = DateUtils.FormatSecondsToMMSS(125);  // "02:05"
```

---

## UIUtils.cs

**用途**: UI 相关工具方法

```csharp
public static class UIUtils
{
    // CanvasGroup 动画
    static void ShowCanvasGroup(CanvasGroup cg, float duration = 0.3f, ZeroEase ease = ZeroEase.OutQuad, bool interactable = true);
    static void HideCanvasGroup(CanvasGroup cg, float duration = 0.3f, ZeroEase ease = ZeroEase.InQuad);
    static void InitCanvasGroup(CanvasGroup cg);  // 初始化为隐藏状态

    // 清理
    static void ClearChild(Transform parent);  // 销毁所有子物体

    // 输入检测
    static bool IsPointerOverUI();  // 鼠标是否在 UI 上

    // Tooltip 定位
    static void SetTooltipPosition(Canvas canvas, RectTransform tooltip, RectTransform canvasRect, RectTransform targetUI);
    static void SetTooltipPositionToMouse(Canvas canvas, RectTransform tooltip, RectTransform canvasRect);
}
```

**示例**:
```csharp
// 显示/隐藏 UI
UIUtils.ShowCanvasGroup(panel, 0.3f, ZeroEase.OutQuad);
UIUtils.HideCanvasGroup(panel);

// 检测点击是否在 UI 上
if (!UIUtils.IsPointerOverUI())
{
    // 处理场景点击
}

// Tooltip 跟随鼠标
UIUtils.SetTooltipPositionToMouse(canvas, tooltipRect, canvasRect);
```

---

## ZeroEase.cs

**用途**: 解耦的缓动曲线枚举，避免对 DOTween 的直接依赖

```csharp
public enum ZeroEase
{
    Linear,
    InSine, OutSine, InOutSine,
    InQuad, OutQuad, InOutQuad,
    InCubic, OutCubic, InOutCubic,
    InQuart, OutQuart, InOutQuart,
    InQuint, OutQuint, InOutQuint,
    InExpo, OutExpo, InOutExpo,
    InCirc, OutCirc, InOutCirc,
    InElastic, OutElastic, InOutElastic,
    InBack, OutBack, InOutBack,
    InBounce, OutBounce, InOutBounce,
    Flash, InFlash, OutFlash, InOutFlash,
}
```

**常用曲线**:
| 曲线 | 效果 |
|------|------|
| `Linear` | 线性（匀速） |
| `OutQuad` | 快入慢出（常用于显示） |
| `InQuad` | 慢入快出（常用于隐藏） |
| `OutBack` | 带回弹的减速 |
| `OutElastic` | 弹性效果 |

---

## DOTweenAdapter.cs

**用途**: 内部适配器，通过反射调用 DOTween，使 ZeroEngine 可选依赖 DOTween

```csharp
internal static class DOTweenAdapter
{
    static bool IsAvailable { get; }  // DOTween 是否可用

    static void DOKill(Component target);
    static void FadeCanvasGroup(CanvasGroup cg, float endValue, float duration, ZeroEase ease);
}
```

> **注意**: 此类为内部使用，通过 `UIUtils` 间接调用。如果项目未安装 DOTween，相关动画会自动降级为即时切换。

---

## ZeroLog.cs

**用途**: ZeroEngine 统一日志系统 (v1.2.0+)，为所有模块提供一致的日志格式

```csharp
public static class ZeroLog
{
    // 模块常量 (避免字符串拼写错误)
    public static class Modules
    {
        public const string Core = "Core";
        public const string Stat = "Stat";
        public const string Buff = "Buff";
        public const string Inventory = "Inventory";
        public const string Quest = "Quest";
        public const string Ability = "Ability";
        public const string Save = "Save";
        public const string Network = "Network";
        // ... 等其他模块
    }

    // 配置
    public static bool Enabled { get; set; } = true;
    public static LogLevel MinLevel { get; set; } = LogLevel.Info;

    // 日志级别
    public enum LogLevel { Debug, Info, Warning, Error }

    // 日志方法
    static void Info(string module, string message);
    static void Info(string module, string message, Object context);
    static void Warning(string module, string message);
    static void Warning(string module, string message, Object context);
    static void Error(string module, string message);
    static void Error(string module, string message, Object context);

    // 便捷方法
    static bool WarnIf(string module, bool condition, string message);
    static bool ErrorIf(string module, bool condition, string message);
    static void Exception(string module, System.Exception ex, string context = null);
}
```

**输出格式**: `[ZeroEngine.{Module}] {message}`

**使用示例**:
```csharp
// 基础日志
ZeroLog.Info(ZeroLog.Modules.Inventory, "Item added successfully");
ZeroLog.Warning(ZeroLog.Modules.Buff, "Buff stack limit exceeded");
ZeroLog.Error(ZeroLog.Modules.Save, "Failed to load save file");

// 条件日志
ZeroLog.WarnIf(ZeroLog.Modules.Quest, questId == null, "Quest ID is null");

// 异常日志
try { /* ... */ }
catch (Exception ex)
{
    ZeroLog.Exception(ZeroLog.Modules.Network, ex, "Failed to connect");
}

// 运行时配置
ZeroLog.Enabled = false;  // 禁用所有日志
ZeroLog.MinLevel = ZeroLog.LogLevel.Warning;  // 只显示警告及以上
```

**设计特点**:
- 始终编译，不依赖 `DEBUG` 宏（适合生产环境）
- 支持 `Enabled` 和 `MinLevel` 运行时动态控制
- 模块常量避免字符串拼写错误
- 与 `DebugUtils` 互补：ZeroLog 用于生产日志，DebugUtils 用于开发调试

---

## DebugUtils.cs

**用途**: 开发调试工具类，仅在编辑器或 DEBUG 模式编译

> 与 ZeroLog 的区别：DebugUtils 依赖 `#if UNITY_EDITOR || DEBUG` 条件编译，在非调试构建中完全移除。

---

## 编译宏

| 宏定义 | 效果 |
|--------|------|
| `ENABLE_INPUT_SYSTEM` | 使用新 Input System 获取鼠标位置 |
| `ZERO_DOTWEEN_SUPPORT` | 启用 DOTween 直接支持（非反射） |