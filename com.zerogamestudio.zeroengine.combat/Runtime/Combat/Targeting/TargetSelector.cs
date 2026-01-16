using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 目标选择器 - 根据配置选择合适的目标
    /// </summary>
    public class TargetSelector
    {
        private readonly List<ITargetable> _cachedTargets = new();
        private readonly HashSet<ITargetable> _excludeSet = new();

        /// <summary>选择器配置</summary>
        public TargetSelectorConfig Config { get; set; }

        /// <summary>源位置获取器</summary>
        public Func<Vector3> OriginGetter { get; set; }

        /// <summary>源队伍ID</summary>
        public int SourceTeamId { get; set; }

        /// <summary>自定义评分函数</summary>
        public Func<ITargetable, float> CustomScoreFunc { get; set; }

        public TargetSelector()
        {
            Config = new TargetSelectorConfig();
        }

        public TargetSelector(TargetSelectorConfig config)
        {
            Config = config ?? new TargetSelectorConfig();
        }

        /// <summary>
        /// 选择单个目标
        /// </summary>
        public ITargetable SelectTarget(IEnumerable<ITargetable> candidates)
        {
            return SelectTarget(candidates, OriginGetter?.Invoke() ?? Vector3.zero);
        }

        /// <summary>
        /// 选择单个目标
        /// </summary>
        public ITargetable SelectTarget(IEnumerable<ITargetable> candidates, Vector3 origin)
        {
            var filtered = ApplyFilters(candidates, origin);
            return ApplyStrategy(filtered, origin);
        }

        /// <summary>
        /// 选择多个目标
        /// </summary>
        public IEnumerable<ITargetable> SelectTargets(IEnumerable<ITargetable> candidates, int maxCount)
        {
            return SelectTargets(candidates, OriginGetter?.Invoke() ?? Vector3.zero, maxCount);
        }

        /// <summary>
        /// 选择多个目标
        /// </summary>
        public IEnumerable<ITargetable> SelectTargets(IEnumerable<ITargetable> candidates, Vector3 origin, int maxCount)
        {
            var filtered = ApplyFilters(candidates, origin);
            return ApplySortedStrategy(filtered, origin, maxCount);
        }

        /// <summary>
        /// 添加排除目标
        /// </summary>
        public void AddExclude(ITargetable target)
        {
            if (target != null)
            {
                _excludeSet.Add(target);
            }
        }

        /// <summary>
        /// 移除排除目标
        /// </summary>
        public void RemoveExclude(ITargetable target)
        {
            _excludeSet.Remove(target);
        }

        /// <summary>
        /// 清空排除列表
        /// </summary>
        public void ClearExcludes()
        {
            _excludeSet.Clear();
        }

        /// <summary>
        /// 应用过滤器
        /// </summary>
        private IEnumerable<ITargetable> ApplyFilters(IEnumerable<ITargetable> candidates, Vector3 origin)
        {
            IEnumerable<ITargetable> result = candidates;

            // 基础过滤
            if (Config.RequireAlive)
            {
                result = TargetFilter.FilterAlive(result);
            }

            if (Config.RequireTargetable)
            {
                result = TargetFilter.FilterTargetable(result);
            }

            // 关系过滤
            switch (Config.RelationType)
            {
                case TargetRelationType.Hostile:
                    result = TargetFilter.FilterHostile(result, SourceTeamId);
                    break;
                case TargetRelationType.Friendly:
                    result = TargetFilter.FilterFriendly(result, SourceTeamId);
                    break;
                case TargetRelationType.Self:
                    result = TargetFilter.FilterByTeam(result, SourceTeamId);
                    break;
            }

            // 范围过滤
            if (Config.MaxRange > 0)
            {
                result = TargetFilter.FilterInRange(result, origin, Config.MaxRange);
            }

            // 视线过滤
            if (Config.RequireLineOfSight)
            {
                result = TargetFilter.FilterWithLineOfSight(result, origin, Config.ObstacleMask);
            }

            // 排除过滤
            if (_excludeSet.Count > 0)
            {
                result = TargetFilter.ExcludeMany(result, _excludeSet);
            }

            return result;
        }

        /// <summary>
        /// 应用选择策略（单目标）
        /// </summary>
        private ITargetable ApplyStrategy(IEnumerable<ITargetable> filtered, Vector3 origin)
        {
            return Config.Strategy switch
            {
                TargetSelectionStrategy.Nearest => TargetPriority.GetNearest(filtered, origin),
                TargetSelectionStrategy.Farthest => TargetPriority.GetFarthest(filtered, origin),
                TargetSelectionStrategy.Random => TargetPriority.GetRandom(filtered),
                TargetSelectionStrategy.HighestPriority => GetFirstByPriority(filtered),
                TargetSelectionStrategy.LowestHealth => GetFirstByHealth(filtered, true),
                TargetSelectionStrategy.HighestHealth => GetFirstByHealth(filtered, false),
                TargetSelectionStrategy.Custom when CustomScoreFunc != null => GetFirstByScore(filtered, CustomScoreFunc),
                _ => TargetPriority.GetNearest(filtered, origin)
            };
        }

        /// <summary>
        /// 应用选择策略（多目标排序）
        /// </summary>
        private IEnumerable<ITargetable> ApplySortedStrategy(IEnumerable<ITargetable> filtered, Vector3 origin, int maxCount)
        {
            IEnumerable<ITargetable> sorted = Config.Strategy switch
            {
                TargetSelectionStrategy.Nearest => TargetPriority.SortByDistance(filtered, origin),
                TargetSelectionStrategy.Farthest => TargetPriority.SortByDistanceFarthest(filtered, origin),
                TargetSelectionStrategy.Random => TargetPriority.GetRandomMultiple(filtered, maxCount),
                TargetSelectionStrategy.HighestPriority => TargetPriority.SortByPriority(filtered),
                TargetSelectionStrategy.Custom when CustomScoreFunc != null =>
                    TargetPriority.SortByScore(filtered, CustomScoreFunc),
                _ => TargetPriority.SortByDistance(filtered, origin)
            };

            int count = 0;
            foreach (var target in sorted)
            {
                if (count >= maxCount) yield break;
                yield return target;
                count++;
            }
        }

        private ITargetable GetFirstByPriority(IEnumerable<ITargetable> targets)
        {
            ITargetable best = null;
            int bestPriority = int.MinValue;

            foreach (var target in targets)
            {
                if (target.TargetPriority > bestPriority)
                {
                    bestPriority = target.TargetPriority;
                    best = target;
                }
            }

            return best;
        }

        private ITargetable GetFirstByHealth(IEnumerable<ITargetable> targets, bool lowestFirst)
        {
            // 转换为 ICombatant 处理
            _cachedTargets.Clear();
            foreach (var target in targets)
            {
                _cachedTargets.Add(target);
            }

            if (_cachedTargets.Count == 0) return null;

            ITargetable best = _cachedTargets[0];
            float bestHealth = GetHealthPercent(best);

            for (int i = 1; i < _cachedTargets.Count; i++)
            {
                float health = GetHealthPercent(_cachedTargets[i]);
                bool isBetter = lowestFirst ? health < bestHealth : health > bestHealth;
                if (isBetter)
                {
                    bestHealth = health;
                    best = _cachedTargets[i];
                }
            }

            return best;
        }

        private ITargetable GetFirstByScore(IEnumerable<ITargetable> targets, Func<ITargetable, float> scoreFunc)
        {
            ITargetable best = null;
            float bestScore = float.MinValue;

            foreach (var target in targets)
            {
                float score = scoreFunc(target);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = target;
                }
            }

            return best;
        }

        private float GetHealthPercent(ITargetable target)
        {
            if (target is ICombatant combatant && combatant.GameObject != null)
            {
                var health = combatant.GameObject.GetComponent<IHealth>();
                if (health != null)
                {
                    return health.HealthPercent;
                }
            }
            return 1f;
        }
    }

    /// <summary>
    /// 目标选择器配置
    /// </summary>
    [Serializable]
    public class TargetSelectorConfig
    {
        /// <summary>目标关系</summary>
        public TargetRelationType RelationType = TargetRelationType.Hostile;

        /// <summary>选择策略</summary>
        public TargetSelectionStrategy Strategy = TargetSelectionStrategy.Nearest;

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
    }
}
