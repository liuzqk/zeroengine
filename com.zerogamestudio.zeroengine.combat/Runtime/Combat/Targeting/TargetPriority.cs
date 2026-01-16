using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 目标优先级策略
    /// </summary>
    public static class TargetPriority
    {
        /// <summary>
        /// 按距离排序（最近优先）
        /// </summary>
        public static IEnumerable<T> SortByDistance<T>(IEnumerable<T> targets, Vector3 origin) where T : ITargetable
        {
            var list = new List<(T target, float sqrDist)>();
            foreach (var target in targets)
            {
                if (target != null)
                {
                    float sqrDist = (target.Position - origin).sqrMagnitude;
                    list.Add((target, sqrDist));
                }
            }
            list.Sort((a, b) => a.sqrDist.CompareTo(b.sqrDist));
            foreach (var item in list)
            {
                yield return item.target;
            }
        }

        /// <summary>
        /// 按距离排序（最远优先）
        /// </summary>
        public static IEnumerable<T> SortByDistanceFarthest<T>(IEnumerable<T> targets, Vector3 origin) where T : ITargetable
        {
            var list = new List<(T target, float sqrDist)>();
            foreach (var target in targets)
            {
                if (target != null)
                {
                    float sqrDist = (target.Position - origin).sqrMagnitude;
                    list.Add((target, sqrDist));
                }
            }
            list.Sort((a, b) => b.sqrDist.CompareTo(a.sqrDist));
            foreach (var item in list)
            {
                yield return item.target;
            }
        }

        /// <summary>
        /// 按优先级排序（高优先级优先）
        /// </summary>
        public static IEnumerable<T> SortByPriority<T>(IEnumerable<T> targets) where T : ITargetable
        {
            var list = new List<T>();
            foreach (var target in targets)
            {
                if (target != null) list.Add(target);
            }
            list.Sort((a, b) => b.TargetPriority.CompareTo(a.TargetPriority));
            return list;
        }

        /// <summary>
        /// 按生命值百分比排序（适用于ICombatant）
        /// </summary>
        public static IEnumerable<ICombatant> SortByHealthPercent(IEnumerable<ICombatant> targets, bool lowestFirst = true)
        {
            var list = new List<ICombatant>();
            foreach (var target in targets)
            {
                if (target != null) list.Add(target);
            }

            list.Sort((a, b) =>
            {
                // 假设有获取生命值百分比的方法
                float percentA = GetHealthPercent(a);
                float percentB = GetHealthPercent(b);
                return lowestFirst
                    ? percentA.CompareTo(percentB)
                    : percentB.CompareTo(percentA);
            });

            return list;
        }

        /// <summary>
        /// 获取生命值百分比（辅助方法）
        /// </summary>
        private static float GetHealthPercent(ICombatant combatant)
        {
            // 尝试获取 IHealth 组件
            if (combatant.GameObject != null)
            {
                var health = combatant.GameObject.GetComponent<IHealth>();
                if (health != null)
                {
                    return health.HealthPercent;
                }
            }
            return 1f; // 默认满血
        }

        /// <summary>
        /// 按威胁值排序（需要威胁值系统）
        /// </summary>
        public static IEnumerable<T> SortByThreat<T>(
            IEnumerable<T> targets,
            Func<T, float> threatGetter,
            bool highestFirst = true) where T : ITargetable
        {
            var list = new List<(T target, float threat)>();
            foreach (var target in targets)
            {
                if (target != null)
                {
                    float threat = threatGetter(target);
                    list.Add((target, threat));
                }
            }

            if (highestFirst)
            {
                list.Sort((a, b) => b.threat.CompareTo(a.threat));
            }
            else
            {
                list.Sort((a, b) => a.threat.CompareTo(b.threat));
            }

            foreach (var item in list)
            {
                yield return item.target;
            }
        }

        /// <summary>
        /// 按自定义评分排序
        /// </summary>
        public static IEnumerable<T> SortByScore<T>(
            IEnumerable<T> targets,
            Func<T, float> scoreFunc,
            bool highestFirst = true) where T : ITargetable
        {
            var list = new List<(T target, float score)>();
            foreach (var target in targets)
            {
                if (target != null)
                {
                    float score = scoreFunc(target);
                    list.Add((target, score));
                }
            }

            if (highestFirst)
            {
                list.Sort((a, b) => b.score.CompareTo(a.score));
            }
            else
            {
                list.Sort((a, b) => a.score.CompareTo(b.score));
            }

            foreach (var item in list)
            {
                yield return item.target;
            }
        }

        /// <summary>
        /// 获取最近目标
        /// </summary>
        public static T GetNearest<T>(IEnumerable<T> targets, Vector3 origin) where T : class, ITargetable
        {
            T nearest = null;
            float minSqrDist = float.MaxValue;

            foreach (var target in targets)
            {
                if (target == null) continue;

                float sqrDist = (target.Position - origin).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    nearest = target;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 获取最远目标
        /// </summary>
        public static T GetFarthest<T>(IEnumerable<T> targets, Vector3 origin) where T : class, ITargetable
        {
            T farthest = null;
            float maxSqrDist = float.MinValue;

            foreach (var target in targets)
            {
                if (target == null) continue;

                float sqrDist = (target.Position - origin).sqrMagnitude;
                if (sqrDist > maxSqrDist)
                {
                    maxSqrDist = sqrDist;
                    farthest = target;
                }
            }

            return farthest;
        }

        /// <summary>
        /// 随机选择目标
        /// </summary>
        public static T GetRandom<T>(IEnumerable<T> targets) where T : class, ITargetable
        {
            var list = new List<T>();
            foreach (var target in targets)
            {
                if (target != null) list.Add(target);
            }

            if (list.Count == 0) return null;

            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// 随机选择多个目标
        /// </summary>
        public static IEnumerable<T> GetRandomMultiple<T>(IEnumerable<T> targets, int count) where T : class, ITargetable
        {
            var list = new List<T>();
            foreach (var target in targets)
            {
                if (target != null) list.Add(target);
            }

            if (list.Count == 0) yield break;

            count = Mathf.Min(count, list.Count);

            // Fisher-Yates 洗牌
            for (int i = 0; i < count; i++)
            {
                int j = UnityEngine.Random.Range(i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
                yield return list[i];
            }
        }
    }

    /// <summary>
    /// 目标选择策略枚举
    /// </summary>
    public enum TargetSelectionStrategy
    {
        /// <summary>最近</summary>
        Nearest,
        /// <summary>最远</summary>
        Farthest,
        /// <summary>血量最低</summary>
        LowestHealth,
        /// <summary>血量最高</summary>
        HighestHealth,
        /// <summary>优先级最高</summary>
        HighestPriority,
        /// <summary>随机</summary>
        Random,
        /// <summary>威胁最高</summary>
        HighestThreat,
        /// <summary>自定义</summary>
        Custom
    }
}
