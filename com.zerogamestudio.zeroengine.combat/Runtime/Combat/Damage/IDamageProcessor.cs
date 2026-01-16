namespace ZeroEngine.Combat
{
    /// <summary>
    /// 伤害处理器接口 - 用于自定义伤害计算管线
    /// </summary>
    public interface IDamageProcessor
    {
        /// <summary>
        /// 处理器优先级（数值越小越先执行）
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 处理伤害数据
        /// </summary>
        /// <param name="damage">当前伤害数据</param>
        /// <param name="target">目标</param>
        /// <param name="context">计算上下文</param>
        /// <returns>处理后的伤害数据</returns>
        DamageData ProcessDamage(DamageData damage, ICombatant target, DamageCalculationContext context);
    }

    /// <summary>
    /// 伤害计算上下文 - 在计算管线中传递的中间数据
    /// </summary>
    public class DamageCalculationContext
    {
        /// <summary>攻击者属性值获取委托</summary>
        public System.Func<string, float> GetAttackerStat;

        /// <summary>防御者属性值获取委托</summary>
        public System.Func<string, float> GetDefenderStat;

        /// <summary>是否已计算暴击</summary>
        public bool CritCalculated;

        /// <summary>是否暴击</summary>
        public bool IsCritical;

        /// <summary>暴击倍率</summary>
        public float CritMultiplier = 2f;

        /// <summary>闪避计算结果</summary>
        public bool DodgeCalculated;

        /// <summary>是否闪避</summary>
        public bool IsDodged;

        /// <summary>防御力减免</summary>
        public float ArmorReduction;

        /// <summary>抗性减免</summary>
        public float ResistanceReduction;

        /// <summary>伤害增幅（乘法）</summary>
        public float DamageMultiplier = 1f;

        /// <summary>伤害加成（加法）</summary>
        public float FlatDamageBonus;

        /// <summary>伤害减免（乘法）</summary>
        public float DamageReduction = 1f;

        /// <summary>伤害吸收量</summary>
        public float AbsorbAmount;

        /// <summary>是否免疫此伤害</summary>
        public bool IsImmune;

        /// <summary>重置上下文</summary>
        public void Reset()
        {
            GetAttackerStat = null;
            GetDefenderStat = null;
            CritCalculated = false;
            IsCritical = false;
            CritMultiplier = 2f;
            DodgeCalculated = false;
            IsDodged = false;
            ArmorReduction = 0f;
            ResistanceReduction = 0f;
            DamageMultiplier = 1f;
            FlatDamageBonus = 0f;
            DamageReduction = 1f;
            AbsorbAmount = 0f;
            IsImmune = false;
        }
    }

    /// <summary>
    /// 伤害处理器基类 - 提供默认实现
    /// </summary>
    public abstract class DamageProcessorBase : IDamageProcessor
    {
        public virtual int Priority => 0;

        public abstract DamageData ProcessDamage(DamageData damage, ICombatant target, DamageCalculationContext context);
    }
}
