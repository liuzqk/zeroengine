using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Spawner
{
    /// <summary>
    /// 定点生成器 - 在预定义的点位生成
    /// </summary>
    public class PointSpawner : SpawnerBase
    {
        [Header("Spawn Points")]
        [SerializeField] private List<Transform> _spawnPoints = new();
        [SerializeField] private bool _useChildrenAsPoints = false;

        [Header("Point Selection")]
        [SerializeField] private PointSelectionMode _selectionMode = PointSelectionMode.Sequential;
        [SerializeField] private bool _allowReuse = true;

        [Header("Point Validation")]
        [SerializeField] private bool _validatePoints = true;
        [SerializeField] private float _pointValidationRadius = 0.5f;
        [SerializeField] private LayerMask _blockingLayers;

        // 运行时状态
        private int _currentPointIndex;
        private List<Transform> _availablePoints = new();
        private List<int> _usedPointIndices = new();

        #region Properties

        /// <summary>生成点数量</summary>
        public int PointCount => _spawnPoints.Count;

        /// <summary>可用点数量</summary>
        public int AvailablePointCount => _availablePoints.Count;

        /// <summary>当前点索引</summary>
        public int CurrentPointIndex => _currentPointIndex;

        #endregion

        #region Unity Lifecycle

        protected override void Start()
        {
            // 收集子物体作为生成点
            if (_useChildrenAsPoints)
            {
                CollectChildPoints();
            }

            // 初始化可用点列表
            RefreshAvailablePoints();

            base.Start();
        }

        #endregion

        #region Point Management

        /// <summary>
        /// 收集子物体作为生成点
        /// </summary>
        private void CollectChildPoints()
        {
            _spawnPoints.Clear();

            foreach (Transform child in transform)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    _spawnPoints.Add(child);
                }
            }
        }

        /// <summary>
        /// 刷新可用点列表
        /// </summary>
        public void RefreshAvailablePoints()
        {
            _availablePoints.Clear();
            _usedPointIndices.Clear();

            for (int i = 0; i < _spawnPoints.Count; i++)
            {
                var point = _spawnPoints[i];
                if (point != null && point.gameObject.activeInHierarchy)
                {
                    _availablePoints.Add(point);
                }
            }

            _currentPointIndex = 0;

            // 随机模式下打乱顺序
            if (_selectionMode == PointSelectionMode.Random)
            {
                ShufflePoints();
            }
        }

        /// <summary>
        /// 打乱点位顺序
        /// </summary>
        private void ShufflePoints()
        {
            for (int i = _availablePoints.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_availablePoints[i], _availablePoints[j]) = (_availablePoints[j], _availablePoints[i]);
            }
        }

        /// <summary>
        /// 添加生成点
        /// </summary>
        public void AddSpawnPoint(Transform point)
        {
            if (point != null && !_spawnPoints.Contains(point))
            {
                _spawnPoints.Add(point);
                _availablePoints.Add(point);
            }
        }

        /// <summary>
        /// 移除生成点
        /// </summary>
        public void RemoveSpawnPoint(Transform point)
        {
            _spawnPoints.Remove(point);
            _availablePoints.Remove(point);
        }

        /// <summary>
        /// 获取下一个生成点
        /// </summary>
        private Transform GetNextSpawnPoint()
        {
            if (_availablePoints.Count == 0)
            {
                if (_allowReuse)
                {
                    RefreshAvailablePoints();
                }

                if (_availablePoints.Count == 0)
                {
                    return null;
                }
            }

            Transform point = null;

            switch (_selectionMode)
            {
                case PointSelectionMode.Sequential:
                    point = _availablePoints[_currentPointIndex];
                    _currentPointIndex = (_currentPointIndex + 1) % _availablePoints.Count;
                    break;

                case PointSelectionMode.Random:
                    int randomIndex = Random.Range(0, _availablePoints.Count);
                    point = _availablePoints[randomIndex];

                    if (!_allowReuse)
                    {
                        _availablePoints.RemoveAt(randomIndex);
                    }
                    break;

                case PointSelectionMode.Closest:
                    point = GetClosestPoint(transform.position);
                    break;

                case PointSelectionMode.Farthest:
                    point = GetFarthestPoint(transform.position);
                    break;

                case PointSelectionMode.LeastUsed:
                    point = GetLeastUsedPoint();
                    break;
            }

            // 验证点位
            if (_validatePoints && point != null && !IsPointValid(point.position))
            {
                _availablePoints.Remove(point);
                return GetNextSpawnPoint(); // 递归获取下一个
            }

            return point;
        }

        /// <summary>
        /// 获取最近点
        /// </summary>
        private Transform GetClosestPoint(Vector3 position)
        {
            Transform closest = null;
            float minDist = float.MaxValue;

            foreach (var point in _availablePoints)
            {
                float dist = (point.position - position).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = point;
                }
            }

            return closest;
        }

        /// <summary>
        /// 获取最远点
        /// </summary>
        private Transform GetFarthestPoint(Vector3 position)
        {
            Transform farthest = null;
            float maxDist = 0f;

            foreach (var point in _availablePoints)
            {
                float dist = (point.position - position).sqrMagnitude;
                if (dist > maxDist)
                {
                    maxDist = dist;
                    farthest = point;
                }
            }

            return farthest;
        }

        /// <summary>
        /// 获取使用次数最少的点
        /// </summary>
        private Transform GetLeastUsedPoint()
        {
            // 简单实现：返回第一个可用点
            return _availablePoints.Count > 0 ? _availablePoints[0] : null;
        }

        /// <summary>
        /// 验证点位是否可用
        /// </summary>
        private bool IsPointValid(Vector3 position)
        {
            if (_blockingLayers == 0) return true;

            return !Physics.CheckSphere(position, _pointValidationRadius, _blockingLayers);
        }

        #endregion

        #region Spawn Logic

        protected override void UpdateSpawnLogic(float deltaTime)
        {
            _spawnTimer -= deltaTime;

            if (_spawnTimer <= 0f && CanSpawn())
            {
                var entry = GetNextEntry();
                var point = GetNextSpawnPoint();

                if (entry != null && point != null)
                {
                    int count = entry.GetActualCount();
                    for (int i = 0; i < count; i++)
                    {
                        // 每个实体使用不同的点（如果需要）
                        Transform spawnPoint = i == 0 ? point : GetNextSpawnPoint();
                        if (spawnPoint == null) spawnPoint = point;

                        DoSpawn(entry, spawnPoint.position, spawnPoint.rotation);
                    }
                }

                _spawnTimer = _spawnData?.GetActualInterval() ?? 1f;
            }
        }

        protected override bool CanSpawn()
        {
            if (!base.CanSpawn()) return false;

            // 检查是否有可用点
            if (_availablePoints.Count == 0 && !_allowReuse)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 在指定点生成
        /// </summary>
        public GameObject SpawnAtPoint(int pointIndex)
        {
            if (pointIndex < 0 || pointIndex >= _spawnPoints.Count)
            {
                Debug.LogWarning($"[PointSpawner] Invalid point index: {pointIndex}");
                return null;
            }

            var point = _spawnPoints[pointIndex];
            if (point == null) return null;

            var entry = GetNextEntry();
            if (entry == null) return null;

            return DoSpawn(entry, point.position, point.rotation);
        }

        /// <summary>
        /// 在所有点同时生成
        /// </summary>
        public List<GameObject> SpawnAtAllPoints()
        {
            var spawned = new List<GameObject>();
            var entry = GetNextEntry();

            if (entry == null) return spawned;

            foreach (var point in _spawnPoints)
            {
                if (point == null) continue;

                if (_validatePoints && !IsPointValid(point.position))
                {
                    continue;
                }

                var entity = DoSpawn(entry, point.position, point.rotation);
                if (entity != null)
                {
                    spawned.Add(entity);
                }
            }

            return spawned;
        }

        /// <summary>
        /// 获取所有生成点
        /// </summary>
        public IReadOnlyList<Transform> GetSpawnPoints() => _spawnPoints;

        #endregion

        #region Gizmos

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            // 绘制所有生成点
            for (int i = 0; i < _spawnPoints.Count; i++)
            {
                var point = _spawnPoints[i];
                if (point == null) continue;

                bool isAvailable = !_validatePoints || IsPointValid(point.position);
                Gizmos.color = isAvailable ? Color.green : Color.red;

                Gizmos.DrawWireSphere(point.position, 0.3f);
                Gizmos.DrawLine(transform.position, point.position);

                // 绘制方向
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(point.position, point.forward * 0.5f);
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // 绘制验证半径
            if (_validatePoints)
            {
                Gizmos.color = new Color(1, 0, 0, 0.2f);
                foreach (var point in _spawnPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawSphere(point.position, _pointValidationRadius);
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 点位选择模式
    /// </summary>
    public enum PointSelectionMode
    {
        /// <summary>顺序选择</summary>
        Sequential,
        /// <summary>随机选择</summary>
        Random,
        /// <summary>最近点</summary>
        Closest,
        /// <summary>最远点</summary>
        Farthest,
        /// <summary>最少使用</summary>
        LeastUsed
    }
}
