using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 范围检测工具类
    /// </summary>
    public static class RangeChecker
    {
        /// <summary>
        /// 检查点是否在圆形范围内
        /// </summary>
        public static bool IsInCircle(Vector3 center, float radius, Vector3 point)
        {
            return (point - center).sqrMagnitude <= radius * radius;
        }

        /// <summary>
        /// 检查点是否在圆形范围内（2D平面）
        /// </summary>
        public static bool IsInCircle2D(Vector3 center, float radius, Vector3 point)
        {
            Vector2 center2D = new Vector2(center.x, center.z);
            Vector2 point2D = new Vector2(point.x, point.z);
            return (point2D - center2D).sqrMagnitude <= radius * radius;
        }

        /// <summary>
        /// 检查点是否在扇形范围内
        /// </summary>
        public static bool IsInCone(Vector3 origin, Vector3 direction, float range, float angle, Vector3 point)
        {
            Vector3 toPoint = point - origin;
            float distance = toPoint.magnitude;

            // 距离检查
            if (distance > range) return false;

            // 角度检查
            if (distance > 0.001f)
            {
                float dot = Vector3.Dot(toPoint.normalized, direction.normalized);
                float cosHalfAngle = Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);
                return dot >= cosHalfAngle;
            }

            return true; // 在原点
        }

        /// <summary>
        /// 检查点是否在扇形范围内（2D平面）
        /// </summary>
        public static bool IsInCone2D(Vector3 origin, Vector3 direction, float range, float angle, Vector3 point)
        {
            Vector2 origin2D = new Vector2(origin.x, origin.z);
            Vector2 direction2D = new Vector2(direction.x, direction.z).normalized;
            Vector2 point2D = new Vector2(point.x, point.z);

            Vector2 toPoint = point2D - origin2D;
            float distance = toPoint.magnitude;

            if (distance > range) return false;

            if (distance > 0.001f)
            {
                float dot = Vector2.Dot(toPoint.normalized, direction2D);
                float cosHalfAngle = Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);
                return dot >= cosHalfAngle;
            }

            return true;
        }

        /// <summary>
        /// 检查点是否在矩形范围内
        /// </summary>
        public static bool IsInRect(Vector3 center, Vector3 size, Quaternion rotation, Vector3 point)
        {
            Vector3 halfSize = size * 0.5f;
            Matrix4x4 worldToLocal = Matrix4x4.TRS(center, rotation, Vector3.one).inverse;
            Vector3 localPoint = worldToLocal.MultiplyPoint3x4(point);

            return Mathf.Abs(localPoint.x) <= halfSize.x &&
                   Mathf.Abs(localPoint.y) <= halfSize.y &&
                   Mathf.Abs(localPoint.z) <= halfSize.z;
        }

        /// <summary>
        /// 检查点是否在矩形范围内（2D平面）
        /// </summary>
        public static bool IsInRect2D(Vector3 center, float width, float height, float rotationY, Vector3 point)
        {
            // 旋转到本地坐标
            float cos = Mathf.Cos(-rotationY * Mathf.Deg2Rad);
            float sin = Mathf.Sin(-rotationY * Mathf.Deg2Rad);

            float dx = point.x - center.x;
            float dz = point.z - center.z;

            float localX = dx * cos - dz * sin;
            float localZ = dx * sin + dz * cos;

            return Mathf.Abs(localX) <= width * 0.5f && Mathf.Abs(localZ) <= height * 0.5f;
        }

        /// <summary>
        /// 计算两点间距离
        /// </summary>
        public static float GetDistance(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }

        /// <summary>
        /// 计算两点间距离（2D平面）
        /// </summary>
        public static float GetDistance2D(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// 计算两点间距离平方（避免开方运算）
        /// </summary>
        public static float GetSqrDistance(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude;
        }

        /// <summary>
        /// 检查是否在范围内
        /// </summary>
        public static bool IsInRange(Vector3 a, Vector3 b, float range)
        {
            return (a - b).sqrMagnitude <= range * range;
        }

        /// <summary>
        /// 检查是否在范围内（带碰撞体半径）
        /// </summary>
        public static bool IsInRange(ITargetable source, ITargetable target, float range)
        {
            float sourceRadius = source.GetTargetRadius();
            float targetRadius = target.GetTargetRadius();
            float totalRange = range + sourceRadius + targetRadius;

            return GetSqrDistance(source.Position, target.Position) <= totalRange * totalRange;
        }

        /// <summary>
        /// 检查是否有视线（无障碍物）
        /// </summary>
        public static bool HasLineOfSight(Vector3 origin, Vector3 target, LayerMask obstacleMask)
        {
            Vector3 direction = target - origin;
            float distance = direction.magnitude;

            return !Physics.Raycast(origin, direction.normalized, distance, obstacleMask);
        }

        /// <summary>
        /// 检查是否有视线（带目标碰撞体）
        /// </summary>
        public static bool HasLineOfSight(Vector3 origin, ITargetable target, LayerMask obstacleMask)
        {
            Vector3 targetCenter = target.GetTargetCenter();
            return HasLineOfSight(origin, targetCenter, obstacleMask);
        }

        /// <summary>
        /// 获取圆形范围内的碰撞体
        /// </summary>
        public static int OverlapSphere(Vector3 center, float radius, Collider[] results, LayerMask layerMask)
        {
            return Physics.OverlapSphereNonAlloc(center, radius, results, layerMask);
        }

        /// <summary>
        /// 获取盒形范围内的碰撞体
        /// </summary>
        public static int OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, Collider[] results, LayerMask layerMask)
        {
            return Physics.OverlapBoxNonAlloc(center, halfExtents, results, orientation, layerMask);
        }

        /// <summary>
        /// 获取胶囊范围内的碰撞体
        /// </summary>
        public static int OverlapCapsule(Vector3 point0, Vector3 point1, float radius, Collider[] results, LayerMask layerMask)
        {
            return Physics.OverlapCapsuleNonAlloc(point0, point1, radius, results, layerMask);
        }

        /// <summary>
        /// 获取扇形范围内的目标
        /// </summary>
        public static IEnumerable<T> GetTargetsInCone<T>(
            IEnumerable<T> candidates,
            Vector3 origin,
            Vector3 direction,
            float range,
            float angle) where T : ITargetable
        {
            foreach (var target in candidates)
            {
                if (target != null && IsInCone(origin, direction, range, angle, target.Position))
                {
                    yield return target;
                }
            }
        }

        /// <summary>
        /// 获取矩形范围内的目标
        /// </summary>
        public static IEnumerable<T> GetTargetsInRect<T>(
            IEnumerable<T> candidates,
            Vector3 center,
            Vector3 size,
            Quaternion rotation) where T : ITargetable
        {
            foreach (var target in candidates)
            {
                if (target != null && IsInRect(center, size, rotation, target.Position))
                {
                    yield return target;
                }
            }
        }

        /// <summary>
        /// 获取圆形范围内的目标
        /// </summary>
        public static IEnumerable<T> GetTargetsInCircle<T>(
            IEnumerable<T> candidates,
            Vector3 center,
            float radius) where T : ITargetable
        {
            float sqrRadius = radius * radius;
            foreach (var target in candidates)
            {
                if (target != null && (target.Position - center).sqrMagnitude <= sqrRadius)
                {
                    yield return target;
                }
            }
        }
    }

    /// <summary>
    /// 范围形状定义
    /// </summary>
    [Serializable]
    public struct RangeShape
    {
        /// <summary>形状类型</summary>
        public RangeShapeType ShapeType;

        /// <summary>半径/范围</summary>
        public float Range;

        /// <summary>角度（扇形用）</summary>
        public float Angle;

        /// <summary>宽度（矩形用）</summary>
        public float Width;

        /// <summary>高度（矩形用）</summary>
        public float Height;

        /// <summary>偏移</summary>
        public Vector3 Offset;

        /// <summary>
        /// 创建圆形范围
        /// </summary>
        public static RangeShape Circle(float radius)
        {
            return new RangeShape { ShapeType = RangeShapeType.Circle, Range = radius };
        }

        /// <summary>
        /// 创建扇形范围
        /// </summary>
        public static RangeShape Cone(float range, float angle)
        {
            return new RangeShape { ShapeType = RangeShapeType.Cone, Range = range, Angle = angle };
        }

        /// <summary>
        /// 创建矩形范围
        /// </summary>
        public static RangeShape Rect(float width, float height)
        {
            return new RangeShape { ShapeType = RangeShapeType.Rectangle, Width = width, Height = height };
        }

        /// <summary>
        /// 检查点是否在范围内
        /// </summary>
        public bool Contains(Vector3 origin, Vector3 forward, Vector3 point)
        {
            Vector3 checkPoint = point - Offset;

            return ShapeType switch
            {
                RangeShapeType.Circle => RangeChecker.IsInCircle(origin, Range, checkPoint),
                RangeShapeType.Cone => RangeChecker.IsInCone(origin, forward, Range, Angle, checkPoint),
                RangeShapeType.Rectangle => RangeChecker.IsInRect2D(origin, Width, Height, Vector3.SignedAngle(Vector3.forward, forward, Vector3.up), checkPoint),
                _ => false
            };
        }
    }

    /// <summary>
    /// 范围形状类型
    /// </summary>
    public enum RangeShapeType
    {
        /// <summary>圆形</summary>
        Circle,
        /// <summary>扇形</summary>
        Cone,
        /// <summary>矩形</summary>
        Rectangle
    }
}
