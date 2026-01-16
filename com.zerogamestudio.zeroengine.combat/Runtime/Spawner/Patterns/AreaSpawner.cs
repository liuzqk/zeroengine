using UnityEngine;

namespace ZeroEngine.Spawner
{
    /// <summary>
    /// 区域生成器 - 在指定区域内随机生成
    /// </summary>
    public class AreaSpawner : SpawnerBase
    {
        [Header("Area Settings")]
        [SerializeField] private AreaShape _areaShape = AreaShape.Box;
        [SerializeField] private Vector3 _areaSize = new Vector3(10f, 0f, 10f);
        [SerializeField] private float _areaRadius = 5f;

        [Header("Spawn Distribution")]
        [SerializeField] private SpawnDistribution _distribution = SpawnDistribution.Random;
        [SerializeField] private bool _avoidOverlap = true;
        [SerializeField] private float _minSpacing = 1f;
        [SerializeField] private int _maxPlacementAttempts = 10;

        [Header("Surface Detection")]
        [SerializeField] private bool _snapToGround = true;
        [SerializeField] private LayerMask _groundLayer = 1;
        [SerializeField] private float _groundCheckHeight = 10f;
        [SerializeField] private float _groundOffset = 0f;

        [Header("Obstacle Avoidance")]
        [SerializeField] private bool _avoidObstacles = true;
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField] private float _obstacleCheckRadius = 0.5f;

        // 网格生成状态
        private int _gridIndex;
        private Vector3[] _gridPositions;

        #region Properties

        /// <summary>区域形状</summary>
        public AreaShape Shape => _areaShape;

        /// <summary>区域大小 (Box)</summary>
        public Vector3 AreaSize => _areaSize;

        /// <summary>区域半径 (Circle/Sphere)</summary>
        public float AreaRadius => _areaRadius;

        #endregion

        #region Spawn Logic

        protected override void UpdateSpawnLogic(float deltaTime)
        {
            _spawnTimer -= deltaTime;

            if (_spawnTimer <= 0f && CanSpawn())
            {
                var entry = GetNextEntry();
                if (entry != null)
                {
                    Vector3 spawnPos = GetSpawnPosition();
                    if (spawnPos != Vector3.zero || !_avoidObstacles)
                    {
                        int count = entry.GetActualCount();
                        for (int i = 0; i < count; i++)
                        {
                            Vector3 pos = i == 0 ? spawnPos : GetSpawnPosition();
                            Quaternion rot = GetSpawnRotation();
                            DoSpawn(entry, pos, rot);
                        }
                    }
                }

                _spawnTimer = _spawnData?.GetActualInterval() ?? 1f;
            }
        }

        /// <summary>
        /// 获取生成位置
        /// </summary>
        private Vector3 GetSpawnPosition()
        {
            for (int attempt = 0; attempt < _maxPlacementAttempts; attempt++)
            {
                Vector3 pos = _distribution switch
                {
                    SpawnDistribution.Random => GetRandomPosition(),
                    SpawnDistribution.Grid => GetGridPosition(),
                    SpawnDistribution.Edge => GetEdgePosition(),
                    SpawnDistribution.Center => transform.position,
                    _ => GetRandomPosition()
                };

                // 地面检测
                if (_snapToGround)
                {
                    pos = SnapToGround(pos);
                    if (pos == Vector3.zero) continue;
                }

                // 障碍检测
                if (_avoidObstacles && HasObstacle(pos))
                {
                    continue;
                }

                // 重叠检测
                if (_avoidOverlap && HasOverlap(pos))
                {
                    continue;
                }

                return pos;
            }

            // 无法找到有效位置，返回中心点
            return _snapToGround ? SnapToGround(transform.position) : transform.position;
        }

        /// <summary>
        /// 获取随机位置
        /// </summary>
        private Vector3 GetRandomPosition()
        {
            Vector3 localPos = _areaShape switch
            {
                AreaShape.Box => new Vector3(
                    Random.Range(-_areaSize.x * 0.5f, _areaSize.x * 0.5f),
                    Random.Range(-_areaSize.y * 0.5f, _areaSize.y * 0.5f),
                    Random.Range(-_areaSize.z * 0.5f, _areaSize.z * 0.5f)
                ),
                AreaShape.Circle => GetRandomCirclePosition(),
                AreaShape.Sphere => Random.insideUnitSphere * _areaRadius,
                _ => Vector3.zero
            };

            return transform.TransformPoint(localPos);
        }

        /// <summary>
        /// 获取圆形区域内随机位置
        /// </summary>
        private Vector3 GetRandomCirclePosition()
        {
            Vector2 circle = Random.insideUnitCircle * _areaRadius;
            return new Vector3(circle.x, 0f, circle.y);
        }

        /// <summary>
        /// 获取网格位置
        /// </summary>
        private Vector3 GetGridPosition()
        {
            if (_gridPositions == null || _gridPositions.Length == 0)
            {
                GenerateGridPositions();
            }

            if (_gridPositions.Length == 0) return transform.position;

            Vector3 pos = _gridPositions[_gridIndex];
            _gridIndex = (_gridIndex + 1) % _gridPositions.Length;

            return transform.TransformPoint(pos);
        }

