using UnityEngine;

namespace ZeroEngine.Projectile
{
    /// <summary>
    /// 追踪轨迹
    /// </summary>
    public class HomingTrajectory : TrajectoryBase
    {
        public override TrajectoryType Type => TrajectoryType.Homing;

        private float _turnSpeed;
        private float _homingDelay;
        private bool _continueWithoutTarget;
        private bool _hasLostTarget;

        public override void Initialize(Vector3 startPosition, Vector3 direction, Transform target, ProjectileDataSO data)
        {
            base.Initialize(startPosition, direction, target, data);

            _turnSpeed = data.HomingTurnSpeed;
            _homingDelay = data.HomingDelay;
            _continueWithoutTarget = data.ContinueWithoutTarget;
            _hasLostTarget = false;
        }

        public override Vector3 UpdatePosition(Vector3 currentPosition, float deltaTime)
        {
            _elapsedTime += deltaTime;

            // 追踪延迟
            if (_elapsedTime >= _homingDelay && !_hasLostTarget)
            {
                UpdateHomingDirection(currentPosition, deltaTime);
            }

            return currentPosition + _direction * _speed * deltaTime;
        }

        /// <summary>
        /// 更新追踪方向
        /// </summary>
        private void UpdateHomingDirection(Vector3 currentPosition, float deltaTime)
        {
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                _hasLostTarget = true;

                if (!_continueWithoutTarget)
                {
                    _isComplete = true;
                }
                return;
            }

            // 计算到目标的方向
            Vector3 targetPosition = GetTargetCenter();
            Vector3 toTarget = (targetPosition - currentPosition).normalized;

            // 计算角度差
            float maxAngle = _turnSpeed * deltaTime;

            // 使用 RotateTowards 平滑转向
            _direction = Vector3.RotateTowards(_direction, toTarget, maxAngle * Mathf.Deg2Rad, 0f).normalized;
        }

        /// <summary>
        /// 获取目标中心点
        /// </summary>
        private Vector3 GetTargetCenter()
        {
            if (_target == null) return Vector3.zero;

            // 尝试获取碰撞体中心
            var collider = _target.GetComponent<Collider>();
            if (collider != null)
            {
                return collider.bounds.center;
            }

            // 尝试获取 ITargetable 中心
            var targetable = _target.GetComponent<Combat.ITargetable>();
            if (targetable != null)
            {
                return targetable.GetTargetCenter();
            }

            return _target.position;
        }

        /// <summary>
        /// 检查目标是否丢失
        /// </summary>
        public bool HasLostTarget => _hasLostTarget;

        public override void SetTarget(Transform newTarget)
        {
            base.SetTarget(newTarget);
            _hasLostTarget = false;
        }
    }
}
