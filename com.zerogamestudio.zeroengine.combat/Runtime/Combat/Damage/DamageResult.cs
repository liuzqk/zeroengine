using UnityEngine;

namespace ZeroEngine.Combat
{
    /// <summary>
    /// 伤害结果 - 记录一次伤害计算的结果
    /// </summary>
    public readonly struct DamageResult
    {
        /// <summary>原始伤害数据</summary>
        public readonly DamageData OriginalDamage;

        /// <summary>最终伤害值（防御/抗性计算后）</summary>
        public readonly float FinalDamage;

        /// <summary>被吸收的伤害</summary>
        public readonly float AbsorbedDamage;

        /// <summary>被格挡的伤害</summary>
        public readonly float BlockedDamage;

        /// <summary>是否暴击</summary>
        public readonly bool IsCritical;

        /// <summary>是否闪避</summary>
        public readonly bool IsDodged;

        /// <summary>是否格挡</summary>
        public readonly bool IsBlocked;

        /// <summary>是否被完全吸收</summary>
        public readonly bool IsAbsorbed;

        /// <summary>是否免疫</summary>
        public readonly bool IsImmune;

        /// <summary>是否击杀</summary>
        public readonly bool IsKill;

        /// <summary>目标剩余生命值</summary>
        public readonly float TargetRemainingHealth;

        /// <summary>生命偷取量</summary>
        public readonly float LifestealAmount;

        /// <summary>结果类型</summary>
        public DamageResultType ResultType
        {
            get
            {
                if (IsImmune) return DamageResultType.Immune;
                if (IsDodged) return DamageResultType.Dodged;
                if (IsAbsorbed) return DamageResultType.Absorbed;
                if (IsBlocked && FinalDamage <= 0) return DamageResultType.Blocked;
                if (IsCritical) return DamageResultType.Critical;
                if (FinalDamage > 0) return DamageResultType.Normal;
                return DamageResultType.None;
            }
        }

        public DamageResult(
            DamageData originalDamage,
            float finalDamage,
            float absorbedDamage = 0f,
            float blockedDamage = 0f,
            bool isCritical = false,
            bool isDodged = false,
            bool isBlocked = false,
            bool isAbsorbed = false,
            bool isImmune = false,
            bool isKill = false,
            float targetRemainingHealth = 0f,
            float lifestealAmount = 0f)
        {
            OriginalDamage = originalDamage;
            FinalDamage = finalDamage;
            AbsorbedDamage = absorbedDamage;
            BlockedDamage = blockedDamage;
            IsCritical = isCritical;
            IsDodged = isDodged;
            IsBlocked = isBlocked;
            IsAbsorbed = isAbsorbed;
            IsImmune = isImmune;
            IsKill = isKill;
            TargetRemainingHealth = targetRemainingHealth;
            LifestealAmount = lifestealAmount;
        }

        /// <summary>
        /// 创建闪避结果
        /// </summary>
        public static DamageResult Dodged(DamageData originalDamage)
        {
            return new DamageResult(originalDamage, 0f, isDodged: true);
        }

        /// <summary>
        /// 创建免疫结果
        /// </summary>
        public static DamageResult Immune(DamageData originalDamage)
        {
            return new DamageResult(originalDamage, 0f, isImmune: true);
        }

        /// <summary>
        /// 创建完全格挡结果
        /// </summary>
        public static DamageResult FullyBlocked(DamageData originalDamage, float blockedAmount)
        {
            return new DamageResult(originalDamage, 0f, blockedDamage: blockedAmount, isBlocked: true);
        }

        public override string ToString()
        {
            return $"[DamageResult] Type={ResultType}, Final={FinalDamage:F1}, Crit={IsCritical}, Kill={IsKill}";
        }
    }

    /// <summary>
    /// 伤害结果类型
    /// </summary>
    public enum DamageResultType
    {
        /// <summary>无伤害</summary>
        None,
        /// <summary>正常伤害</summary>
        Normal,
        /// <summary>暴击伤害</summary>
        Critical,
        /// <summary>闪避</summary>
        Dodged,
        /// <summary>格挡</summary>
        Blocked,
        /// <summary>吸收</summary>
        Absorbed,
        /// <summary>免疫</summary>
        Immune
    }
}
