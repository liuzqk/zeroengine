using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Spawner
{
    /// <summary>
    /// 生成器基类
    /// </summary>
    public abstract class SpawnerBase : MonoBehaviour
    {
        [Header("Spawn Data")]
        [SerializeField] protected SpawnDataSO _spawnData;

        [Header("Activation")]
        [SerializeField] protected bool _activateOnStart = true;
        [SerializeField] protected bool _autoDeactivateWhenComplete = true;

        [Header("Debug")]
        [SerializeField] protected bool _showDebugGizmos = true;

        // 运行时状态
        protected bool _isActive;
        protected float _spawnTimer;
        protected float _runTime;
        protected int _totalSpawnCount;
        protected int _killCount;
        protected int _currentEntryIndex;

        // 活跃实体追踪
        protected readonly List<GameObject> _activeEntities = new();
        protected readonly List<GameObject> _entitiesToRemove = new();

        #region Properties

        /// <summary>生成数据配置</summary>
        public SpawnDataSO SpawnData => _spawnData;

        /// <summary>是否激活</summary>
        public bool IsActive => _isActive;

        /// <summary>当前活跃实体数量</summary>
        public int ActiveCount => _activeEntities.Count;

        /// <summary>总生成数量</summary>
        public int TotalSpawnCount => _totalSpawnCount;

        /// <summary>击杀数量</summary>
        public int KillCount => _killCount;

        /// <summary>运行时长</summary>
        public float RunTime => _runTime;

        #endregion

        #region Unity Lifecycle

        protected virtual void Start()
        {
            if (_activateOnStart && _spawnData != null)
            {
                Activate();
            }
        }

        protected virtual void Update()
        {
            if (!_isActive) return;

            _runTime += Time.deltaTime;
            UpdateSpawnLogic(Time.deltaTime);
            CleanupDestroyedEntities();
        }

        protected virtual void OnDestroy()
        {
            // 清理所有生成的实体
            ClearAllEntities(DespawnReason.SpawnerDeactivated);
        }

        #endregion

        #region Activation

        /// <summary>
        /// 激活生成器
        /// </summary>
        public virtual void Activate()
        {
            if (_isActive) return;
            if (_spawnData == null)
            {
                Debug.LogWarning($"[{GetType().Name}] SpawnData is null, cannot activate.");
                return;
            }

            _isActive = true;
            _spawnTimer = _spawnData.InitialDelay;
            _runTime = 0f;
            _totalSpawnCount = 0;
            _killCount = 0;
            _currentEntryIndex = 0;

            // 预热对象池
            if (_spawnData.UsePooling && SpawnManager.Instance != null)
            {
                foreach (var entry in _spawnData.Entries)
                {
                    if (entry.Prefab != null && entry.IsEnabled)
                    {
                        SpawnManager.Instance.WarmupPool(entry.Prefab, _spawnData.PoolWarmupSize);
                    }
                }
            }

            SpawnEvents.InvokeSpawnerActivate(new SpawnerActivateEventArgs(this, _spawnData));
            OnActivated();
        }

        /// <summary>
        /// 停用生成器
        /// </summary>
        public virtual void Deactivate(string reason = "Manual")
        {
            if (!_isActive) return;

            _isActive = false;

            SpawnEvents.InvokeSpawnerDeactivate(new SpawnerDeactivateEventArgs(
                this, reason, _totalSpawnCount, _runTime
            ));

            OnDeactivated();
        }

        /// <summary>
        /// 激活时回调
        /// </summary>
        protected virtual void OnActivated() { }

        /// <summary>
        /// 停用时回调
        /// </summary>
        protected virtual void OnDeactivated() { }

        #endregion

        #region Spawn Logic

        /// <summary>
        /// 更新生成逻辑 (子类重写)
        /// </summary>
        protected abstract void UpdateSpawnLogic(float deltaTime);

        /// <summary>
        /// 执行生成
        /// </summary>
        protected virtual GameObject DoSpawn(SpawnEntry entry, Vector3 position, Quaternion rotation)
        {
            if (entry == null || entry.Prefab == null) return null;

            // 检查限制
            if (!CanSpawn()) return null;

            // 计算实际位置和旋转
            Vector3 spawnPos = _spawnData.GetSpawnPosition(position);
            Quaternion spawnRot = _spawnData.GetSpawnRotation(rotation);
            Vector3 scale = entry.GetActualScale();

            // 生成实体
            GameObject entity;
            if (_spawnData.UsePooling && SpawnManager.Instance != null)
            {
                entity = SpawnManager.Instance.Spawn(entry.Prefab, spawnPos, spawnRot);
            }
            else
            {
                entity = Instantiate(entry.Prefab, spawnPos, spawnRot);
            }

            if (entity == null) return null;

            // 设置缩放
            entity.transform.localScale = scale;

            // 追踪实体
            _activeEntities.Add(entity);
            _totalSpawnCount++;

            // 注册死亡监听
            var health = entity.GetComponent<Combat.IHealth>();
            if (health != null)
            {
                health.OnDeath += (args) => OnEntityKilled(entity);
            }

            // 发布事件
            SpawnEvents.InvokeSpawn(new SpawnEventArgs(
                this, entity, spawnPos, spawnRot, entry, GetCurrentWaveIndex(), _spawnData
            ));

            return entity;
        }

        /// <summary>
        /// 批量生成
        /// </summary>
        protected virtual List<GameObject> DoSpawnBatch(SpawnEntry entry, Vector3 position, Quaternion rotation, int count)
        {
            var spawned = new List<GameObject>(count);

            for (int i = 0; i < count; i++)
            {
                var entity = DoSpawn(entry, position, rotation);
                if (entity != null)
                {
                    spawned.Add(entity);
                }
            }

            return spawned;
        }

        /// <summary>
        /// 检查是否可以生成
        /// </summary>
        protected virtual bool CanSpawn()
        {
            if (_spawnData == null) return false;

            // 检查最大活跃数量
            if (_spawnData.MaxActiveCount > 0 && _activeEntities.Count >= _spawnData.MaxActiveCount)
            {
                return false;
            }

            // 检查总生成限制
            if (_spawnData.TotalSpawnLimit > 0 && _totalSpawnCount >= _spawnData.TotalSpawnLimit)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取下一个生成条目
        /// </summary>
        protected virtual SpawnEntry GetNextEntry()
        {
            if (_spawnData == null || _spawnData.Entries.Count == 0) return null;

            switch (_spawnData.Mode)
            {
                case SpawnMode.Sequential:
                    var entry = _spawnData.Entries[_currentEntryIndex];
                    _currentEntryIndex = (_currentEntryIndex + 1) % _spawnData.Entries.Count;
                    return entry.IsEnabled ? entry : GetNextEntry();

                case SpawnMode.Random:
                    return _spawnData.GetRandomEntry();

                case SpawnMode.Loop:
                    goto case SpawnMode.Sequential;

                case SpawnMode.Burst:
                    // Burst 模式在激活时一次性生成所有
                    return null;

                default:
                    return _spawnData.Entries[0];
            }
        }

        /// <summary>
        /// 获取当前波次索引 (供子类重写)
        /// </summary>
        protected virtual int GetCurrentWaveIndex() => 0;

        #endregion

        #region Entity Management

        /// <summary>
        /// 实体被击杀时
        /// </summary>
        protected virtual void OnEntityKilled(GameObject entity)
        {
            _killCount++;
            _activeEntities.Remove(entity);

            SpawnEvents.InvokeDespawn(new DespawnEventArgs(
                this, entity, DespawnReason.Killed
            ));

            // 检查是否全部清除
            CheckAllCleared();
        }

        /// <summary>
        /// 清理已销毁的实体
        /// </summary>
        protected virtual void CleanupDestroyedEntities()
        {
            _entitiesToRemove.Clear();

            for (int i = 0; i < _activeEntities.Count; i++)
            {
                if (_activeEntities[i] == null)
                {
                    _entitiesToRemove.Add(_activeEntities[i]);
                }
            }

            for (int i = 0; i < _entitiesToRemove.Count; i++)
            {
                _activeEntities.Remove(_entitiesToRemove[i]);
            }
        }

        /// <summary>
        /// 清除所有实体
        /// </summary>
        public virtual void ClearAllEntities(DespawnReason reason = DespawnReason.Manual)
        {
            for (int i = _activeEntities.Count - 1; i >= 0; i--)
            {
                var entity = _activeEntities[i];
                if (entity != null)
                {
                    SpawnEvents.InvokeDespawn(new DespawnEventArgs(this, entity, reason));

                    if (_spawnData != null && _spawnData.UsePooling && SpawnManager.Instance != null)
                    {
                        SpawnManager.Instance.Despawn(entity);
                    }
                    else
                    {
                        Destroy(entity);
                    }
                }
            }

            _activeEntities.Clear();
        }

        /// <summary>
        /// 检查是否全部清除
        /// </summary>
        protected virtual void CheckAllCleared()
        {
            if (_activeEntities.Count == 0 && _totalSpawnCount > 0)
            {
                SpawnEvents.InvokeAllCleared(new AllClearedEventArgs(
                    this, _killCount, _totalSpawnCount, _runTime
                ));

                if (_autoDeactivateWhenComplete && !CanSpawn())
                {
                    Deactivate("AllCleared");
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 设置生成数据
        /// </summary>
        public void SetSpawnData(SpawnDataSO data)
        {
            _spawnData = data;
        }

        /// <summary>
        /// 强制生成一个实体
        /// </summary>
        public GameObject ForceSpawn()
        {
            if (_spawnData == null) return null;

            var entry = GetNextEntry();
            if (entry == null) return null;

            return DoSpawn(entry, transform.position, transform.rotation);
        }

        /// <summary>
        /// 获取所有活跃实体
        /// </summary>
        public IReadOnlyList<GameObject> GetActiveEntities() => _activeEntities;

        #endregion

        #region Gizmos

        protected virtual void OnDrawGizmos()
        {
            if (!_showDebugGizmos) return;

            // 绘制生成点
            Gizmos.color = _isActive ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // 绘制随机范围
            if (_spawnData != null && _spawnData.PositionRandomRange != Vector3.zero)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawWireCube(transform.position + _spawnData.PositionOffset,
                    _spawnData.PositionRandomRange * 2);
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (_spawnData == null) return;

            // 绘制偏移
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + _spawnData.PositionOffset);
            Gizmos.DrawWireSphere(transform.position + _spawnData.PositionOffset, 0.2f);
        }

        #endregion
    }
}
