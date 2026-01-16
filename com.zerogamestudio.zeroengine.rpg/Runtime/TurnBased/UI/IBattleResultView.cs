using System;
using System.Threading.Tasks;

namespace ZeroEngine.RPG.TurnBased.UI
{
    /// <summary>
    /// 战斗结算界面接口
    /// 显示战斗结果和奖励
    /// </summary>
    public interface IBattleResultView
    {
        #region 显示控制

        /// <summary>
        /// 显示胜利结算
        /// </summary>
        /// <param name="reward">奖励数据</param>
        /// <returns>异步任务 (动画完成后返回)</returns>
        Task ShowVictoryAsync(object reward);

        /// <summary>
        /// 显示失败结算
        /// </summary>
        /// <returns>异步任务 (动画完成后返回)</returns>
        Task ShowDefeatAsync();

        /// <summary>
        /// 显示逃跑结算
        /// </summary>
        /// <returns>异步任务 (动画完成后返回)</returns>
        Task ShowEscapeAsync();

        /// <summary>
        /// 隐藏结算界面
        /// </summary>
        void Hide();

        #endregion

        #region 状态

        /// <summary>
        /// 是否正在显示
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// 动画是否已完成
        /// </summary>
        bool IsAnimationComplete { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 用户确认结算时触发
        /// </summary>
        event Action OnConfirm;

        #endregion
    }
}
