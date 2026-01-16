# ZeroEngine.Pool API 文档

> **用途**: 本文档面向AI助手，提供Pool模块的快速参考。
> **版本**: v1.0.0+
> **最后更新**: 2025-12-31

---

## 目录结构

```
Pool/
├── PoolManager.cs        # 对象池管理器（单例）
└── PoolExtensions.cs     # 扩展方法
```

---

## PoolManager.cs

**用途**: 通用对象池管理器，支持同步/异步、Addressables

### 编译宏

| 宏 | 功能 |
|---|------|
| `UNITASK_ENABLED` | 异步API（UniTask） |
| `ADDRESSABLES_ENABLED` | Addressables支持 |

### 同步API

```csharp
public class PoolManager : Singleton<PoolManager>
{
    // 生成对象
    GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation);
    T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component;
    
    // 归还对象
    void Despawn(GameObject obj, float delay = 0);
    
    // 清除
    void ClearAllPools();
}
```

### 异步API (需要 UNITASK_ENABLED)

```csharp
// 预加载对象到池中
UniTask PreloadAsync(GameObject prefab, int count, CancellationToken ct = default);
```

### Addressables API (需要 UNITASK_ENABLED + ADDRESSABLES_ENABLED)

```csharp
// 从Addressables异步生成
UniTask<GameObject> SpawnAsync(string addressableKey, Vector3 position, Quaternion rotation);
UniTask<T> SpawnAsync<T>(string addressableKey, Vector3 position, Quaternion rotation);

// 预加载Addressable对象
UniTask PreloadAsync(string addressableKey, int count);
```

---

## 扩展方法

```csharp
public static class PoolExtensions
{
    // 任何GameObject都可以调用
    public static void Despawn(this GameObject obj, float delay = 0);
}
```

---

## 内部机制

- **SmartPool**: 每个Prefab一个池，使用Queue管理
- **自动收缩**: 定期检查并销毁长时间未使用的对象
- **容量限制**: `MaxCapacity` 防止池无限增长

---

## 使用示例

```csharp
// 同步生成
var bullet = PoolManager.Instance.Spawn(bulletPrefab, firePoint.position, firePoint.rotation);

// 归还对象
bullet.Despawn();  // 扩展方法
// 或
PoolManager.Instance.Despawn(bullet);

// 延迟归还
bullet.Despawn(2f);  // 2秒后归还

// 异步生成（Addressables）
#if ADDRESSABLES_ENABLED && UNITASK_ENABLED
var enemy = await PoolManager.Instance.SpawnAsync("Prefabs/Enemy", spawnPoint, Quaternion.identity);
#endif

// 预加载
#if UNITASK_ENABLED
await PoolManager.Instance.PreloadAsync(bulletPrefab, 20);
#endif
```

---

## 注意事项

1. **OnEnable/OnDisable**: 池对象会触发这些回调，适合重置状态
2. **命名约定**: 归还的对象会被移到 `[Pools]` 根节点下
3. **自动清理**: 场景切换时调用 `ClearAllPools()` 防止泄漏
