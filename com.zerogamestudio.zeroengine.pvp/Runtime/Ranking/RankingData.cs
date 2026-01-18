using System;
using System.Collections.Generic;

namespace ZeroEngine.PvP.Ranking
{
    /// <summary>
    /// 排名数据
    /// </summary>
    [Serializable]
    public class RankingData
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        public string PlayerId { get; set; }

        /// <summary>
        /// 玩家名称
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// 当前排名
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// 积分
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// 段位
        /// </summary>
        public RankTier Tier { get; set; } = RankTier.Bronze;

        /// <summary>
        /// 段位星数
        /// </summary>
        public int TierStars { get; set; }

        /// <summary>
        /// 攻击胜利次数
        /// </summary>
        public int AttackWins { get; set; }

        /// <summary>
        /// 攻击失败次数
        /// </summary>
        public int AttackLosses { get; set; }

        /// <summary>
        /// 防守胜利次数
        /// </summary>
        public int DefenseWins { get; set; }

        /// <summary>
        /// 防守失败次数
        /// </summary>
        public int DefenseLosses { get; set; }

        /// <summary>
        /// 连胜次数
        /// </summary>
        public int WinStreak { get; set; }

        /// <summary>
        /// 最高连胜
        /// </summary>
        public int MaxWinStreak { get; set; }

        /// <summary>
        /// 攻击胜率
        /// </summary>
        public float AttackWinRate => AttackWins + AttackLosses > 0
            ? (float)AttackWins / (AttackWins + AttackLosses)
            : 0f;

        /// <summary>
        /// 防守胜率
        /// </summary>
        public float DefenseWinRate => DefenseWins + DefenseLosses > 0
            ? (float)DefenseWins / (DefenseWins + DefenseLosses)
            : 0f;

        /// <summary>
        /// 记录攻击结果
        /// </summary>
        public void RecordAttack(bool won, int scoreChange)
        {
            if (won)
            {
                AttackWins++;
                WinStreak++;
                if (WinStreak > MaxWinStreak)
                    MaxWinStreak = WinStreak;
            }
            else
            {
                AttackLosses++;
                WinStreak = 0;
            }

            Score += scoreChange;
            UpdateTier();
        }

        /// <summary>
        /// 记录防守结果
        /// </summary>
        public void RecordDefense(bool won, int scoreChange)
        {
            if (won)
            {
                DefenseWins++;
            }
            else
            {
                DefenseLosses++;
            }

            Score += scoreChange;
            UpdateTier();
        }

        /// <summary>
        /// 更新段位
        /// </summary>
        private void UpdateTier()
        {
            // 根据积分更新段位
            if (Score < 1000)
            {
                Tier = RankTier.Bronze;
                TierStars = Score / 200;
            }
            else if (Score < 2000)
            {
                Tier = RankTier.Silver;
                TierStars = (Score - 1000) / 200;
            }
            else if (Score < 3000)
            {
                Tier = RankTier.Gold;
                TierStars = (Score - 2000) / 200;
            }
            else if (Score < 4000)
            {
                Tier = RankTier.Platinum;
                TierStars = (Score - 3000) / 200;
            }
            else if (Score < 5000)
            {
                Tier = RankTier.Diamond;
                TierStars = (Score - 4000) / 200;
            }
            else
            {
                Tier = RankTier.Master;
                TierStars = (Score - 5000) / 500;
            }

            TierStars = Math.Min(TierStars, 5);
        }
    }

    /// <summary>
    /// 段位
    /// </summary>
    public enum RankTier
    {
        Bronze,
        Silver,
        Gold,
        Platinum,
        Diamond,
        Master,
        GrandMaster
    }

    /// <summary>
    /// 赛季配置
    /// </summary>
    [Serializable]
    public class SeasonConfig
    {
        /// <summary>
        /// 赛季ID
        /// </summary>
        public string SeasonId { get; set; }

        /// <summary>
        /// 赛季名称
        /// </summary>
        public string SeasonName { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 胜利获得积分
        /// </summary>
        public int WinScore { get; set; } = 30;

        /// <summary>
        /// 失败扣除积分
        /// </summary>
        public int LoseScore { get; set; } = 10;

        /// <summary>
        /// 防守胜利奖励积分
        /// </summary>
        public int DefenseWinScore { get; set; } = 5;

        /// <summary>
        /// 连胜奖励积分（每场额外）
        /// </summary>
        public int WinStreakBonus { get; set; } = 5;
    }
}
