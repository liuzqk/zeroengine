using System;
using System.Collections.Generic;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 战斗队伍/阵营定义
    /// </summary>
    [Serializable]
    public class CombatTeam
    {
        /// <summary>队伍ID</summary>
        public int TeamId;

        /// <summary>队伍名称</summary>
        public string TeamName;

        /// <summary>队伍颜色（用于UI显示）</summary>
        public UnityEngine.Color TeamColor = UnityEngine.Color.white;

        /// <summary>队伍成员</summary>
        private readonly List<ICombatant> _members = new();

        /// <summary>只读成员列表</summary>
        public IReadOnlyList<ICombatant> Members => _members;

        /// <summary>存活成员数量</summary>
        public int AliveCount
        {
            get
            {
                int count = 0;
                foreach (var member in _members)
                {
                    if (member.IsAlive) count++;
                }
                return count;
            }
        }

        /// <summary>队伍是否全灭</summary>
        public bool IsDefeated => AliveCount == 0;

        public CombatTeam(int teamId, string teamName = null)
        {
            TeamId = teamId;
            TeamName = teamName ?? $"Team_{teamId}";
        }

        /// <summary>
        /// 添加成员
        /// </summary>
        public void AddMember(ICombatant combatant)
        {
            if (combatant != null && !_members.Contains(combatant))
            {
                _members.Add(combatant);
            }
        }

        /// <summary>
        /// 移除成员
        /// </summary>
        public bool RemoveMember(ICombatant combatant)
        {
            return _members.Remove(combatant);
        }

        /// <summary>
        /// 清空成员
        /// </summary>
        public void ClearMembers()
        {
            _members.Clear();
        }

        /// <summary>
        /// 检查是否包含成员
        /// </summary>
        public bool Contains(ICombatant combatant)
        {
            return _members.Contains(combatant);
        }

        /// <summary>
        /// 获取所有存活成员
        /// </summary>
        public IEnumerable<ICombatant> GetAliveMembers()
        {
            foreach (var member in _members)
            {
                if (member.IsAlive)
                {
                    yield return member;
                }
            }
        }
    }

    /// <summary>
    /// 队伍关系定义
    /// </summary>
    public enum TeamRelation
    {
        /// <summary>友好（同队）</summary>
        Friendly,

        /// <summary>中立</summary>
        Neutral,

        /// <summary>敌对</summary>
        Hostile
    }

    /// <summary>
    /// 队伍关系管理器
    /// </summary>
    public class TeamRelationManager
    {
        private readonly Dictionary<(int, int), TeamRelation> _relations = new();

        /// <summary>默认关系</summary>
        public TeamRelation DefaultRelation { get; set; } = TeamRelation.Hostile;

        /// <summary>
        /// 设置队伍关系
        /// </summary>
        public void SetRelation(int teamA, int teamB, TeamRelation relation)
        {
            // 确保 key 的顺序一致
            var key = teamA < teamB ? (teamA, teamB) : (teamB, teamA);
            _relations[key] = relation;
        }

        /// <summary>
        /// 获取队伍关系
        /// </summary>
        public TeamRelation GetRelation(int teamA, int teamB)
        {
            if (teamA == teamB) return TeamRelation.Friendly;

            var key = teamA < teamB ? (teamA, teamB) : (teamB, teamA);
            return _relations.TryGetValue(key, out var relation) ? relation : DefaultRelation;
        }

        /// <summary>
        /// 检查是否敌对
        /// </summary>
        public bool IsHostile(int teamA, int teamB)
        {
            return GetRelation(teamA, teamB) == TeamRelation.Hostile;
        }

        /// <summary>
        /// 检查是否友好
        /// </summary>
        public bool IsFriendly(int teamA, int teamB)
        {
            return GetRelation(teamA, teamB) == TeamRelation.Friendly;
        }

        /// <summary>
        /// 检查两个战斗单位是否敌对
        /// </summary>
        public bool IsHostile(ICombatant a, ICombatant b)
        {
            if (a == null || b == null) return false;
            return IsHostile(a.TeamId, b.TeamId);
        }

        /// <summary>
        /// 检查两个战斗单位是否友好
        /// </summary>
        public bool IsFriendly(ICombatant a, ICombatant b)
        {
            if (a == null || b == null) return false;
            return IsFriendly(a.TeamId, b.TeamId);
        }

        /// <summary>
        /// 清空所有关系
        /// </summary>
        public void Clear()
        {
            _relations.Clear();
        }
    }
}
