// ============================================================================
// ElementType.cs
// 五行属性系统
// 创建于: 2026-01-09
// ============================================================================

using System;

namespace ZeroEngine.Character.MartialArts
{
    /// <summary>
    /// 五行属性
    /// </summary>
    [Flags]
    public enum ElementType
    {
        /// <summary>无属性</summary>
        None = 0,

        /// <summary>金 - 锐利, 肃杀</summary>
        Metal = 1 << 0,

        /// <summary>木 - 生长, 柔韧</summary>
        Wood = 1 << 1,

        /// <summary>水 - 流动, 寒冷</summary>
        Water = 1 << 2,

        /// <summary>火 - 炎热, 爆发</summary>
        Fire = 1 << 3,

        /// <summary>土 - 厚重, 稳固</summary>
        Earth = 1 << 4,

        // ===== 仙侠扩展属性 =====

        /// <summary>阴 - 阴柔, 幽冥</summary>
        Yin = 1 << 5,

        /// <summary>阳 - 阳刚, 光明</summary>
        Yang = 1 << 6,

        /// <summary>雷 - 迅捷, 毁灭</summary>
        Thunder = 1 << 7,

        /// <summary>风 - 轻灵, 锋锐</summary>
        Wind = 1 << 8,

        // ===== 组合 =====

        /// <summary>五行全属性</summary>
        AllFiveElements = Metal | Wood | Water | Fire | Earth,

        /// <summary>阴阳</summary>
        YinYang = Yin | Yang
    }

    /// <summary>
    /// 五行相生相克关系
    /// </summary>
    public static class ElementRelation
    {
        /// <summary>
        /// 获取相生属性 (我生)
        /// 金生水, 水生木, 木生火, 火生土, 土生金
        /// </summary>
        public static ElementType GetGenerating(ElementType element)
        {
            return element switch
            {
                ElementType.Metal => ElementType.Water,
                ElementType.Water => ElementType.Wood,
                ElementType.Wood => ElementType.Fire,
                ElementType.Fire => ElementType.Earth,
                ElementType.Earth => ElementType.Metal,
                _ => ElementType.None
            };
        }

        /// <summary>
        /// 获取被生属性 (生我)
        /// </summary>
        public static ElementType GetGeneratedBy(ElementType element)
        {
            return element switch
            {
                ElementType.Metal => ElementType.Earth,
                ElementType.Water => ElementType.Metal,
                ElementType.Wood => ElementType.Water,
                ElementType.Fire => ElementType.Wood,
                ElementType.Earth => ElementType.Fire,
                _ => ElementType.None
            };
        }

        /// <summary>
        /// 获取相克属性 (我克)
        /// 金克木, 木克土, 土克水, 水克火, 火克金
        /// </summary>
        public static ElementType GetOvercoming(ElementType element)
        {
            return element switch
            {
                ElementType.Metal => ElementType.Wood,
                ElementType.Wood => ElementType.Earth,
                ElementType.Earth => ElementType.Water,
                ElementType.Water => ElementType.Fire,
                ElementType.Fire => ElementType.Metal,
                _ => ElementType.None
            };
        }

        /// <summary>
        /// 获取被克属性 (克我)
        /// </summary>
        public static ElementType GetOvercomeBy(ElementType element)
        {
            return element switch
            {
                ElementType.Metal => ElementType.Fire,
                ElementType.Wood => ElementType.Metal,
                ElementType.Earth => ElementType.Wood,
                ElementType.Water => ElementType.Earth,
                ElementType.Fire => ElementType.Water,
                _ => ElementType.None
            };
        }

        /// <summary>
        /// 计算属性克制伤害倍率
        /// </summary>
        /// <param name="attacker">攻击方属性</param>
        /// <param name="defender">防御方属性</param>
        /// <returns>伤害倍率 (1.0 = 无克制)</returns>
        public static float CalculateDamageMultiplier(ElementType attacker, ElementType defender)
        {
            // 无属性
            if (attacker == ElementType.None || defender == ElementType.None)
                return 1.0f;

            // 相克 (我克敌) - 增伤
            if (GetOvercoming(attacker) == defender)
                return 1.3f; // +30% 伤害

            // 被克 (敌克我) - 减伤
            if (GetOvercomeBy(attacker) == defender)
                return 0.7f; // -30% 伤害

            // 相生 (我生敌) - 轻微减伤
            if (GetGenerating(attacker) == defender)
                return 0.9f; // -10% 伤害

            // 被生 (敌生我) - 轻微增伤
            if (GetGeneratedBy(attacker) == defender)
                return 1.1f; // +10% 伤害

            // 阴阳相克
            if ((attacker == ElementType.Yin && defender == ElementType.Yang) ||
                (attacker == ElementType.Yang && defender == ElementType.Yin))
                return 1.2f; // +20% 伤害

            return 1.0f;
        }

        /// <summary>
        /// 检查两个属性是否相克
        /// </summary>
        public static bool AreOpposing(ElementType a, ElementType b)
        {
            return GetOvercoming(a) == b || GetOvercomeBy(a) == b;
        }

        /// <summary>
        /// 检查两个属性是否相生
        /// </summary>
        public static bool AreGenerating(ElementType a, ElementType b)
        {
            return GetGenerating(a) == b || GetGeneratedBy(a) == b;
        }

        /// <summary>
        /// 获取属性的中文名称
        /// </summary>
        public static string GetElementName(ElementType element)
        {
            return element switch
            {
                ElementType.Metal => "金",
                ElementType.Wood => "木",
                ElementType.Water => "水",
                ElementType.Fire => "火",
                ElementType.Earth => "土",
                ElementType.Yin => "阴",
                ElementType.Yang => "阳",
                ElementType.Thunder => "雷",
                ElementType.Wind => "风",
                _ => "无"
            };
        }
    }
}
