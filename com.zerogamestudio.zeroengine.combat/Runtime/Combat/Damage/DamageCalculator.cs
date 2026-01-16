using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 伤害计算器 - 处理伤害计算管线
    /// </summary>
    public class DamageCalculator
    {
        private static DamageCalculator _default;

        /// <summary>
        /// 默认伤害计算器实例
        /// </summary>
        public static DamageCalculator Default => _default ??= new DamageCalculator();

        private readonly List<IDamageProcessor> _processors = new();
        private readonly DamageCalculationContext _context = new();

        /// <summary>
        /// 伤害计算公式配置
        /// </summary>
        public DamageFormulaConfig FormulaConfig { get; set; } = new();

        /// <summary>
        /// 注册伤害处理器
        /// </summary>
        public void RegisterProcessor(IDamageProcessor processor)
        {
            _processors.Add(processor);
            _processors.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        /// <summary>
        /// 移除伤害处理器
        /// </summary>
        public void UnregisterProcessor(IDamageProcessor processor)
        {
            _processors.Remove(processor);
        }

        /// <summary>
        /// 计算伤害
        /// </summary>
        /// <param name="damage">伤害数据</param>
        /// <param name="target">目标</param>
        /// <param name="attackerStatGetter">攻击者属性获取器</param>
        /// <param name="defenderStatGetter">防御者属性获取器</param>
        /// <returns>伤害结果</returns>
        public DamageResult Calculate(
            DamageData damage,
            ICombatant target,
            Func<string, float> attackerStatGetter = null,
            Func<string, float> defenderStatGetter = null)
        {
            if (target == null || !target.IsAlive)
            {
                return new DamageResult(damage, 0f);
            }

            // 重置上下文
            _context.Reset();
            _context.GetAttackerStat = attackerStatGetter ?? (_ => 0f);
            _context.GetDefenderStat = defenderStatGetter ?? (_ => 0f);

            // 通过处理器管线处理伤害
            var processedDamage = damage;
            foreach (var processor in _processors)
            {
                processedDamage = processor.ProcessDamage(processedDamage, target, _context);
            }

            // 检查免疫
            if (_context.IsImmune)
            {
                return DamageResult.Immune(damage);
            }

            // 计算闪避
            if (!_context.DodgeCalculated && !damage.HasFlag(DamageFlags.IgnoreDodge))
            {
                float dodgeChance = _context.GetDefenderStat(FormulaConfig.DodgeStatName);
                if (UnityEngine.Random.value < dodgeChance)
                {
                    return DamageResult.Dodged(damage);
                }
            }
            else if (_context.IsDodged)
            {
                return DamageResult.Dodged(damage);
            }

            // 计算暴击
            bool isCritical = _context.IsCritical;
            if (!_context.CritCalculated)
            {
                float critChance = processedDamage.CritChance + _context.GetAttackerStat(FormulaConfig.CritChanceStatName);
                isCritical = UnityEngine.Random.value < critChance;
            }

            // 计算基础伤害
            float finalDamage = processedDamage.BaseDamage;

            // 应用伤害加成
            finalDamage += _context.FlatDamageBonus;
            finalDamage *= _context.DamageMultiplier;

            // 应用暴击
            if (isCritical)
            {
                float critMult = processedDamage.CritMultiplier + _context.GetAttackerStat(FormulaConfig.CritDamageStatName);
                finalDamage *= Mathf.Max(1f, critMult);
            }

            // 应用防御减免
            if (!damage.HasFlag(DamageFlags.IgnoreArmor))
            {
                if (damage.HasDamageType(DamageType.Physical))
                {
                    float armor = _context.GetDefenderStat(FormulaConfig.ArmorStatName);
                    float armorReduction = CalculateArmorReduction(armor);
                    _context.ArmorReduction = armorReduction;
                    finalDamage *= (1f - armorReduction);
                }

                if (damage.HasDamageType(DamageType.Magical) || damage.HasDamageType(DamageType.AllElemental))
                {
                    float resistance = _context.GetDefenderStat(FormulaConfig.MagicResistStatName);
                    float resistReduction = CalculateResistanceReduction(resistance);
                    _context.ResistanceReduction = resistReduction;
                    finalDamage *= (1f - resistReduction);
                }
            }

            // 应用伤害减免
            finalDamage *= _context.DamageReduction;

            // 处理伤害吸收
            float absorbedDamage = 0f;
            if (_context.AbsorbAmount > 0 && !damage.HasFlag(DamageFlags.CannotAbsorb))
            {
                absorbedDamage = Mathf.Min(finalDamage, _context.AbsorbAmount);
                finalDamage -= absorbedDamage;
            }

            // 确保伤害不为负
            finalDamage = Mathf.Max(0f, finalDamage);

            // 计算生命偷取
            float lifestealAmount = 0f;
            if (damage.HasFlag(DamageFlags.Lifesteal) && damage.Source != null)
            {
                float lifestealRate = _context.GetAttackerStat(FormulaConfig.LifestealStatName);
                lifestealAmount = finalDamage * lifestealRate;
            }

            return new DamageResult(
                damage,
                finalDamage,
                absorbedDamage: absorbedDamage,
                isCritical: isCritical,
                isAbsorbed: absorbedDamage > 0 && finalDamage <= 0,
                lifestealAmount: lifestealAmount
            );
        }

        /// <summary>
        /// 计算护甲减免
        /// 默认公式: reduction = armor / (armor + 100)
        /// </summary>
        protected virtual float CalculateArmorReduction(float armor)
        {
            if (armor <= 0) return 0f;
            return armor / (armor + FormulaConfig.ArmorConstant);
        }

        /// <summary>
        /// 计算抗性减免
        /// 默认公式: reduction = resistance / (resistance + 100)
        /// </summary>
        protected virtual float CalculateResistanceReduction(float resistance)
        {
            if (resistance <= 0) return 0f;
            return resistance / (resistance + FormulaConfig.ResistanceConstant);
        }
    }

    /// <summary>
    /// 伤害公式配置
    /// </summary>
    [Serializable]
    public class DamageFormulaConfig
    {
        /// <summary>护甲属性名</summary>
        public string ArmorStatName = "Armor";

        /// <summary>魔抗属性名</summary>
        public string MagicResistStatName = "MagicResist";

        /// <summary>闪避率属性名</summary>
        public string DodgeStatName = "DodgeChance";

        /// <summary>暴击率属性名</summary>
        public string CritChanceStatName = "CritChance";

        /// <summary>暴击伤害属性名</summary>
        public string CritDamageStatName = "CritDamage";

        /// <summary>生命偷取属性名</summary>
        public string LifestealStatName = "Lifesteal";

        /// <summary>护甲常数（用于减免计算）</summary>
        public float ArmorConstant = 100f;

        /// <summary>抗性常数（用于减免计算）</summary>
        public float ResistanceConstant = 100f;
    }
}
