# ZeroEngine.ModSystem API 文档

> **用途**: 本文档面向AI助手，提供ModSystem模块的快速参考。
> **版本**: v1.0.0+
> **最后更新**: 2025-12-31

---

## 目录结构

```
ModSystem/
├── ModManifest.cs          # Mod清单数据结构
├── ModLoader.cs            # 核心加载器（单例）
├── ModHotReloader.cs       # 热重载组件（MonoBehaviour）
├── ModContentParser.cs     # JSON→对象解析器
├── ModSystemIntegration.cs # 场景一键集成组件
├── ZeroEngineTypeRegistry.cs # 内置类型注册表
├── ITypeRegistry.cs        # 类型注册接口
├── IAssetRegistry.cs       # 资源注册接口
├── Schemas/                # JSON Schema定义
├── Scripting/              # Lua脚本支持
│   ├── LuaScriptRunner.cs
│   └── ModScriptManager.cs
└── Steam/                  # Steam Workshop集成
    ├── SteamWorkshopManager.cs
    └── WorkshopModBridge.cs
```

---

## ModManifest.cs

**用途**: Mod清单数据结构，对应 `manifest.json`

```csharp
public class ModManifest
{
    public string Id;           // 唯一ID，如 "author.modname"
    public string Name;         // 显示名称
    public string Version;      // 语义化版本 "1.0.0"
    public string Author;
    public string Description;
    public string[] Dependencies;  // 依赖的mod ID数组
    public string[] Conflicts;     // 冲突的mod ID数组
    public string GameVersion;     // 支持的游戏版本
    public string[] ContentPaths;  // 内容目录（默认 ["content"]）
    public bool Enabled;           // 是否启用
    
    [NonSerialized] public string RootPath;   // 运行时：mod根目录
    [NonSerialized] public int LoadOrder;     // 运行时：加载顺序
}
```

---

## ModLoader.cs

**用途**: Mod加载核心，单例模式

```csharp
public class ModLoader
{
    public static ModLoader Instance { get; }
    
    // 属性
    public IReadOnlyDictionary<string, LoadedMod> LoadedMods { get; }
    public ITypeRegistry TypeRegistry { get; }
    public IAssetRegistry AssetRegistry { get; }
    
    // 事件
    public event Action<string> OnModLoaded;
    public event Action<string> OnModUnloaded;
    public event Action<string> OnModReloaded;
    public event Action<string, Exception> OnModLoadError;
    
    // 方法
    void Initialize(ITypeRegistry typeRegistry, IAssetRegistry assetRegistry, string modsFolder = null);
    void LoadAllMods();           // 加载Mods目录下所有mod
    void LoadMod(string modPath); // 加载指定路径的mod
    void UnloadMod(string modId); // 卸载mod
    void ReloadMod(string modId); // 重新加载mod（热重载用）
    List<string> DetectChangedMods(); // 检测文件变化的mod
}

public class LoadedMod
{
    public ModManifest Manifest;
    public DateTime LoadTime;
    public List<UnityEngine.Object> LoadedAssets;
    public Dictionary<string, DateTime> FileLastModified; // 热重载检测用
}
```

**常用场景**:
```csharp
// 初始化并加载
ModLoader.Instance.Initialize(new ZeroEngineTypeRegistry(), new DefaultAssetRegistry());
ModLoader.Instance.LoadAllMods();

// 获取已加载的mod
var mods = ModLoader.Instance.LoadedMods;

// 监听mod加载
ModLoader.Instance.OnModLoaded += modId => Debug.Log($"Loaded: {modId}");
```

---

## ModHotReloader.cs

**用途**: 热重载组件，挂载到GameObject自动检测mod文件变化

```csharp
public class ModHotReloader : MonoBehaviour
{
    [SerializeField] bool _enableHotReload = true;
    [SerializeField] bool _editorOnly = true;  // 仅编辑器模式生效
    
    public event Action OnModsReloaded;
    public event Action<string> OnModReloaded;
    
    void CheckAndReloadMods();      // 手动触发检查
    void ForceReloadAllMods();      // 强制重载所有mod
    void ForceReloadMod(string modId);
}
```

**工作原理**: 监听 `OnApplicationFocus`，窗口恢复焦点时检测文件修改时间变化。

---

## ITypeRegistry.cs

**用途**: 类型注册接口，用于将字符串类型名映射到C#类型

```csharp
public interface ITypeRegistry
{
    object CreateInstance(string typeName);  // 创建实例
    void RegisterType(string typeName, Type type);
    void RegisterTypes(IEnumerable<KeyValuePair<string, Type>> types);
    bool HasType(string typeName);
    Type GetType(string typeName);
    IEnumerable<string> GetRegisteredTypeNames();
}

// 默认实现
public class DefaultTypeRegistry : ITypeRegistry { ... }
```

**常用场景**:
```csharp
var registry = new DefaultTypeRegistry();
registry.RegisterType("DamageEffect", typeof(DamageEffectData));
var instance = registry.CreateInstance("DamageEffect");
```

---

## ZeroEngineTypeRegistry.cs

**用途**: 预注册ZeroEngine内置类型的注册表

```csharp
public class ZeroEngineTypeRegistry : ITypeRegistry
{
    // 构造时自动注册:
    // - AbilitySystem: ManualTriggerData, IntervalTriggerData, OnHitTriggerData,
    //                  DamageEffectData, HealEffectData, ApplyBuffEffectData, ...
    // - BuffSystem: BuffData, BuffStatModifierConfig
    // - Inventory: InventoryItemSO
    // - AbilityDataSO
    
    void RegisterScriptableObjectType(string name, Type type); // 注册SO类型
    void RegisterTypeWithFactory(string name, Type type, Func<object> factory); // 自定义工厂
}
```

