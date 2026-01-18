using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.PvP.Matching
{
    /// <summary>
    /// 匹配服务
    /// </summary>
    public class MatchingService
    {
        /// <summary>
        /// 匹配配置
        /// </summary>
        public MatchingConfig Config { get; }

        /// <summary>
        /// 候选队伍池
        /// </summary>
        private readonly List<Snapshot.TeamSnapshot> _candidatePool = new();

        private System.Random _random = new();

        public MatchingService(MatchingConfig config = null)
        {
            Config = config ?? new MatchingConfig();
        }

        /// <summary>
        /// 添加候选队伍到池中
        /// </summary>
        public void AddCandidate(Snapshot.TeamSnapshot snapshot)
        {
            _candidatePool.Add(snapshot);
        }

        /// <summary>
        /// 清空候选池
        /// </summary>
        public void ClearPool()
        {
            _candidatePool.Clear();
        }

        /// <summary>
        /// 匹配对手
        /// </summary>
        public Snapshot.TeamSnapshot FindOpponent(int playerPower, int floorLevel = 1)
        {
            if (_candidatePool.Count == 0)
                return null;

            // 计算战力范围
            float tolerance = Config.PowerTolerance + floorLevel * Config.FloorToleranceBonus;
            int minPower = Mathf.RoundToInt(playerPower * (1f - tolerance));
            int maxPower = Mathf.RoundToInt(playerPower * (1f + tolerance));

            // 筛选符合条件的候选
            var validCandidates = new List<Snapshot.TeamSnapshot>();
            foreach (var candidate in _candidatePool)
            {
                if (candidate.TotalPower >= minPower && candidate.TotalPower <= maxPower)
                {
                    validCandidates.Add(candidate);
                }
            }

            // 如果没有完全匹配的，放宽条件
            if (validCandidates.Count == 0)
            {
                minPower = Mathf.RoundToInt(playerPower * 0.5f);
                maxPower = Mathf.RoundToInt(playerPower * 1.5f);

                foreach (var candidate in _candidatePool)
                {
                    if (candidate.TotalPower >= minPower && candidate.TotalPower <= maxPower)
                    {
                        validCandidates.Add(candidate);
                    }
                }
            }

            // 仍然没有就随机选一个
            if (validCandidates.Count == 0 && _candidatePool.Count > 0)
            {
                return _candidatePool[_random.Next(_candidatePool.Count)];
            }

            if (validCandidates.Count == 0)
                return null;

            // 随机选择一个
            return validCandidates[_random.Next(validCandidates.Count)];
        }

        /// <summary>
        /// 批量匹配多个对手（用于地下城遭遇）
        /// </summary>
        public List<Snapshot.TeamSnapshot> FindOpponents(int playerPower, int count, int floorLevel = 1)
        {
            var results = new List<Snapshot.TeamSnapshot>();
            var used = new HashSet<string>();

            for (int i = 0; i < count && i < _candidatePool.Count; i++)
            {
                var opponent = FindOpponent(playerPower, floorLevel);
                if (opponent != null && !used.Contains(opponent.SnapshotId))
                {
                    results.Add(opponent);
                    used.Add(opponent.SnapshotId);
                }
            }

            return results;
        }
    }

    /// <summary>
    /// 匹配配置
    /// </summary>
    [Serializable]
    public class MatchingConfig
    {
        /// <summary>
        /// 战力容差（百分比）
        /// </summary>
        public float PowerTolerance { get; set; } = 0.1f;

        /// <summary>
        /// 每层增加的容差
        /// </summary>
        public float FloorToleranceBonus { get; set; } = 0.02f;

        /// <summary>
        /// 最大等待时间（秒）
        /// </summary>
        public float MaxWaitTime { get; set; } = 5f;
    }
}
