using System;

namespace ZeroEngine.RPG.Systems
{
    /// <summary>
    /// BP 强化系统接口 - 八方旅人风格
    /// </summary>
    public interface IBoostable
    {
        /// <summary>
        /// 当前 BP 点数
        /// </summary>
        int CurrentBP { get; }

        /// <summary>
        /// 最大 BP 点数 (默认 5)
        /// </summary>
        int MaxBP { get; }

        /// <summary>
        /// 每回合恢复的 BP (默认 1)
        /// </summary>
        int BPPerTurn { get; }

        /// <summary>
        /// 是否可以使用 BP
        /// </summary>
        bool CanBoost { get; }

        /// <summary>
        /// 消耗指定数量的 BP
        /// </summary>
        /// <param name="amount">消耗数量 (1-3)</param>
        /// <returns>是否成功消耗</returns>
        bool ConsumeBP(int amount);

        /// <summary>
        /// 恢复 BP
        /// </summary>
        /// <param name="amount">恢复数量</param>
        void RecoverBP(int amount);

        /// <summary>
        /// 回合开始时的 BP 恢复
        /// </summary>
        void OnTurnStartBPRecovery();

        /// <summary>
        /// BP 变化事件 (oldValue, newValue)
        /// </summary>
        event Action<int, int> OnBPChanged;
    }
}
