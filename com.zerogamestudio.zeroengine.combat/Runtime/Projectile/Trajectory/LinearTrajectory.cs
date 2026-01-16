using UnityEngine;

namespace ZeroEngine.Projectile
{
    /// <summary>
    /// 直线轨迹
    /// </summary>
    public class LinearTrajectory : TrajectoryBase
    {
        public override TrajectoryType Type => TrajectoryType.Linear;

        public override Vector3 UpdatePosition(Vector3 currentPosition, float deltaTime)
        {
            _elapsedTime += deltaTime;
            return currentPosition + _direction * _speed * deltaTime;
        }
    }
}
