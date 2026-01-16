using UnityEngine;

namespace ZeroEngine.Projectile
{
    /// <summary>
    /// 抛物线轨迹
    /// </summary>
    public class ParabolicTrajectory : TrajectoryBase
    {
        public override TrajectoryType Type => TrajectoryType.Parabolic;

        private Vector3 _velocity;
        private Vector3 _gravity;
        private float _groundY;

        public override void Initialize(Vector3 startPosition, Vector3 direction, Transform target, ProjectileDataSO data)
        {
            base.Initialize(startPosition, direction, target, data);

            // 计算初始速度
            float angle = data.LaunchAngle * Mathf.Deg2Rad;
            Vector3 horizontalDir = new Vector3(direction.x, 0, direction.z).normalized;

            // 如果有目标，计算到目标的抛物线
            if (target != null)
            {
                Vector3 targetPos = target.position;
                _velocity = CalculateLaunchVelocity(startPosition, targetPos, data.Gravity, angle);
            }
            else
            {
                // 使用固定角度和速度
                _velocity = horizontalDir * Mathf.Cos(angle) * _speed + Vector3.up * Mathf.Sin(angle) * _speed;
            }

            _gravity = Vector3.down * data.Gravity;
            _groundY = startPosition.y - 100f; // 默认地面高度
        }

        public override Vector3 UpdatePosition(Vector3 currentPosition, float deltaTime)
        {
            _elapsedTime += deltaTime;

            // 更新速度（应用重力）
            _velocity += _gravity * deltaTime;

            // 更新方向
            _direction = _velocity.normalized;

            // 计算新位置
            Vector3 newPosition = currentPosition + _velocity * deltaTime;

            // 检查是否落地
            if (newPosition.y <= _groundY)
            {
                _isComplete = true;
                newPosition.y = _groundY;
            }

            return newPosition;
        }

        public override Vector3 GetDirection()
        {
            return _velocity.normalized;
        }

        public override float GetSpeed()
        {
            return _velocity.magnitude;
        }

        /// <summary>
        /// 设置地面高度
        /// </summary>
        public void SetGroundY(float groundY)
        {
            _groundY = groundY;
        }

        /// <summary>
        /// 计算到达目标所需的发射速度
        /// </summary>
        private Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 target, float gravity, float angle)
        {
            Vector3 displacement = target - start;
            float horizontalDist = new Vector3(displacement.x, 0, displacement.z).magnitude;
            float verticalDist = displacement.y;

            // 使用物理公式计算初速度
            // v = sqrt(g * x^2 / (2 * cos^2(angle) * (x * tan(angle) - y)))
            float cosAngle = Mathf.Cos(angle);
            float sinAngle = Mathf.Sin(angle);
            float tanAngle = Mathf.Tan(angle);

            float denominator = 2 * cosAngle * cosAngle * (horizontalDist * tanAngle - verticalDist);

            if (denominator <= 0)
            {
                // 无法到达目标，使用默认速度
                Vector3 dir = displacement.normalized;
                return dir * _speed;
            }

            float speed = Mathf.Sqrt(gravity * horizontalDist * horizontalDist / denominator);
            speed = Mathf.Clamp(speed, _speed * 0.5f, _speed * 2f);

            Vector3 horizontalDir = new Vector3(displacement.x, 0, displacement.z).normalized;
            return horizontalDir * cosAngle * speed + Vector3.up * sinAngle * speed;
        }
    }
}