        /// <summary>
        /// 生成网格位置
        /// </summary>
        private void GenerateGridPositions()
        {
            int countX = Mathf.Max(1, Mathf.FloorToInt(_areaSize.x / _minSpacing));
            int countZ = Mathf.Max(1, Mathf.FloorToInt(_areaSize.z / _minSpacing));

            _gridPositions = new Vector3[countX * countZ];

            float startX = -_areaSize.x * 0.5f + _minSpacing * 0.5f;
            float startZ = -_areaSize.z * 0.5f + _minSpacing * 0.5f;

            int index = 0;
            for (int x = 0; x < countX; x++)
            {
                for (int z = 0; z < countZ; z++)
                {
                    _gridPositions[index++] = new Vector3(
                        startX + x * _minSpacing,
                        0f,
                        startZ + z * _minSpacing
                    );
                }
            }

            // 随机打乱顺序
            for (int i = _gridPositions.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_gridPositions[i], _gridPositions[j]) = (_gridPositions[j], _gridPositions[i]);
            }

            _gridIndex = 0;
        }

        /// <summary>
        /// 获取边缘位置
        /// </summary>
        private Vector3 GetEdgePosition()
        {
            Vector3 localPos;

            if (_areaShape == AreaShape.Circle || _areaShape == AreaShape.Sphere)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                localPos = new Vector3(
                    Mathf.Cos(angle) * _areaRadius,
                    0f,
                    Mathf.Sin(angle) * _areaRadius
                );
            }
            else
            {
                // Box 边缘
                int edge = Random.Range(0, 4);
                float t = Random.Range(-0.5f, 0.5f);

                localPos = edge switch
                {
                    0 => new Vector3(-_areaSize.x * 0.5f, 0f, t * _areaSize.z), // Left
                    1 => new Vector3(_areaSize.x * 0.5f, 0f, t * _areaSize.z),  // Right
                    2 => new Vector3(t * _areaSize.x, 0f, -_areaSize.z * 0.5f), // Bottom
                    _ => new Vector3(t * _areaSize.x, 0f, _areaSize.z * 0.5f)   // Top
                };
            }

            return transform.TransformPoint(localPos);
        }

        /// <summary>
        /// 获取生成旋转
        /// </summary>
        private Quaternion GetSpawnRotation()
        {
            Quaternion rot = transform.rotation;

            if (_spawnData != null)
            {
                rot = _spawnData.GetSpawnRotation(rot);
            }

            return rot;
        }

        /// <summary>
        /// 贴地处理
        /// </summary>
        private Vector3 SnapToGround(Vector3 position)
        {
            Vector3 rayStart = position + Vector3.up * _groundCheckHeight;

            if (Physics.Raycast(rayStart, Vector3.down, out var hit, _groundCheckHeight * 2f, _groundLayer))
            {
                return hit.point + Vector3.up * _groundOffset;
            }

            return Vector3.zero; // 未找到地面
        }

        /// <summary>
        /// 检查障碍物
        /// </summary>
        private bool HasObstacle(Vector3 position)
        {
            return Physics.CheckSphere(position, _obstacleCheckRadius, _obstacleLayer);
        }

        /// <summary>
        /// 检查重叠
        /// </summary>
        private bool HasOverlap(Vector3 position)
        {
            float minSpacingSqr = _minSpacing * _minSpacing;

            for (int i = 0; i < _activeEntities.Count; i++)
            {
                var entity = _activeEntities[i];
                if (entity == null) continue;

                float distSqr = (entity.transform.position - position).sqrMagnitude;
                if (distSqr < minSpacingSqr)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 设置区域大小
        /// </summary>
        public void SetAreaSize(Vector3 size)
        {
            _areaSize = size;
            _gridPositions = null; // 重新生成网格
        }

        /// <summary>
        /// 设置区域半径
        /// </summary>
        public void SetAreaRadius(float radius)
        {
            _areaRadius = radius;
        }

        /// <summary>
        /// 检查点是否在区域内
        /// </summary>
        public bool IsPointInArea(Vector3 worldPoint)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

            return _areaShape switch
            {
                AreaShape.Box => Mathf.Abs(localPoint.x) <= _areaSize.x * 0.5f &&
                                 Mathf.Abs(localPoint.y) <= _areaSize.y * 0.5f &&
                                 Mathf.Abs(localPoint.z) <= _areaSize.z * 0.5f,
                AreaShape.Circle => new Vector2(localPoint.x, localPoint.z).magnitude <= _areaRadius,
                AreaShape.Sphere => localPoint.magnitude <= _areaRadius,
                _ => false
            };
        }

        #endregion

        #region Gizmos

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            Gizmos.color = _isActive ? new Color(0, 1, 0, 0.3f) : new Color(1, 1, 0, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;

            switch (_areaShape)
            {
                case AreaShape.Box:
                    Gizmos.DrawWireCube(Vector3.zero, _areaSize);
                    Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.1f);
                    Gizmos.DrawCube(Vector3.zero, _areaSize);
                    break;

                case AreaShape.Circle:
                    DrawCircleGizmo(_areaRadius, 32);
                    break;

                case AreaShape.Sphere:
                    Gizmos.DrawWireSphere(Vector3.zero, _areaRadius);
                    break;
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        private void DrawCircleGizmo(float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = new Vector3(radius, 0f, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 point = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }

        #endregion
    }

    /// <summary>
    /// 区域形状
    /// </summary>
    public enum AreaShape
    {
        Box,
        Circle,
        Sphere
    }

    /// <summary>
    /// 生成分布方式
    /// </summary>
    public enum SpawnDistribution
    {
        /// <summary>完全随机</summary>
        Random,
        /// <summary>网格分布</summary>
        Grid,
        /// <summary>边缘分布</summary>
        Edge,
        /// <summary>中心点</summary>
        Center
    }
}
