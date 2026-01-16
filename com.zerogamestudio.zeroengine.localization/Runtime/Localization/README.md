# ZeroEngine.Localization API 文档

> **用途**: 本文档面向AI助手，提供Localization（本地化）模块的快速参考。
> **版本**: v1.7.0+
> **最后更新**: 2026-01-03

---

## 依赖

| 包名 | 必需 |
|------|------|
| `com.unity.localization` | ✅ 需要安装 |

**安装方式**: Package Manager → + → Add package by name → `com.unity.localization`

> ⚠️ 未安装时，代码仍可编译但功能降级（返回占位符文本）。

---

## 目录结构

```
Runtime/Localization/
├── LocalizationManager.cs       # 本地化管理器（单例）
├── LocalizedText.cs             # UI 自动本地化组件
├── LocalizedStringExtensions.cs # LocalizedString 扩展方法 (v1.7.0+)
└── README.md

Editor/Localization/
└── TranslationCheckerWindow.cs  # 翻译完整性检查工具 (v1.7.0+)
```

---

## LocalizationManager.cs

**用途**: Unity Localization 的简化封装

```csharp
public class LocalizationManager : Singleton<LocalizationManager>
{
    // 属性
    Locale CurrentLocale { get; }
    IList<Locale> AvailableLocales { get; }
    
    // 事件
    event Action<Locale> OnLocaleChanged;
    
    // 切换语言
    void SetLocale(string languageCode);  // "en", "zh-Hans", "ja"
    void SetLocale(Locale locale);
    
    // 获取本地化字符串
    string GetString(string tableName, string entryName);
    string GetString(string tableName, string entryName, params object[] args);
    string GetString(LocalizedString localizedString);
}
```

---

## LocalizedText.cs

**用途**: UI 组件，自动更新 Text/TMP 内容

```csharp
public class LocalizedText : MonoBehaviour
{
    [SerializeField] string _tableName = "UI";
    [SerializeField] string _entryName;
    
    void SetKey(string tableName, string entryName);
    void UpdateText();
}
```

**使用**:
1. 添加到带有 Text 或 TMP_Text 的 GameObject
2. 设置 Table Name 和 Entry Name
3. 语言切换时自动刷新

---

## 使用示例

```csharp
// 1. 获取本地化字符串
string title = LocalizationManager.Instance.GetString("UI", "MainMenu_Title");

// 2. 带参数的字符串
string msg = LocalizationManager.Instance.GetString("UI", "Welcome", playerName);

// 3. 切换语言
LocalizationManager.Instance.SetLocale("zh-Hans");  // 简体中文
LocalizationManager.Instance.SetLocale("en");       // 英语

// 4. 监听语言变化
LocalizationManager.Instance.OnLocaleChanged += locale => {
    Debug.Log($"Language changed to: {locale.Identifier.Code}");
};

// 5. 动态设置 LocalizedText
GetComponent<LocalizedText>().SetKey("UI", "NewKey");
```

---

## Unity Localization 快速设置

1. **创建 Localization Settings**:
   Window → Asset Management → Localization Tables → Create Localization Settings

2. **添加语言**:
   在 Localization Settings 中添加 Locales (如 English, Chinese)

3. **创建 String Table**:
   Window → Asset Management → Localization Tables → New Table Collection

4. **添加条目**:
   在 String Table 中添加 Key-Value 对

---

## LocalizedStringExtensions.cs (v1.7.0+)

**用途**: LocalizedString 扩展方法，提供安全获取和 Debug 模式

### API

```csharp
public static class LocalizedStringExtensions
{
    // 全局设置
    static bool DebugMode { get; set; }           // 启用时显示 [key]
    static string MissingKeyFormat { get; set; }  // 默认 "[{0}]"

    // 扩展方法
    string GetSafe(this LocalizedString ls);                      // 安全获取
    string GetSafe(this LocalizedString ls, params object[] args); // 带参数
    bool IsValid(this LocalizedString ls);                        // 检查有效性
    string GetKey(this LocalizedString ls);                       // 获取 key 名
    string GetTableName(this LocalizedString ls);                 // 获取表名
}
```

### 推荐用法（配置优先）

```csharp
// 1. 在 ScriptableObject 中配置 LocalizedString
[CreateAssetMenu]
public class ItemConfigSO : ScriptableObject
{
    [SerializeField] LocalizedString _itemName;
    [SerializeField] LocalizedString _description;

    // 使用扩展方法安全获取
    public string ItemName => _itemName.GetSafe();
    public string Description => _description.GetSafe();
}

// 2. 带参数的翻译
[SerializeField] LocalizedString _damageText;  // "{0} damage"
public string GetDamageText(int value) => _damageText.GetSafe(value);

// 3. 开发时启用 Debug 模式
LocalizedStringExtensions.DebugMode = true;  // 显示 [key] 便于定位

// 4. 检查有效性
if (_itemName.IsValid())
{
    ShowItemName(_itemName.GetSafe());
}
```

### 空值处理

| 情况 | GetSafe() 返回 |
|------|----------------|
| LocalizedString 为 null | `[NULL]` |
| LocalizedString.IsEmpty | `[NULL]` |
| 翻译为空字符串 | `[key]` |
| Debug 模式 | `[key]` |
| 正常翻译 | 翻译文本 |

---

## Translation Checker (v1.7.0+)

**用途**: 编辑器工具，检查翻译完整性

**菜单**: `ZeroEngine > Localization > Translation Checker`

### 功能

1. **扫描所有 String Table** - 分析项目中的本地化表
2. **覆盖率显示** - 进度条显示各语言翻译完成度
3. **缺失 key 列表** - 展开查看未翻译的 key
4. **导出报告** - 生成 Markdown 格式报告

### 界面

```
┌─────────────────────────────────────────────────┐
│  Translation Checker                            │
│  [Scan All Tables] [Export Report]              │
├─────────────────────────────────────────────────┤
│  ▼ UI (150 keys)                                │
│    English (en)    ████████████ 150/150 (100%) 🟢│
│    Chinese (zh)    ██████████▒  148/150 (98%)  🟡│
│    Japanese (ja)   ████████░░░  120/150 (80%)  🔴│
│      Missing (30):                              │
│        - Settings_AudioVolume                   │
│        - Credits_SpecialThanks                  │
│        ... and 28 more                          │
└─────────────────────────────────────────────────┘
```

### 导出报告格式

```markdown
# Translation Completeness Report

## UI
Total Keys: 150

| Locale | Coverage | Missing |
|--------|----------|---------|
| English (en) | 100% | 0 |
| Chinese (zh) | 98% | 2 |

### Missing Keys - Chinese
- `Settings_AudioVolume`
- `Credits_SpecialThanks`
```
