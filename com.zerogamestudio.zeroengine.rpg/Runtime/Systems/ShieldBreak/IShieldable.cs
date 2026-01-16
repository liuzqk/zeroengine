using System;
using System.Collections.Generic;

namespace ZeroEngine.RPG.Systems
{
    /// <summary>
    /// 可破盾单位接口 - 八方旅人风格
    /// </summary>
    public interface IShieldable
    {
        /// <summary>
        /// 当前护盾点数
        /// </summary>
        int ShieldPoints { get; }

        /// <summary>
        /// 最大护盾点数
        /// </summary>
        int MaxShield { get; }

        /// <summary>
        /// 弱点列表
        /// </summary>
        IReadOnlyCollection<WeaknessType> Weaknesses { get; }

        /// <summary>
        /// 是否处于破盾状态
        /// </summary>
        bool IsBroken { get; }

        /// <summary>
        /// 破盾状态剩余回合数
        /// </summary>
        int BrokenTurnsRemaining { get; }

        /// <summary>
        /// 检查是否对指定弱点类型有效
        /// </summary>
        /// <param name="attackType">攻击类型</param>
        /// <returns>是否命中弱点</returns>
        bool CheckWeakness(WeaknessType attackType);

        /// <summary>
        /// 对护盾造成伤害
        /// </summary>
        /// <param name="amount">伤害量 (通常为 1)</param>
        /// <returns>是否触发破盾</returns>
        bool TakeShieldDamage(int amount);

        /// <summary>
        /// 触发破盾
        /// </summary>
        void Break();

        /// <summary>
        /// 从破盾状态恢复
        /// </summary>
        void RecoverFromBreak();

        /// <summary>
        /// 回合结束时处理破盾恢复
        /// </summary>
        void OnTurnEndShieldRecovery();

        /// <summary>
        /// 护盾点数变化事件 (oldValue, newValue)
        /// </summary>
        event Action<int, int> OnShieldChanged;

        /// <summary>
        /// 破盾事件
        /// </summary>
        event Action OnBroken;

        /// <summary>
        /// 从破盾恢复事件
        /// </summary>
        event Action OnRecovered;

        /// <summary>
        /// 弱点命中事件 (attackType, shieldDamage)
        /// </summary>
        event Action<WeaknessType, int> OnWeaknessHit;
    }
}
