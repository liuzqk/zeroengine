using UnityEngine;

namespace ZeroEngine.Projectile
{
    /// <summary>
    /// 弹道基类 - 所有弹道的基础组件
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ProjectileBase : MonoBehaviour
    {
        [Header("Runtime Data")]
        [SerializeField] private ProjectileDataSO _data;

        private ITrajectory _trajectory;
        private Transform _target;
        private Transform _owner;
        private Vector3 _direction;
        private float _lifetime;
        private int _bounceCount;
        private int _penetrateCount;
        private bool _isActive;
        private int _hitMask;

        // 碰撞检测缓存 (NonAlloc)
        private static readonly Collider[] _hitColliders = new Collider[32];
        private static readonly RaycastHit[] _raycastHits = new RaycastHit[16];

        // 组件缓存
        private Rigidbody _rigidbody;
        private Collider _collider;

        #region Properties

        /// <summary>弹道配置数据</summary>
        public ProjectileDataSO Data => _data;

        /// <summary>当前轨迹</summary>
        public ITrajectory Trajectory => _trajectory;

        /// <summary>目标Transform</summary>
        public Transform Target => _target;

        /// <summary>发射者</summary>
        public Transform Owner => _owner;

        /// <summary>当前方向</summary>
        public Vector3 Direction => _direction;

        /// <summary>剩余弹跳次数</summary>
        public int RemainingBounces => _data != null ? _data.BounceCount - _bounceCount : 0;

        /// <summary>剩余穿透次数</summary>
        public int RemainingPenetrations => _data != null ? _data.PierceCount - _penetrateCount : 0;

        /// <summary>是否激活</summary>
        public bool IsActive => _isActive;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();

            // 配置 Rigidbody
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void Update()
        {
            if (!_isActive || _data == null) return;

            float deltaTime = Time.deltaTime;
            _lifetime += deltaTime;

            // 检查生命周期
            if (_lifetime >= _data.MaxLifetime)
            {
                Destroy(ProjectileDestroyReason.Timeout);
                return;
            }

            // 更新轨迹
            UpdateTrajectory(deltaTime);

            // 检查距离限制
            if (_data.MaxDistance > 0)
            {
                float distance = Vector3.Distance(transform.position, _trajectory != null ?
                    ((TrajectoryBase)_trajectory).GetStartPosition() : transform.position);
                if (distance >= _data.MaxDistance)
                {
                    Destroy(ProjectileDestroyReason.MaxDistance);
                    return;
                }
            }

            // 检查轨迹是否完成
            if (_trajectory != null && _trajectory.IsComplete)
            {
                Destroy(ProjectileDestroyReason.TrajectoryComplete);
                return;
            }
        }

        private void FixedUpdate()
        {
            if (!_isActive || _data == null) return;

            // 碰撞检测
            PerformCollisionDetection();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive || _data == null) return;

            // 检查是否为有效目标
            if (!IsValidTarget(other)) return;

            ProcessHit(other, transform.position);
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化弹道
        /// </summary>
        public void Initialize(ProjectileDataSO data, Vector3 position, Vector3 direction, Transform owner, Transform target = null, int hitMask = -1)
        {
            _data = data;
            _owner = owner;
            _target = target;
            _direction = direction.normalized;
            _hitMask = hitMask;
            _lifetime = 0f;
            _bounceCount = 0;
            _penetrateCount = 0;
            _isActive = true;

            transform.position = position;
            transform.forward = _direction;

            // 创建轨迹
            _trajectory = CreateTrajectory(data.TrajectoryType);
            _trajectory?.Initialize(position, _direction, target, data);

            // 配置碰撞体
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }

            // 发布发射事件
            ProjectileEvents.InvokeLaunch(new ProjectileLaunchEventArgs(
                this, position, _direction, target, null
            ));
        }

        /// <summary>
        /// 创建轨迹实例
        /// </summary>
        private ITrajectory CreateTrajectory(TrajectoryType type)
        {
            return type switch
            {
                TrajectoryType.Linear => new LinearTrajectory(),
                TrajectoryType.Parabolic => new ParabolicTrajectory(),
                TrajectoryType.Homing => new HomingTrajectory(),
                TrajectoryType.Bezier => new BezierTrajectory(),
                TrajectoryType.Custom => null, // 由外部设置
                _ => new LinearTrajectory()
            };
        }

        /// <summary>
        /// 设置自定义轨迹
        /// </summary>
        public void SetTrajectory(ITrajectory trajectory)
        {
            _trajectory = trajectory;
            if (_trajectory != null && _data != null)
            {
                _trajectory.Initialize(transform.position, _direction, _target, _data);
            }
        }

        #endregion

        #region Movement

        /// <summary>
        /// 更新轨迹位置
        /// </summary>
        private void UpdateTrajectory(float deltaTime)
        {
            if (_trajectory == null) return;

            Vector3 newPosition = _trajectory.UpdatePosition(transform.position, deltaTime);
            _direction = _trajectory.GetDirection();

            // 平滑移动
            transform.position = newPosition;

            // 更新朝向
            if (_direction != Vector3.zero)
            {
                transform.forward = _direction;
            }
        }

        #endregion

        #region Collision

