using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Projectile
{
    /// <summary>
    /// 弹道管理器 - 管理弹道的生成、回收和统计
    /// </summary>
    public class ProjectileManager : Core.MonoSingleton<ProjectileManager>
    {
        [Header("Pool Settings")]
        [SerializeField] private int _initialPoolSize = 20;
        [SerializeField] private int _maxPoolSize = 100;
        [SerializeField] private bool _usePooling = true;

        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;

        // 对象池
        private readonly Dictionary<string, Queue<ProjectileBase>> _pools = new();
        private readonly Dictionary<string, int> _poolSizes = new();

        // 活跃弹道
        private readonly List<ProjectileBase> _activeProjectiles = new();
        private readonly List<ProjectileBase> _projectilesToRemove = new();

        // 统计
        private int _totalSpawned;
        private int _totalDespawned;
        private int _poolHits;
        private int _poolMisses;

        #region Properties

        /// <summary>当前活跃弹道数量</summary>
        public int ActiveCount => _activeProjectiles.Count;

        /// <summary>总生成数量</summary>
        public int TotalSpawned => _totalSpawned;

        /// <summary>总回收数量</summary>
        public int TotalDespawned => _totalDespawned;

        /// <summary>对象池命中率</summary>
        public float PoolHitRate => _poolHits + _poolMisses > 0 ?
            (float)_poolHits / (_poolHits + _poolMisses) : 0f;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // 订阅弹道事件
            ProjectileEvents.OnProjectileDestroy += OnProjectileDestroy;
        }

        protected override void OnDestroy()
        {
            ProjectileEvents.OnProjectileDestroy -= OnProjectileDestroy;

            // 清理所有池
            ClearAllPools();
            base.OnDestroy();
        }

        private void LateUpdate()
        {
            // 清理已销毁的弹道
            CleanupDestroyedProjectiles();
        }

        #endregion

        #region Spawn Methods

        /// <summary>
        /// 生成弹道
        /// </summary>
        public ProjectileBase Spawn(
            ProjectileDataSO data,
            Vector3 position,
            Vector3 direction,
            Transform owner,
            Transform target = null,
            int hitMask = -1)
        {
            if (data == null)
            {
                Debug.LogError("[ProjectileManager] ProjectileDataSO is null!");
                return null;
            }

            // 获取或创建弹道实例
            ProjectileBase projectile = GetFromPool(data);

            if (projectile == null)
            {
                projectile = CreateProjectile(data);
            }

            if (projectile == null)
            {
                Debug.LogError($"[ProjectileManager] Failed to create projectile: {data.name}");
                return null;
            }

            // 初始化
            projectile.gameObject.SetActive(true);
            projectile.Initialize(data, position, direction, owner, target, hitMask);

            // 添加到活跃列表
            _activeProjectiles.Add(projectile);
            _totalSpawned++;

            return projectile;
        }

        /// <summary>
        /// 生成弹道 (简化版)
        /// </summary>
        public ProjectileBase Spawn(
            ProjectileDataSO data,
            Transform spawnPoint,
            Transform owner,
            Transform target = null)
        {
            if (spawnPoint == null)
            {
                Debug.LogError("[ProjectileManager] SpawnPoint is null!");
                return null;
            }

            return Spawn(data, spawnPoint.position, spawnPoint.forward, owner, target);
        }

        /// <summary>
        /// 批量生成弹道 (散射)
        /// </summary>
        public List<ProjectileBase> SpawnSpread(
            ProjectileDataSO data,
            Vector3 position,
            Vector3 direction,
            Transform owner,
            int count,
            float spreadAngle,
            Transform target = null,
            int hitMask = -1)
        {
            var projectiles = new List<ProjectileBase>(count);

            if (count <= 0) return projectiles;

            if (count == 1)
            {
                var p = Spawn(data, position, direction, owner, target, hitMask);
                if (p != null) projectiles.Add(p);
                return projectiles;
            }

            float halfSpread = spreadAngle * 0.5f;
            float angleStep = spreadAngle / (count - 1);

            for (int i = 0; i < count; i++)
            {
                float angle = -halfSpread + angleStep * i;
                Vector3 spreadDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;

                var p = Spawn(data, position, spreadDirection, owner, target, hitMask);
                if (p != null) projectiles.Add(p);
            }

            return projectiles;
        }

        /// <summary>
        /// 批量生成弹道 (圆形)
        /// </summary>
        public List<ProjectileBase> SpawnCircle(
            ProjectileDataSO data,
            Vector3 position,
            Transform owner,
            int count,
            float startAngle = 0f,
            int hitMask = -1)
        {
            var projectiles = new List<ProjectileBase>(count);

            if (count <= 0) return projectiles;

            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + angleStep * i;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;

                var p = Spawn(data, position, direction, owner, null, hitMask);
                if (p != null) projectiles.Add(p);
            }

            return projectiles;
        }

        #endregion

        #region Despawn Methods

        /// <summary>
        /// 回收弹道
        /// </summary>
        public void Despawn(ProjectileBase projectile)
        {
            if (projectile == null) return;

            // 从活跃列表移除
            _activeProjectiles.Remove(projectile);
            _totalDespawned++;

            // 回收到池
            if (_usePooling)
            {
                ReturnToPool(projectile);
            }
            else
            {
                Destroy(projectile.gameObject);
            }
        }

        /// <summary>
        /// 回收所有弹道
        /// </summary>
        public void DespawnAll()
        {
            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
            {
                var projectile = _activeProjectiles[i];
                if (projectile != null)
                {
                    projectile.Destroy(ProjectileDestroyReason.Manual);
                }
            }

            _activeProjectiles.Clear();
        }

        /// <summary>
        /// 回收指定所有者的所有弹道
        /// </summary>
        public void DespawnByOwner(Transform owner)
        {
            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
            {
                var projectile = _activeProjectiles[i];
                if (projectile != null && projectile.Owner == owner)
                {
                    projectile.Destroy(ProjectileDestroyReason.Manual);
                }
            }
        }

        #endregion

        #region Pool Management

        /// <summary>
        /// 预热对象池
        /// </summary>
        public void WarmupPool(ProjectileDataSO data, int count = -1)
        {
            if (data == null || data.Prefab == null) return;

            int warmupCount = count > 0 ? count : _initialPoolSize;
            string key = GetPoolKey(data);

            if (!_pools.ContainsKey(key))
            {
                _pools[key] = new Queue<ProjectileBase>();
                _poolSizes[key] = 0;
            }

            for (int i = 0; i < warmupCount; i++)
            {
                if (_poolSizes[key] >= _maxPoolSize) break;

                var projectile = CreateProjectile(data);
                if (projectile != null)
                {
                    projectile.gameObject.SetActive(false);
                    _pools[key].Enqueue(projectile);
                    _poolSizes[key]++;
                }
            }
        }

        /// <summary>
        /// 从池中获取
        /// </summary>
        private ProjectileBase GetFromPool(ProjectileDataSO data)
        {
            if (!_usePooling || data.Prefab == null) return null;

            string key = GetPoolKey(data);

            if (_pools.TryGetValue(key, out var pool) && pool.Count > 0)
            {
                var projectile = pool.Dequeue();
                if (projectile != null)
                {
                    _poolHits++;
                    return projectile;
                }
            }

            _poolMisses++;
            return null;
        }

        /// <summary>
        /// 返回到池
        /// </summary>
        private void ReturnToPool(ProjectileBase projectile)
        {
            if (projectile == null || projectile.Data == null) return;

            string key = GetPoolKey(projectile.Data);

            if (!_pools.ContainsKey(key))
            {
                _pools[key] = new Queue<ProjectileBase>();
                _poolSizes[key] = 0;
            }

            // 检查池大小限制
            if (_poolSizes[key] >= _maxPoolSize)
            {
                Destroy(projectile.gameObject);
                return;
            }

            // 重置并入池
            projectile.Reset();
            projectile.gameObject.SetActive(false);
            projectile.transform.SetParent(transform);

            _pools[key].Enqueue(projectile);
            _poolSizes[key]++;
        }

        /// <summary>
        /// 清理所有池
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                while (pool.Count > 0)
                {
                    var projectile = pool.Dequeue();
                    if (projectile != null)
                    {
                        Destroy(projectile.gameObject);
                    }
                }
            }

            _pools.Clear();
            _poolSizes.Clear();
        }

        /// <summary>
        /// 获取池键名
        /// </summary>
        private string GetPoolKey(ProjectileDataSO data)
        {
            return data.name;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 创建弹道实例
        /// </summary>
        private ProjectileBase CreateProjectile(ProjectileDataSO data)
        {
            if (data.Prefab != null)
            {
                var instance = Instantiate(data.Prefab, transform);
                var projectile = instance.GetComponent<ProjectileBase>();

                if (projectile == null)
                {
                    projectile = instance.AddComponent<ProjectileBase>();
                }

                return projectile;
            }
            else
            {
                // 创建默认弹道对象
                var go = new GameObject($"Projectile_{data.name}");
                go.transform.SetParent(transform);

                var projectile = go.AddComponent<ProjectileBase>();

                // 添加基本碰撞体
                var sphereCollider = go.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.1f;
                sphereCollider.isTrigger = true;

                return projectile;
            }
        }

        /// <summary>
        /// 清理已销毁的弹道
        /// </summary>
        private void CleanupDestroyedProjectiles()
        {
            _projectilesToRemove.Clear();

            for (int i = 0; i < _activeProjectiles.Count; i++)
            {
                if (_activeProjectiles[i] == null || !_activeProjectiles[i].IsActive)
                {
                    _projectilesToRemove.Add(_activeProjectiles[i]);
                }
            }

            for (int i = 0; i < _projectilesToRemove.Count; i++)
            {
                _activeProjectiles.Remove(_projectilesToRemove[i]);
            }
        }

        /// <summary>
        /// 弹道销毁事件处理
        /// </summary>
        private void OnProjectileDestroy(ProjectileDestroyEventArgs args)
        {
            // 事件处理（如有需要）
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// 获取所有活跃弹道
        /// </summary>
        public IReadOnlyList<ProjectileBase> GetActiveProjectiles()
        {
            return _activeProjectiles;
        }

        /// <summary>
        /// 获取指定所有者的弹道
        /// </summary>
        public List<ProjectileBase> GetProjectilesByOwner(Transform owner)
        {
            var result = new List<ProjectileBase>();

            for (int i = 0; i < _activeProjectiles.Count; i++)
            {
                if (_activeProjectiles[i].Owner == owner)
                {
                    result.Add(_activeProjectiles[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取范围内的弹道
        /// </summary>
        public List<ProjectileBase> GetProjectilesInRange(Vector3 center, float radius)
        {
            var result = new List<ProjectileBase>();
            float radiusSqr = radius * radius;

            for (int i = 0; i < _activeProjectiles.Count; i++)
            {
                var projectile = _activeProjectiles[i];
                if (projectile == null) continue;

                float distSqr = (projectile.transform.position - center).sqrMagnitude;
                if (distSqr <= radiusSqr)
                {
                    result.Add(projectile);
                }
            }

            return result;
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!_showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"[ProjectileManager Debug]");
            GUILayout.Label($"Active: {ActiveCount}");
            GUILayout.Label($"Spawned: {_totalSpawned} | Despawned: {_totalDespawned}");
            GUILayout.Label($"Pool Hit Rate: {PoolHitRate:P1}");
            GUILayout.Label($"Pools: {_pools.Count}");

            foreach (var kvp in _poolSizes)
            {
                GUILayout.Label($"  {kvp.Key}: {_pools[kvp.Key].Count}/{kvp.Value}");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion
    }
}
