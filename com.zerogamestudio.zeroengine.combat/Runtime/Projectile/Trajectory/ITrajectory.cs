using UnityEngine;

namespace ZeroEngine.Projectile
{
    /// <summary>
    /// 轨迹接口 - 定义弹道运动行为
    /// </summary>
    public interface ITrajectory
    {
        /// <summary>轨迹类型</summary>
        TrajectoryType Type { get; }

        /// <summary>
        /// 初始化轨迹
        /// </summary>
        /// <param name="startPosition">起始位置</param>
        /// <param name="direction">初始方向</param>
        /// <param name="target">目标Transform（可选）</param>
        /// <param name="data">弹道配置</param>
        void Initialize(Vector3 startPosition, Vector3 direction, Transform target, ProjectileDataSO data);

        /// <summary>
        /// 更新轨迹并返回新位置
        /// </summary>
        /// <param name="currentPosition">当前位置</param>
        /// <param name="deltaTime">时间增量</param>
        /// <returns>新位置</returns>
        Vector3 UpdatePosition(Vector3 currentPosition, float deltaTime);

        /// <summary>
        /// 获取当前移动方向
        /// </summary>
        Vector3 GetDirection();

        /// <summary>
        /// 获取当前速度
        /// </summary>
        float GetSpeed();

        /// <summary>
        /// 设置新方向（用于反弹）
        /// </summary>
        void SetDirection(Vector3 newDirection);

        /// <summary>
        /// 更新目标（用于追踪弹道）
        /// </summary>
        void SetTarget(Transform newTarget);

        /// <summary>
        /// 轨迹是否已完成（如抛物线落地）
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// 重置轨迹状态
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// 轨迹基类 - 提供通用实现
    /// </summary>
    public abstract class TrajectoryBase : ITrajectory
    {
        public abstract TrajectoryType Type { get; }

        protected Vector3 _startPosition;
        protected Vector3 _direction;
        protected Transform _target;
        protected ProjectileDataSO _data;
        protected float _elapsedTime;
        protected float _speed;
        protected bool _isComplete;

        public bool IsComplete => _isComplete;

        public virtual void Initialize(Vector3 startPosition, Vector3 direction, Transform target, ProjectileDataSO data)
        {
            _startPosition = startPosition;
            _direction = direction.normalized;
            _target = target;
            _data = data;
            _speed = data.Speed;
            _elapsedTime = 0f;
            _isComplete = false;
        }

        public abstract Vector3 UpdatePosition(Vector3 currentPosition, float deltaTime);

        public virtual Vector3 GetDirection() => _direction;

        public virtual float GetSpeed() => _speed;

        public virtual void SetDirection(Vector3 newDirection)
        {
            _direction = newDirection.normalized;
        }

        public virtual void SetTarget(Transform newTarget)
        {
            _target = newTarget;
        }

        public virtual void Reset()
        {
            _elapsedTime = 0f;
            _isComplete = false;
        }
    }
}
