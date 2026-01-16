using UnityEngine;

namespace ZeroEngine.Projectile
{
    /// <summary>
    /// 贝塞尔曲线轨迹
    /// </summary>
    public class BezierTrajectory : TrajectoryBase
    {
        public override TrajectoryType Type => TrajectoryType.Bezier;

        private Vector3 _p0; // 起点
        private Vector3 _p1; // 控制点
        private Vector3 _p2; // 终点
        private float _duration;
        private float _t;

        public override void Initialize(Vector3 startPosition, Vector3 direction, Transform target, ProjectileDataSO data)
        {
            base.Initialize(startPosition, direction, target, data);

            _p0 = startPosition;

            // 计算终点
            if (target != null)
            {
                _p2 = target.position;
            }
            else
            {
                // 无目标时，沿方向飞行固定距离
                float distance = data.MaxDistance > 0 ? data.MaxDistance : data.Speed * data.MaxLifetime;
                _p2 = startPosition + direction * distance;
            }

            // 计算控制点
            Vector3 midPoint = (_p0 + _p2) * 0.5f;
            Vector3 offset = data.ControlPointOffset;

            // 添加随机变化
            if (data.CurveHeightVariance > 0)
            {
                offset.y += Random.Range(-data.CurveHeightVariance, data.CurveHeightVariance);
            }

            // 将偏移转换到本地坐标
            Vector3 forward = (_p2 - _p0).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 up = Vector3.Cross(forward, right);

            _p1 = midPoint + right * offset.x + up * offset.y + forward * offset.z;

            // 计算曲线长度和持续时间
            float curveLength = EstimateCurveLength();
            _duration = curveLength / data.Speed;

            _t = 0f;
        }

        public override Vector3 UpdatePosition(Vector3 currentPosition, float deltaTime)
        {
            _elapsedTime += deltaTime;
            _t = _elapsedTime / _duration;

            if (_t >= 1f)
            {
                _t = 1f;
                _isComplete = true;
            }

            // 计算贝塞尔曲线位置
            Vector3 newPosition = CalculateBezierPoint(_t);

            // 更新方向（切线方向）
            _direction = CalculateBezierTangent(_t);

            return newPosition;
        }

        /// <summary>
        /// 计算二次贝塞尔曲线上的点
        /// B(t) = (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
        /// </summary>
        private Vector3 CalculateBezierPoint(float t)
        {
            float oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * _p0 +
                   2f * oneMinusT * t * _p1 +
                   t * t * _p2;
        }

        /// <summary>
        /// 计算贝塞尔曲线切线方向
        /// B'(t) = 2(1-t)(P1-P0) + 2t(P2-P1)
        /// </summary>
        private Vector3 CalculateBezierTangent(float t)
        {
            float oneMinusT = 1f - t;
            Vector3 tangent = 2f * oneMinusT * (_p1 - _p0) + 2f * t * (_p2 - _p1);
            return tangent.normalized;
        }

        /// <summary>
        /// 估算曲线长度（分段线性近似）
        /// </summary>
        private float EstimateCurveLength(int segments = 20)
        {
            float length = 0f;
            Vector3 prevPoint = _p0;

            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 point = CalculateBezierPoint(t);
                length += Vector3.Distance(prevPoint, point);
                prevPoint = point;
            }

            return length;
        }

        /// <summary>
        /// 获取当前 t 值（曲线进度 0-1）
        /// </summary>
        public float Progress => _t;

        /// <summary>
        /// 设置新的终点
        /// </summary>
        public void SetEndPoint(Vector3 endPoint)
        {
            _p2 = endPoint;
            // 重新计算控制点
            Vector3 midPoint = (_p0 + _p2) * 0.5f;
            _p1 = new Vector3(midPoint.x, _p1.y, midPoint.z);
        }
    }
}
