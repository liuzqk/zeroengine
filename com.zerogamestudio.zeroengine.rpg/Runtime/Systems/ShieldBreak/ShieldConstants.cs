namespace ZeroEngine.RPG.Systems
{
    /// <summary>
    /// 护盾破击系统常量 - 八方旅人风格
    /// </summary>
    public static class ShieldConstants
    {
        /// <summary>
        /// 默认护盾点数
        /// </summary>
        public const int DEFAULT_SHIELD_POINTS = 3;

        /// <summary>
        /// 最大护盾点数
        /// </summary>
        public const int MAX_SHIELD_POINTS = 99;

        /// <summary>
        /// 破盾后恢复回合数
        /// </summary>
        public const int BREAK_RECOVERY_TURNS = 1;

        /// <summary>
        /// 破盾状态伤害加成倍率 (50%)
        /// </summary>
        public const float BREAK_DAMAGE_MULTIPLIER = 1.5f;

        /// <summary>
        /// 命中弱点伤害加成倍率 (20%)
        /// </summary>
        public const float WEAKNESS_DAMAGE_MULTIPLIER = 1.2f;

        /// <summary>
        /// 命中弱点时减少的护盾点数
        /// </summary>
        public const int SHIELD_DAMAGE_PER_WEAKNESS_HIT = 1;

        /// <summary>
        /// 非弱点攻击时减少的护盾点数 (通常为 0)
        /// </summary>
        public const int SHIELD_DAMAGE_PER_NORMAL_HIT = 0;

        /// <summary>
        /// 计算破盾状态下的伤害
        /// </summary>
        /// <param name="baseDamage">基础伤害</param>
        /// <returns>破盾加成后的伤害</returns>
        public static float GetBreakDamage(float baseDamage)
        {
            return baseDamage * BREAK_DAMAGE_MULTIPLIER;
        }

        /// <summary>
        /// 计算弱点命中的伤害
        /// </summary>
        /// <param name="baseDamage">基础伤害</param>
        /// <returns>弱点加成后的伤害</returns>
        public static float GetWeaknessDamage(float baseDamage)
        {
            return baseDamage * WEAKNESS_DAMAGE_MULTIPLIER;
        }

        /// <summary>
        /// 计算弱点+破盾双重加成伤害
        /// </summary>
        /// <param name="baseDamage">基础伤害</param>
        /// <returns>双重加成后的伤害</returns>
        public static float GetWeaknessBreakDamage(float baseDamage)
        {
            return baseDamage * WEAKNESS_DAMAGE_MULTIPLIER * BREAK_DAMAGE_MULTIPLIER;
        }
    }
}
