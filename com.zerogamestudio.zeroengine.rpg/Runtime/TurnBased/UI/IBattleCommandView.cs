using System;

namespace ZeroEngine.RPG.TurnBased.UI
{
    /// <summary>
    /// 战斗指令面板接口
    /// 玩家选择攻击/技能/防御/物品/逃跑的 UI
    /// </summary>
    public interface IBattleCommandView
    {
        #region 显示控制

        /// <summary>
        /// 显示指令面板
        /// </summary>
        /// <param name="actor">当前行动的战斗单位</param>
        void Show(ITurnBasedCombatant actor);

        /// <summary>
        /// 隐藏指令面板
        /// </summary>
        void Hide();

        #endregion

        #region Boost 控制

        /// <summary>
        /// 设置当前 Boost 等级
        /// </summary>
        /// <param name="level">Boost 等级 (0-3)</param>
        void SetBoostLevel(int level);

        /// <summary>
        /// 获取当前 Boost 等级
        /// </summary>
        int CurrentBoostLevel { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 选择攻击时触发
        /// </summary>
        event Action OnAttackSelected;

        /// <summary>
        /// 选择技能时触发 (需要进入技能选择)
        /// </summary>
        event Action OnSkillSelected;

        /// <summary>
        /// 选择防御时触发
        /// </summary>
        event Action OnDefendSelected;

        /// <summary>
        /// 选择物品时触发 (需要进入物品选择)
        /// </summary>
        event Action OnItemSelected;

        /// <summary>
        /// 选择逃跑时触发
        /// </summary>
        event Action OnEscapeSelected;

        /// <summary>
        /// Boost 等级变化时触发
        /// </summary>
        event Action<int> OnBoostChanged;

        #endregion
    }
}
