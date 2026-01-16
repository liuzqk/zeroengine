using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 目标过滤器 - 用于过滤有效目标
    /// </summary>
    public static class TargetFilter
    {
        /// <summary>
        /// 过滤存活目标
        /// </summary>
        public static IEnumerable<T> FilterAlive<T>(IEnumerable<T> targets) where T : ITargetable
        {
            foreach (var target in targets)
            {
                if (target != null && target.IsAlive)
                {
                    yield return target;
                }
            }
        }

        /// <summary>
        /// 过滤可选中目标
        /// </summary>
        public static IEnumerable<T> FilterTargetable<T>(IEnumerable<T> targets) where T : ITargetable
        {
            foreach (var target in targets)
            {
                if (target != null && target.IsTargetable)
                {
                    yield return target;
                }
            }
        }

        /// <summary>
        /// 按队伍过滤
        /// </summary>
        public static IEnumerable<T> FilterByTeam<T>(IEnumerable<T> targets, int teamId) where T : ITargetable
        {
            foreach (var target in targets)
            {
                if (target != null && target.TeamId == teamId)
                {
                    yield return target;
                }
            }
        }

        /// <summary>
        /// 过滤敌对目标
        /// </summary>
        public static IEnumerable<T> FilterHostile<T>(IEnumerable<T> targets, int sourceTeamId) where T : ITargetable
        {
            var relationManager = CombatManager.Instance?.RelationManager;
            if (relationManager == null)
            {
                // 如果没有关系管理器，认为不同队伍就是敌对
                foreach (var target in targets)
                {
                    if (target != null && target.TeamId != sourceTeamId)
                    {
                        yield return target;
                    }
                }
            }
            else
            {
                foreach (var target in targets)
                {
                    if (target != null && relationManager.IsHostile(sourceTeamId, target.TeamId))
                    {
                        yield return target;
                    }
                }
            }
        }

        /// <summary>
        /// 过滤友方目标
        /// </summary>
        public static IEnumerable<T> FilterFriendly<T>(IEnumerable<T> targets, int sourceTeamId) where T : ITargetable
        {
            var relationManager = CombatManager.Instance?.RelationManager;
            if (relationManager == null)
            {
                // 如果没有关系管理器，认为同队伍就是友方
                foreach (var target in targets)
                {
                    if (target != null && target.TeamId == sourceTeamId)
                    {
                        yield return target;
                    }
                }
            }
            else
            {
                foreach (var target in targets)
                {
                    if (target != null && relationManager.IsFriendly(sourceTeamId, target.TeamId))
                    {
                        yield return target;
                    }
                }
            }
        }

        /// <summary>
        /// 按范围过滤
        /// </summary>
        public static IEnumerable<T> FilterInRange<T>(IEnumerable<T> targets, Vector3 center, float range) where T : ITargetable
        {
            float sqrRange = range * range;
            foreach (var target in targets)
            {
                if (target != null)
                {
                    float sqrDistance = (target.Position - center).sqrMagnitude;
                    if (sqrDistance <= sqrRange)
                    {
                        yield return target;
                    }
                }
            }
        }

        /// <summary>
        /// 按圆锥范围过滤
        /// </summary>
        public static IEnumerable<T> FilterInCone<T>(
            IEnumerable<T> targets,
            Vector3 origin,
            Vector3 direction,
            float range,
            float angle) where T : ITargetable
        {
            float sqrRange = range * range;
            float cosHalfAngle = Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);
            direction = direction.normalized;

            foreach (var target in targets)
            {
                if (target == null) continue;

                Vector3 toTarget = target.Position - origin;
                float sqrDistance = toTarget.sqrMagnitude;

                // 范围检查
                if (sqrDistance > sqrRange) continue;

                // 角度检查
                float dot = Vector3.Dot(toTarget.normalized, direction);
                if (dot >= cosHalfAngle)
                {
                    yield return target;
                }
            }
        }

        /// <summary>
        /// 按矩形范围过滤
        /// </summary>
        public static IEnumerable<T> FilterInRect<T>(
            IEnumerable<T> targets,
            Vector3 center,
            Vector3 size,
            Quaternion rotation) where T : ITargetable
        {
            Vector3 halfSize = size * 0.5f;
            Matrix4x4 worldToLocal = Matrix4x4.TRS(center, rotation, Vector3.one).inverse;

            foreach (var target in targets)
            {
                if (target == null) continue;

                Vector3 localPos = worldToLocal.MultiplyPoint3x4(target.Position);

                if (Mathf.Abs(localPos.x) <= halfSize.x &&
                    Mathf.Abs(localPos.y) <= halfSize.y &&
                    Mathf.Abs(localPos.z) <= halfSize.z)
                {
                    yield return target;
                }
            }
        }

        /// <summary>
        /// 排除指定目标
        /// </summary>
        public static IEnumerable<T> Exclude<T>(IEnumerable<T> targets, T exclude) where T : ITargetable
        {
            foreach (var target in targets)
            {
                if (target != null && !ReferenceEquals(target, exclude))
                {
                    yield return target;
                }
            }
        }

        /// <summary>
        /// 排除多个目标
        /// </summary>
        public static IEnumerable<T> ExcludeMany<T>(IEnumerable<T> targets, HashSet<T> excludeSet) where T : ITargetable
        {
            foreach (var target in targets)
            {
                if (target != null && !excludeSet.Contains(target))
                {
                    yield return target;
                }
            }
        }

        /// <summary>
        /// 按自定义条件过滤
        /// </summary>
        public static IEnumerable<T> FilterBy<T>(IEnumerable<T> targets, Func<T, bool> predicate) where T : ITargetable
        {
            foreach (var target in targets)
            {
                if (target != null && predicate(target))
                {
                    yield return target;
                }
            }
        }

        /// <summary>
        /// 视线检查过滤
        /// </summary>
        public static IEnumerable<T> FilterWithLineOfSight<T>(
            IEnumerable<T> targets,
            Vector3 origin,
            LayerMask obstacleMask) where T : ITargetable
        {
            foreach (var target in targets)
            {
                if (target == null) continue;

                Vector3 targetCenter = target.GetTargetCenter();
                Vector3 direction = targetCenter - origin;
                float distance = direction.magnitude;

                if (!Physics.Raycast(origin, direction.normalized, distance, obstacleMask))
                {
                    yield return target;
                }
            }
        }
    }

    /// <summary>
    /// 目标过滤器配置
    /// </summary>
    [Serializable]
    public class TargetFilterConfig
    {
        /// <summary>目标关系</summary>
        public TargetRelationType RelationType = TargetRelationType.Hostile;

        /// <summary>是否需要存活</summary>
        public bool RequireAlive = true;

        /// <summary>是否需要可选中</summary>
        public bool RequireTargetable = true;

        /// <summary>最大范围（0表示无限）</summary>
        public float MaxRange = 0f;

        /// <summary>是否需要视线</summary>
        public bool RequireLineOfSight = false;

        /// <summary>视线检测层级</summary>
        public LayerMask ObstacleMask = ~0;

        /// <summary>自定义过滤标签</summary>
        public string[] IncludeTags;

        /// <summary>排除标签</summary>
        public string[] ExcludeTags;
    }

    /// <summary>
    /// 目标关系类型
    /// </summary>
    public enum TargetRelationType
    {
        /// <summary>任意</summary>
        Any,
        /// <summary>敌对</summary>
        Hostile,
        /// <summary>友方</summary>
        Friendly,
        /// <summary>自身</summary>
        Self,
        /// <summary>非自身</summary>
        NotSelf
    }
}
