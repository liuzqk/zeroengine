using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.PvP.Snapshot
{
    /// <summary>
    /// 队伍快照，用于异步PvP
    /// </summary>
    [Serializable]
    public class TeamSnapshot
    {
        /// <summary>
        /// 快照ID
        /// </summary>
        public string SnapshotId { get; set; }

        /// <summary>
        /// 玩家ID
        /// </summary>
        public string PlayerId { get; set; }

        /// <summary>
        /// 玩家名称
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// 队伍战力
        /// </summary>
        public int TotalPower { get; set; }

        /// <summary>
        /// 单位快照列表
        /// </summary>
        public List<UnitSnapshot> Units { get; set; } = new();

        /// <summary>
        /// 阵型数据
        /// </summary>
        public FormationSnapshot Formation { get; set; }

        /// <summary>
        /// 快照创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 快照版本
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// 防守胜率
        /// </summary>
        public float DefenseWinRate { get; set; }

        /// <summary>
        /// 防守次数
        /// </summary>
        public int DefenseCount { get; set; }

        public TeamSnapshot()
        {
            SnapshotId = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 单位快照
    /// </summary>
    [Serializable]
    public class UnitSnapshot
    {
        /// <summary>
        /// 单位ID
        /// </summary>
        public string UnitId { get; set; }

        /// <summary>
        /// 单位模板ID
        /// </summary>
        public string TemplateId { get; set; }

        /// <summary>
        /// 单位名称
        /// </summary>
        public string UnitName { get; set; }

        /// <summary>
        /// 等级
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 星级
        /// </summary>
        public int StarRank { get; set; }

        /// <summary>
        /// 战力
        /// </summary>
        public int Power { get; set; }

        /// <summary>
        /// 最大生命值
        /// </summary>
        public float MaxHealth { get; set; }

        /// <summary>
        /// 攻击力
        /// </summary>
        public float Attack { get; set; }

        /// <summary>
        /// 防御力
        /// </summary>
        public float Defense { get; set; }

        /// <summary>
        /// 攻击速度
        /// </summary>
        public float AttackSpeed { get; set; }

        /// <summary>
        /// 装备的技能ID列表
        /// </summary>
        public List<string> EquippedSkillIds { get; set; } = new();

        /// <summary>
        /// AI配置
        /// </summary>
        public AIConfigSnapshot AIConfig { get; set; }

        /// <summary>
        /// 在阵型中的位置
        /// </summary>
        public Vector2Int FormationPosition { get; set; }
    }

    /// <summary>
    /// 阵型快照
    /// </summary>
    [Serializable]
    public class FormationSnapshot
    {
        /// <summary>
        /// 阵型ID
        /// </summary>
        public string FormationId { get; set; }

        /// <summary>
        /// 单位位置映射
        /// </summary>
        public Dictionary<string, Vector2Int> UnitPositions { get; set; } = new();
    }

    /// <summary>
    /// AI配置快照
    /// </summary>
    [Serializable]
    public class AIConfigSnapshot
    {
        /// <summary>
        /// 目标优先级
        /// </summary>
        public int TargetPriority { get; set; }

        /// <summary>
        /// 技能策略
        /// </summary>
        public int SkillStrategy { get; set; }

        /// <summary>
        /// 激进度
        /// </summary>
        public float Aggression { get; set; }
    }
}
