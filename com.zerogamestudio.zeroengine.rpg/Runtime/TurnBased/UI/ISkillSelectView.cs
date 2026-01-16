using System;
using System.Collections.Generic;

namespace ZeroEngine.RPG.TurnBased.UI
{
    /// <summary>
    /// 技能选择面板接口
    /// 显示可用技能列表供玩家选择
    /// </summary>
    public interface ISkillSelectView
    {
        #region 显示控制

        /// <summary>
        /// 显示技能选择面板
        /// </summary>
        /// <param name="actor">当前行动的战斗单位</param>
        /// <param name="skillIds">可用技能 ID 列表</param>
        void Show(ITurnBasedCombatant actor, IEnumerable<string> skillIds);

        /// <summary>
        /// 隐藏技能选择面板
        /// </summary>
        void Hide();

        #endregion

        #region 状态

        /// <summary>
        /// 是否正在显示
        /// </summary>
        bool IsVisible { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 选择技能时触发
        /// </summary>
        event Action<string> OnSkillSelected;

        /// <summary>
        /// 取消选择时触发
        /// </summary>
        event Action OnCancel;

        #endregion
    }
}
