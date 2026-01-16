using System;
using System.Collections.Generic;

namespace ZeroEngine.RPG.TurnBased.UI
{
    /// <summary>
    /// 目标选择面板接口
    /// 显示可选目标供玩家选择
    /// </summary>
    public interface ITargetSelectView
    {
        #region 显示控制

        /// <summary>
        /// 显示目标选择面板
        /// </summary>
        /// <param name="targets">可选目标列表</param>
        /// <param name="allowMultiple">是否允许多选</param>
        void Show(IEnumerable<ITurnBasedCombatant> targets, bool allowMultiple = false);

        /// <summary>
        /// 隐藏目标选择面板
        /// </summary>
        void Hide();

        #endregion

        #region 状态

        /// <summary>
        /// 是否正在显示
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// 当前选中的目标
        /// </summary>
        IReadOnlyList<ITurnBasedCombatant> SelectedTargets { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 确认目标选择时触发
        /// </summary>
        event Action<IReadOnlyList<ITurnBasedCombatant>> OnTargetConfirmed;

        /// <summary>
        /// 取消选择时触发
        /// </summary>
        event Action OnCancel;

        #endregion
    }
}