        /// <summary>
        /// 执行碰撞检测 (Raycast 方式，更精确)
        /// </summary>
        private void PerformCollisionDetection()
        {
            if (_data == null) return;

            float speed = _trajectory?.GetSpeed() ?? _data.Speed;
            float checkDistance = speed * Time.fixedDeltaTime * 1.5f;

            // 射线检测
            int hitCount = Physics.RaycastNonAlloc(
                transform.position,
                _direction,
                _raycastHits,
                checkDistance,
                _hitMask
            );

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _raycastHits[i];
                if (!IsValidTarget(hit.collider)) continue;

                ProcessHit(hit.collider, hit.point);

                if (!_isActive) break;
            }
        }

        /// <summary>
        /// 检查是否为有效目标
        /// </summary>
        private bool IsValidTarget(Collider other)
        {
            if (other == null) return false;

            // 忽略发射者
            if (_owner != null && other.transform.IsChildOf(_owner))
            {
                return false;
            }

            // 检查层级掩码
            int layer = 1 << other.gameObject.layer;
            if ((_hitMask & layer) == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 处理命中
        /// </summary>
        private void ProcessHit(Collider other, Vector3 hitPoint)
        {
            // 获取目标接口
            var targetable = other.GetComponentInParent<Combat.ITargetable>();
            var health = other.GetComponentInParent<Combat.IHealth>();

            // 计算伤害
            Combat.DamageData? damageData = null;
            Combat.DamageResult? damageResult = null;

            if (_data.BaseDamage > 0 && health != null)
            {
                var ownerCombatant = _owner != null ? _owner.GetComponent<Combat.ICombatant>() : null;
                damageData = Combat.DamageData.Physical(_data.BaseDamage, ownerCombatant);
                damageResult = health.TakeDamage(damageData.Value);
            }

            // AOE 伤害
            if (_data.AOERadius > 0)
            {
                ApplyAOEDamage(hitPoint);
            }

            // 发布命中事件
            ProjectileEvents.InvokeHit(new ProjectileHitEventArgs(
                this, other.gameObject, hitPoint, _direction, damageData, damageResult
            ));

            // 处理穿透
            if (_data.PierceCount > 0 && _penetrateCount < _data.PierceCount)
            {
                _penetrateCount++;
                return; // 继续飞行
            }

            // 处理弹跳
            if (_data.BounceCount > 0 && _bounceCount < _data.BounceCount)
            {
                Bounce(other, hitPoint);
                return; // 继续飞行
            }

            // 销毁
            Destroy(ProjectileDestroyReason.Hit);
        }

        /// <summary>
        /// 弹跳
        /// </summary>
        private void Bounce(Collider surface, Vector3 hitPoint)
        {
            _bounceCount++;

            // 计算反射方向
            Vector3 normal = (transform.position - hitPoint).normalized;
            if (normal == Vector3.zero)
            {
                // 使用碰撞体表面法线
                normal = surface.ClosestPoint(transform.position + _direction) - hitPoint;
                normal = normal.normalized;
            }

            Vector3 reflectedDirection = Vector3.Reflect(_direction, normal);
            _direction = reflectedDirection;

            // 更新轨迹方向
            _trajectory?.SetDirection(_direction);

            // 发布弹跳事件
            ProjectileEvents.InvokeBounce(new ProjectileBounceEventArgs(
                this, hitPoint, normal, reflectedDirection, _bounceCount
            ));
        }

        /// <summary>
        /// 应用 AOE 伤害
        /// </summary>
        private void ApplyAOEDamage(Vector3 center)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                _data.AOERadius,
                _hitColliders,
                _hitMask
            );

            for (int i = 0; i < hitCount; i++)
            {
                var col = _hitColliders[i];
                if (!IsValidTarget(col)) continue;

                var health = col.GetComponentInParent<Combat.IHealth>();
                if (health == null) continue;

                // 计算距离衰减
                float distance = Vector3.Distance(center, col.transform.position);
                float normalizedDistance = Mathf.Clamp01(distance / _data.AOERadius);
                float falloff = _data.AOEDamageFalloff.Evaluate(normalizedDistance);

                float aoeDamage = _data.BaseDamage * falloff;
                if (aoeDamage > 0)
                {
                    var ownerCombatant = _owner != null ? _owner.GetComponent<Combat.ICombatant>() : null;
                    var damageData = Combat.DamageData.Physical(aoeDamage, ownerCombatant);
                    health.TakeDamage(damageData);
                }
            }
        }

        #endregion

        #region Destruction

        /// <summary>
        /// 销毁弹道
        /// </summary>
        public void Destroy(ProjectileDestroyReason reason)
        {
            if (!_isActive) return;

            _isActive = false;

            // 发布销毁事件
            ProjectileEvents.InvokeDestroy(new ProjectileDestroyEventArgs(
                this, transform.position, reason
            ));

            // 通知管理器回收
            if (ProjectileManager.Instance != null)
            {
                ProjectileManager.Instance.Despawn(this);
            }
            else
            {
                GameObject.Destroy(gameObject);
            }
        }

        /// <summary>
        /// 重置状态 (用于对象池)
        /// </summary>
        public void Reset()
        {
            _data = null;
            _trajectory = null;
            _target = null;
            _owner = null;
            _direction = Vector3.forward;
            _lifetime = 0f;
            _bounceCount = 0;
            _penetrateCount = 0;
            _isActive = false;
        }

        #endregion

        #region Target Management

        /// <summary>
        /// 设置新目标 (用于追踪弹道)
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            _target = newTarget;
            _trajectory?.SetTarget(newTarget);
        }

        /// <summary>
        /// 清除目标
        /// </summary>
        public void ClearTarget()
        {
            _target = null;
            _trajectory?.SetTarget(null);
        }

        #endregion
    }

    /// <summary>
    /// TrajectoryBase 扩展方法
    /// </summary>
    public static class TrajectoryBaseExtensions
    {
        /// <summary>
        /// 获取起始位置 (需要在 TrajectoryBase 中添加此方法)
        /// </summary>
        public static Vector3 GetStartPosition(this TrajectoryBase trajectory)
        {
            // 通过反射获取 _startPosition (不推荐，但保持兼容)
            var field = typeof(TrajectoryBase).GetField("_startPosition",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (Vector3)field.GetValue(trajectory) : Vector3.zero;
        }
    }
}
