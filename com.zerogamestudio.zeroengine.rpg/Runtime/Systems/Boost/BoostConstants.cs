namespace ZeroEngine.RPG.Systems
{
    /// <summary>
    /// BP 强化系统常量 - 八方旅人风格
    /// </summary>
    public static class BoostConstants
    {
        /// <summary>
        /// 默认最大 BP 点数
        /// </summary>
        public const int DEFAULT_MAX_BP = 5;

        /// <summary>
        /// 默认每回合恢复 BP
        /// </summary>
        public const int DEFAULT_BP_PER_TURN = 1;

        /// <summary>
        /// 单次最大消耗 BP
        /// </summary>
        public const int MAX_BOOST_LEVEL = 3;

        /// <summary>
        /// 每点 BP 的伤害加成倍率 (50%)
        /// </summary>
        public const float BOOST_MULTIPLIER_PER_POINT = 0.5f;

        /// <summary>
        /// 每点 BP 的治疗加成倍率 (50%)
        /// </summary>
        public const float BOOST_HEAL_MULTIPLIER_PER_POINT = 0.5f;

        /// <summary>
        /// 每点 BP 的攻击次数加成 (用于多段攻击)
        /// </summary>
        public const int BOOST_EXTRA_HITS_PER_POINT = 1;

        /// <summary>
        /// 计算 BP 强化后的伤害倍率
        /// </summary>
        /// <param name="boostLevel">BP 等级 (0-3)</param>
        /// <returns>伤害倍率 (1.0 - 2.5)</returns>
        public static float GetDamageMultiplier(int boostLevel)
        {
            return 1f + boostLevel * BOOST_MULTIPLIER_PER_POINT;
        }

        /// <summary>
        /// 计算 BP 强化后的治疗倍率
        /// </summary>
        /// <param name="boostLevel">BP 等级 (0-3)</param>
        /// <returns>治疗倍率 (1.0 - 2.5)</returns>
        public static float GetHealMultiplier(int boostLevel)
        {
            return 1f + boostLevel * BOOST_HEAL_MULTIPLIER_PER_POINT;
        }

        /// <summary>
        /// 计算 BP 强化后的攻击次数
        /// </summary>
        /// <param name="baseHits">基础攻击次数</param>
        /// <param name="boostLevel">BP 等级 (0-3)</param>
        /// <returns>总攻击次数</returns>
        public static int GetTotalHits(int baseHits, int boostLevel)
        {
            return baseHits + boostLevel * BOOST_EXTRA_HITS_PER_POINT;
        }
    }
}