---

## IAssetRegistry.cs

**用途**: 资源注册接口，管理mod加载的资源

```csharp
public interface IAssetRegistry
{
    void RegisterAsset(string key, UnityEngine.Object asset);
    T GetAsset<T>(string key) where T : UnityEngine.Object;
    bool HasAsset(string key);
    void UnregisterAsset(string key);
    void UnregisterAssetsByPrefix(string prefix); // 卸载整个mod的资源
    IEnumerable<string> GetAllKeys();
}

// 默认实现
public class DefaultAssetRegistry : IAssetRegistry { ... }
```

**资源键命名约定**: `{modId}:{assetName}`，如 `"mymod:sword_icon"`

---

## ModContentParser.cs

**用途**: 解析mod的JSON内容文件，创建Unity对象

```csharp
public class ModContentParser
{
    ModContentParser(ITypeRegistry typeRegistry, IAssetRegistry assetRegistry);
    
    void ParseDirectory(string directoryPath, LoadedMod mod); // 解析整个目录
    void ParseJsonFile(string filePath, LoadedMod mod);       // 解析单个JSON
    Sprite LoadSprite(string path);                           // 加载图片
    AudioClip LoadAudioClip(string path);                     // 加载音频（需异步）
}
```

**JSON格式约定**: 使用 `$type` 字段指定类型名
```json
{
    "$type": "DamageEffectData",
    "DamageAmount": 25,
    "DamageType": "Physical"
}
```

---

## ModSystemIntegration.cs

**用途**: 场景一键集成组件，挂载即用

```csharp
public class ModSystemIntegration : MonoBehaviour
{
    [SerializeField] string _customModsFolder;      // 自定义路径
    [SerializeField] bool _autoLoadOnAwake = true;  // Awake时自动加载
    [SerializeField] bool _enableHotReload = true;
    [SerializeField] bool _useBuiltinTypeRegistry = true; // 使用ZeroEngineTypeRegistry
    
    event Action OnModsLoaded;
    event Action<string> OnModLoaded;
    event Action<string> OnModReloaded;
    
    void Initialize();
    void LoadMods();
    T GetModAsset<T>(string modId, string assetName);
    void RegisterCustomTypes(Action<ITypeRegistry> action); // 扩展类型
}
```

---

## Scripting/LuaScriptRunner.cs

**用途**: Lua脚本执行器（需要MoonSharp包）

```csharp
// 编译宏: MOONSHARP
public class LuaScriptRunner
{
    DynValue ExecuteFile(string filePath);
    DynValue Execute(string code, string chunkName = "chunk");
    DynValue Call(string functionName, params object[] args);
    void SetGlobal(string name, object value);
    T GetGlobal<T>(string name);
    void RegisterCallback(string name, Delegate callback);
}
```

**预注册的Lua全局函数**:
- `Log(msg)`, `LogWarning(msg)`, `LogError(msg)`
- `Random(min, max)`, `RandomInt(min, max)`
- `Time()`, `DeltaTime()`

---

## Scripting/ModScriptManager.cs

**用途**: 管理每个mod的Lua脚本环境

```csharp
public class ModScriptManager
{
    void InitializeModScripts(LoadedMod mod);  // 初始化mod脚本
    void UnloadModScripts(string modId);
    void BroadcastEvent(string eventName, params object[] args); // 触发所有mod的事件
    object CallModFunction(string modId, string functionName, params object[] args);
    LuaScriptRunner GetModRunner(string modId);
}
```

**Mod脚本约定**:
- 放在 `scripts/` 目录
- `main.lua` 自动首先加载
- 可定义 `OnModLoad()`, `OnModUnload()` 回调

---

## Steam/SteamWorkshopManager.cs

**用途**: Steam Workshop API封装（需要Steamworks.NET）

```csharp
// 编译宏: STEAMWORKS_NET
public class SteamWorkshopManager
{
    bool IsInitialized { get; }
    IReadOnlyList<WorkshopItem> SubscribedItems { get; }
    
    event Action<WorkshopItem> OnItemDownloaded;
    event Action<List<WorkshopItem>> OnSubscribedItemsLoaded;
    event Action<ulong, bool, string> OnItemPublished;
    
    bool Initialize();
    void RefreshSubscribedItems();
    string GetItemLocalPath(ulong publishedFileId);
    void DownloadItem(ulong publishedFileId, bool highPriority = false);
    void PublishItem(string title, string description, string contentPath, string previewImagePath, string[] tags);
    void UpdateItem(ulong publishedFileId, ...);
    void RunCallbacks(); // 需在Update中调用
}

public class WorkshopItem
{
    public ulong PublishedFileId;
    public string Title, Description, LocalPath;
    public string[] Tags;
    public bool IsSubscribed, NeedsUpdate;
}
```

---

## Steam/WorkshopModBridge.cs

**用途**: Workshop与ModLoader的集成

```csharp
public class WorkshopModBridge
{
    void Initialize();
    void LoadSubscribedMods();     // 加载所有订阅的Workshop mod
    void PublishMod(string modPath, string previewImagePath = null);
    SteamWorkshopManager GetWorkshopManager();
    void Update(); // 需在Update中调用
}
```

---

## 编辑器工具

### ModCreatorWindow

**菜单**: ZeroEngine > Mod System > Create New Mod...

创建新mod模板，包含manifest.json和示例内容。

### ModExporter

**菜单**: ZeroEngine > Mod System > Export to Mod...
**右键**: Assets > Export to Mod JSON

将AbilityDataSO/BuffData/InventoryItemSO导出为mod JSON格式。

### ModValidatorWindow

**菜单**: ZeroEngine > Mod System > Validate Mods...

批量验证Mods目录下所有mod的格式和内容。
