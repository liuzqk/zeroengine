using System;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 仇恨系数配置 — 控制不同行为产生多少仇恨
    /// 可序列化，支持 ScriptableObject 或 Inspector 配置
    /// </summary>
    [Serializable]
    public class ThreatModifier
    {
        /// <summary>
        /// 伤害仇恨系数：实际仇恨 = 伤害值 × DamageMul
        /// </summary>
        public float DamageMul = 1.0f;

        /// <summary>
        /// 技能额外仇恨系数：技能命中额外增加 伤害 × SkillBonusMul
        /// </summary>
        public float SkillBonusMul = 0.5f;

        /// <summary>
        /// 治疗仇恨系数：治疗量 × HealMul（均摊给所有敌方单位）
        /// </summary>
        public float HealMul = 0.5f;

        /// <summary>
        /// 坦克嘲讽光环：每 tick 对所有敌人增加的固定仇恨
        /// </summary>
        public float TauntPerTick = 8f;

        /// <summary>
        /// 仇恨衰减率：每 tick 所有仇恨值 × DecayRate
        /// 0.95 表示每 tick 衰减 5%
        /// </summary>
        public float DecayRate = 0.95f;

        /// <summary>
        /// 死亡仇恨转移比例：死亡时将自身仇恨表的 TransferRatio 比例
        /// 转移给距离最近的同阵营单位
        /// </summary>
        public float TransferRatio = 0.5f;

        /// <summary>
        /// 仇恨清理阈值：低于此值的仇恨条目直接移除
        /// 避免仇恨表无限膨胀
        /// </summary>
        public float PruneThreshold = 0.1f;

        /// <summary>
        /// 默认配置（全部默认值）
        /// </summary>
        public static readonly ThreatModifier Default = new();
    }
}
