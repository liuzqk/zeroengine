# ZeroEngine.UI.MVVM API 文档

> **用途**: 本文档面向AI助手，提供MVVM（UI框架）模块的快速参考。
> **版本**: v1.0.0+ (格式化/验证/双向绑定 v1.3.0+)
> **最后更新**: 2026-01-01

---

## 目录结构

```
UI/MVVM/
├── ViewModelBase.cs      # ViewModel 基类 + BindingContext
├── BindableProperty.cs   # 可绑定属性 + BindableList + BindableDictionary
├── Validators.cs         # 常用验证器集合 (v1.3.0+)
├── RelayCommand.cs       # 命令封装
└── MVVMView.cs           # View 基类
```

---

## BindableProperty.cs

**用途**: 可观察属性，值变更时触发事件，支持格式化和验证 (v1.3.0+)

```csharp
public struct ValidationResult
{
    public bool IsValid;
    public string ErrorMessage;

    public static ValidationResult Valid { get; }
    public static ValidationResult Invalid(string message);
}

public class BindableProperty<T>
{
    public T Value { get; set; }
    public string FormattedValue { get; }           // v1.3.0+ 格式化后的字符串
    public ValidationResult ValidationState { get; } // v1.3.0+ 验证状态
    public bool IsValid { get; }                    // v1.3.0+ 是否验证通过

    public event Action<T> OnValueChanged;
    public event Action<ValidationResult> OnValidationChanged; // v1.3.0+

    // 链式配置 (v1.3.0+)
    BindableProperty<T> WithFormat(Func<T, string> formatter);
    BindableProperty<T> WithFormat(string format);  // 如 "{0:F2}"
    BindableProperty<T> WithValidation(Func<T, ValidationResult> validator);
    BindableProperty<T> WithValidation(Func<T, bool> condition, string errorMessage);

    // 值操作
    void SetValueWithoutValidation(T value);  // v1.3.0+ 跳过验证
    void NotifyValueChanged();                // 强制触发通知
    void Register(Action<T> callback);
    void RegisterAndInvoke(Action<T> callback);
    void Unregister(Action<T> callback);
    void ClearListeners();
}
```

---

## ViewModelBase.cs

**用途**: ViewModel 基类

```csharp
public abstract class ViewModelBase
{
    public virtual void Initialize();
    public virtual void Refresh();
    
    void AddDisposable(Action dispose);  // 注册清理回调
    void Dispose();                       // 释放资源
    protected virtual void OnDispose();
}
```

---

## BindingContext

**用途**: 管理 View 和 ViewModel 的绑定

```csharp
public class BindingContext
{
    // 属性绑定
    void Bind<T>(BindableProperty<T> property, Action<T> onValueChanged);
    void BindList<T>(BindableList<T> list, Action onListChanged);

    // UI绑定
    void BindClick(Button button, Action onClick);
    void BindCommand(Button button, ICommand command, object parameter = null);
    void BindText(Text text, BindableProperty<string> property);
    void BindTextFormatted<T>(Text text, BindableProperty<T> property);  // v1.3.0+ 格式化绑定
    void BindInput(InputField input, BindableProperty<string> property);      // Legacy
    void BindInput(TMP_InputField input, BindableProperty<string> property);  // TMP
    void BindSlider(Slider slider, BindableProperty<float> property);
    void BindToggle(Toggle toggle, BindableProperty<bool> property);
    void BindDropdown(Dropdown dropdown, BindableProperty<int> property);     // Legacy
    void BindDropdown(TMP_Dropdown dropdown, BindableProperty<int> property); // TMP

    // 验证绑定 (v1.3.0+)
    void BindValidation<T>(Text errorText, BindableProperty<T> property);
    void BindValidationColor<T>(Graphic graphic, BindableProperty<T> property, Color validColor, Color invalidColor);

    // 双向绑定 (v1.3.0+)
    void BindTwoWay<T>(BindableProperty<T> source, BindableProperty<T> target);

    void Dispose();  // 解除所有绑定
}
```

---

## RelayCommand.cs

**用途**: 命令封装，支持 CanExecute 检查

```csharp
public class RelayCommand
{
    RelayCommand(Action execute, Func<bool> canExecute = null);
    
    bool CanExecute();
    void Execute();
    void RaiseCanExecuteChanged();  // 通知UI刷新按钮状态
}
```

---

## 使用示例

```csharp
// 1. 定义 ViewModel
public class PlayerViewModel : ViewModelBase
{
    public BindableProperty<string> Name = new BindableProperty<string>();
    public BindableProperty<int> Level = new BindableProperty<int>();
    public RelayCommand LevelUpCommand;
    
    public override void Initialize()
    {
        LevelUpCommand = new RelayCommand(LevelUp, () => Level.Value < 100);
    }
    
    private void LevelUp()
    {
        Level.Value++;
        LevelUpCommand.RaiseCanExecuteChanged();
    }
}

// 2. 在 View 中绑定
public class PlayerView : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] Button levelUpButton;
    
    private BindingContext _context = new BindingContext();
    private PlayerViewModel _vm = new PlayerViewModel();
    
    void Start()
    {
        _vm.Initialize();
        
        _context.BindText(nameText, _vm.Name);
        _context.Bind(_vm.Level, v => levelText.text = $"Lv.{v}");
        _context.BindCommand(levelUpButton, _vm.LevelUpCommand);
    }
    
    void OnDestroy()
    {
        _context.Dispose();
        _vm.Dispose();
    }
}
```

---

## 编译宏

| 宏定义 | 效果 |
|--------|------|
| `TEXTMESHPRO_ENABLED` | 启用 TMP_InputField / TMP_Dropdown 绑定 |
