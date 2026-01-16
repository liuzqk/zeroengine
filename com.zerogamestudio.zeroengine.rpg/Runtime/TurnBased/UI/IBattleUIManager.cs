using System;
using System.Collections.Generic;

namespace ZeroEngine.RPG.TurnBased.UI
{
    /// <summary>
    /// 战斗 UI 管理器接口
    /// 协调战斗中的所有 UI 组件
    /// </summary>
    public interface IBattleUIManager
    {
        #region 显示/隐藏控制

        /// <summary>
        /// 显示战斗 UI
        /// </summary>
        void ShowBattleUI();

        /// <summary>
        /// 隐藏战斗 UI
        /// </summary>
        void HideBattleUI();

        #endregion

        #region 指令面板

        /// <summary>
        /// 显示指令面板
        /// </summary>
        /// <param name="actor">当前行动的战斗单位</param>
        void ShowCommandPanel(ITurnBasedCombatant actor);

        /// <summary>
        /// 隐藏指令面板
        /// </summary>
        void HideCommandPanel();

        #endregion

        #region 结算界面

        /// <summary>
        /// 显示战斗结算
        /// </summary>
        /// <param name="result">战斗结果</param>
        /// <param name="reward">奖励数据 (可为 null)</param>
        void ShowResult(BattleResult result, object reward);

        #endregion

        #region 事件

        /// <summary>
        /// 玩家选择行动时触发
        /// </summary>
        event Action<BattleAction> OnPlayerActionSelected;

        #endregion
    }
}
