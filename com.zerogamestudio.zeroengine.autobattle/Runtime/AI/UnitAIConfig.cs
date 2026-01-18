using System;

namespace ZeroEngine.AutoBattle.AI
{
    /// <summary>
    /// 单位 AI 配置
    /// </summary>
    [Serializable]
    public class UnitAIConfig
    {
        /// <summary>
        /// 目标选择优先级
        /// </summary>
        public TargetPriority TargetPriority { get; set; } = TargetPriority.Nearest;

        /// <summary>
        /// 技能使用策略
        /// </summary>
        public SkillUseStrategy SkillStrategy { get; set; } = SkillUseStrategy.WhenReady;

        /// <summary>
        /// 移动倾向
        /// </summary>
        public MovementTendency MovementTendency { get; set; } = MovementTendency.Hold;

        /// <summary>
        /// 激进度 (0-1)
        /// </summary>
        public float Aggression { get; set; } = 0.5f;

        /// <summary>
        /// 自保阈值（血量百分比低于此值时触发自保行为）
        /// </summary>
        public float SelfPreserveThreshold { get; set; } = 0.3f;

        /// <summary>
        /// 配合度 (0-1)，影响是否配合队友集火
        /// </summary>
        public float Cooperation { get; set; } = 0.5f;
    }

    /// <summary>
    /// 目标选择优先级
    /// </summary>
    public enum TargetPriority
    {
        /// <summary>
        /// 最近的敌人
        /// </summary>
        Nearest,

        /// <summary>
        /// 最远的敌人
        /// </summary>
        Farthest,

        /// <summary>
        /// 血量最低的敌人
        /// </summary>
        LowestHealth,

        /// <summary>
        /// 血量最高的敌人
        /// </summary>
        HighestHealth,

        /// <summary>
        /// 后排敌人
        /// </summary>
        BackRow,

        /// <summary>
        /// 前排敌人
        /// </summary>
        FrontRow,

        /// <summary>
        /// 随机目标
        /// </summary>
        Random
    }

    /// <summary>
    /// 技能使用策略
    /// </summary>
    public enum SkillUseStrategy
    {
        /// <summary>
        /// 冷却好就用
        /// </summary>
        WhenReady,

        /// <summary>
        /// 保留到关键时刻
        /// </summary>
        Conservative,

        /// <summary>
        /// 按条件触发
        /// </summary>
        Conditional,

        /// <summary>
        /// 按固定顺序使用
        /// </summary>
        Sequential
    }

    /// <summary>
    /// 移动倾向
    /// </summary>
    public enum MovementTendency
    {
        /// <summary>
        /// 固定不动
        /// </summary>
        Hold,

        /// <summary>
        /// 靠近敌人
        /// </summary>
        Aggressive,

        /// <summary>
        /// 保持距离
        /// </summary>
        Kite,

        /// <summary>
        /// 跟随坦克
        /// </summary>
        FollowTank,

        /// <summary>
        /// 躲避危险
        /// </summary>
        Evasive
    }
}
