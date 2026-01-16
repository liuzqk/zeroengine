// ============================================================================
// SectInstance.cs
// 运行时门派实例
// 创建于: 2026-01-09
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Character.Sect
{
    /// <summary>
    /// 运行时门派实例
    /// 记录角色在门派中的状态 (职位、贡献度、声望等)
    /// </summary>
    [Serializable]
    public class SectInstance
    {
        /// <summary>门派类型</summary>
        public SectType SectType { get; private set; }

        /// <summary>门派数据引用</summary>
        public SectDataSO Data { get; private set; }

        /// <summary>当前职位</summary>
        public SectRank CurrentRank { get; private set; }

        /// <summary>当前贡献度</summary>
        public int Contribution { get; private set; }

        /// <summary>累计贡献度 (历史总计)</summary>
        public int TotalContribution { get; private set; }

        /// <summary>门派声望 (可正可负)</summary>
        public int Reputation { get; private set; }

        /// <summary>入门时间</summary>
        public DateTime JoinDate { get; private set; }

        /// <summary>已学习的门派武学 ID 列表</summary>
        public List<string> LearnedMartialArts { get; private set; } = new List<string>();

        /// <summary>是否被逐出门派</summary>
        public bool IsExpelled { get; private set; }

        /// <summary>逐出原因</summary>
        public string ExpelReason { get; private set; }

        // ===== 事件 =====

        /// <summary>职位变化事件</summary>
        public event Action<SectRank, SectRank> OnRankChanged;

        /// <summary>贡献度变化事件</summary>
        public event Action<int, int> OnContributionChanged;

        /// <summary>声望变化事件</summary>
        public event Action<int, int> OnReputationChanged;

        /// <summary>学习武学事件</summary>
        public event Action<string> OnMartialArtLearned;

        // ===== 构造 =====

        public SectInstance(SectDataSO data, SectRank initialRank = SectRank.Initiate)
        {
            Data = data;
            SectType = data.sectType;
            CurrentRank = initialRank;
            Contribution = 0;
            TotalContribution = 0;
            Reputation = 0;
            JoinDate = DateTime.Now;
            IsExpelled = false;
        }

        /// <summary>
        /// 反序列化构造
        /// </summary>
        public SectInstance(SectSaveData saveData, SectDataSO data)
        {
            Data = data;
            SectType = saveData.sectType;
            CurrentRank = saveData.rank;
            Contribution = saveData.contribution;
            TotalContribution = saveData.totalContribution;
            Reputation = saveData.reputation;
            JoinDate = DateTime.Parse(saveData.joinDate);
            LearnedMartialArts = new List<string>(saveData.learnedMartialArts);
            IsExpelled = saveData.isExpelled;
            ExpelReason = saveData.expelReason;
        }

        // ===== 贡献度操作 =====

        /// <summary>
        /// 增加贡献度
        /// </summary>
        public void AddContribution(int amount)
        {
            if (amount <= 0) return;

            int oldValue = Contribution;
            Contribution += amount;
            TotalContribution += amount;

            OnContributionChanged?.Invoke(oldValue, Contribution);

            // 检查是否可以晋升
            CheckPromotion();
        }

        /// <summary>
        /// 消耗贡献度 (学习武学等)
        /// </summary>
        public bool SpendContribution(int amount)
        {
            if (amount <= 0 || Contribution < amount) return false;

            int oldValue = Contribution;
            Contribution -= amount;

            OnContributionChanged?.Invoke(oldValue, Contribution);
            return true;
        }

        // ===== 声望操作 =====

        /// <summary>
        /// 修改声望
        /// </summary>
        public void ModifyReputation(int delta)
        {
            if (delta == 0) return;

            int oldValue = Reputation;
            Reputation += delta;

            OnReputationChanged?.Invoke(oldValue, Reputation);

            // 声望过低可能被逐出
            if (Reputation < -100 && !IsExpelled)
            {
                Expel("声望过低，被逐出师门");
            }
        }

        // ===== 职位操作 =====

        /// <summary>
        /// 晋升职位
        /// </summary>
        public bool Promote(SectRank newRank)
        {
            if ((int)newRank <= (int)CurrentRank) return false;
            if (IsExpelled) return false;

            // 检查晋升条件
            var requirement = Data?.GetRankRequirement(newRank);
            if (requirement != null)
            {
                if (TotalContribution < requirement.contributionRequired)
                    return false;
            }

            SectRank oldRank = CurrentRank;
            CurrentRank = newRank;

            OnRankChanged?.Invoke(oldRank, CurrentRank);
            return true;
        }

        /// <summary>
        /// 降级职位
        /// </summary>
        public bool Demote(SectRank newRank)
        {
            if ((int)newRank >= (int)CurrentRank) return false;
            if (newRank == SectRank.None) return false;

            SectRank oldRank = CurrentRank;
            CurrentRank = newRank;

            OnRankChanged?.Invoke(oldRank, CurrentRank);
            return true;
        }

        /// <summary>
        /// 检查并自动晋升
        /// </summary>
        private void CheckPromotion()
        {
            if (Data == null || IsExpelled) return;

            // 从当前职位的下一级开始检查
            for (int i = (int)CurrentRank + 1; i <= (int)SectRank.Elder; i++)
            {
                SectRank targetRank = (SectRank)i;
                var requirement = Data.GetRankRequirement(targetRank);

                if (requirement == null) continue;

                if (TotalContribution >= requirement.contributionRequired)
                {
                    // 满足条件，晋升
                    Promote(targetRank);
                }
                else
                {
                    // 不满足条件，停止检查更高职位
                    break;
                }
            }
        }

        // ===== 武学操作 =====

        /// <summary>
        /// 学习门派武学
        /// </summary>
        public bool LearnMartialArt(string martialArtId)
        {
            if (string.IsNullOrEmpty(martialArtId)) return false;
            if (LearnedMartialArts.Contains(martialArtId)) return false;
            if (IsExpelled) return false;

            // 检查是否可以学习
            if (Data != null && !Data.CanLearnMartialArt(martialArtId, CurrentRank, Contribution))
                return false;

            // 获取武学条目，扣除贡献度
            var entry = Data?.martialArts.Find(m => m.martialArtId == martialArtId);
            if (entry != null && entry.contributionCost > 0)
            {
                if (!SpendContribution(entry.contributionCost))
                    return false;
            }

            LearnedMartialArts.Add(martialArtId);
            OnMartialArtLearned?.Invoke(martialArtId);

            return true;
        }

        /// <summary>
        /// 检查是否已学习指定武学
        /// </summary>
        public bool HasLearnedMartialArt(string martialArtId)
        {
            return LearnedMartialArts.Contains(martialArtId);
        }

        /// <summary>
        /// 获取当前可学习的武学列表
        /// </summary>
        public List<SectMartialArtEntry> GetAvailableMartialArts()
        {
            if (Data == null) return new List<SectMartialArtEntry>();

            return Data.martialArts.FindAll(m =>
                !LearnedMartialArts.Contains(m.martialArtId) &&
                (int)CurrentRank >= (int)m.requiredRank &&
                Contribution >= m.contributionCost);
        }

        // ===== 逐出门派 =====

        /// <summary>
        /// 被逐出门派
        /// </summary>
        public void Expel(string reason)
        {
            if (IsExpelled) return;

            IsExpelled = true;
            ExpelReason = reason;
            CurrentRank = SectRank.None;

            // 注意: 已学武学不会被清除 (但可能有使用限制)
        }

        /// <summary>
        /// 重新加入门派 (特殊情况)
        /// </summary>
        public void Rejoin(SectRank rank = SectRank.Initiate)
        {
            if (!IsExpelled) return;

            IsExpelled = false;
            ExpelReason = null;
            CurrentRank = rank;
            Contribution = 0; // 贡献度清零
            Reputation = 0;   // 声望清零
        }

        // ===== 序列化 =====

        /// <summary>
        /// 转换为存档数据
        /// </summary>
        public SectSaveData ToSaveData()
        {
            return new SectSaveData
            {
                sectType = SectType,
                rank = CurrentRank,
                contribution = Contribution,
                totalContribution = TotalContribution,
                reputation = Reputation,
                joinDate = JoinDate.ToString("O"),
                learnedMartialArts = LearnedMartialArts.ToArray(),
                isExpelled = IsExpelled,
                expelReason = ExpelReason
            };
        }
    }

    /// <summary>
    /// 门派存档数据
    /// </summary>
    [Serializable]
    public class SectSaveData
    {
        public SectType sectType;
        public SectRank rank;
        public int contribution;
        public int totalContribution;
        public int reputation;
        public string joinDate;
        public string[] learnedMartialArts;
        public bool isExpelled;
        public string expelReason;
    }
}
