using System;
using System.Collections.Generic;

namespace ZeroEngine.RPG.TurnBased.UI
{
    /// <summary>
    /// 战斗 UI 事件定义
    /// </summary>
    public static class BattleUIEvents
    {
        #region 事件名称常量

        /// <summary>
        /// 指令面板打开
        /// </summary>
        public const string CommandPanelOpened = "BattleUI.CommandPanel.Opened";

        /// <summary>
        /// 指令面板关闭
        /// </summary>
        public const string CommandPanelClosed = "BattleUI.CommandPanel.Closed";

        /// <summary>
        /// 技能选择面板打开
        /// </summary>
        public const string SkillSelectOpened = "BattleUI.SkillSelect.Opened";

        /// <summary>
        /// 技能选择面板关闭
        /// </summary>
        public const string SkillSelectClosed = "BattleUI.SkillSelect.Closed";

        /// <summary>
        /// 目标选择面板打开
        /// </summary>
        public const string TargetSelectOpened = "BattleUI.TargetSelect.Opened";

        /// <summary>
        /// 目标选择面板关闭
        /// </summary>
        public const string TargetSelectClosed = "BattleUI.TargetSelect.Closed";

        /// <summary>
        /// 结算界面打开
        /// </summary>
        public const string ResultViewOpened = "BattleUI.Result.Opened";

        /// <summary>
        /// 结算界面关闭
        /// </summary>
        public const string ResultViewClosed = "BattleUI.Result.Closed";

        #endregion
    }

    /// <summary>
    /// 战斗 UI 状态枚举
    /// </summary>
    public enum BattleUIState
    {
        /// <summary>
        /// 隐藏状态
        /// </summary>
        Hidden,

        /// <summary>
        /// 主指令面板
        /// </summary>
        CommandPanel,

        /// <summary>
        /// 技能选择
        /// </summary>
        SkillSelect,

        /// <summary>
        /// 目标选择
        /// </summary>
        TargetSelect,

        /// <summary>
        /// 物品选择
        /// </summary>
        ItemSelect,

        /// <summary>
        /// 结算界面
        /// </summary>
        Result
    }

    /// <summary>
    /// 战斗结果枚举
    /// </summary>
    public enum BattleResult
    {
        /// <summary>
        /// 胜利
        /// </summary>
        Victory,

        /// <summary>
        /// 失败
        /// </summary>
        Defeat,

        /// <summary>
        /// 逃跑
        /// </summary>
        Escape
    }

    /// <summary>
    /// 指令类型枚举
    /// </summary>
    public enum CommandType
    {
        /// <summary>
        /// 普通攻击
        /// </summary>
        Attack,

        /// <summary>
        /// 技能
        /// </summary>
        Skill,

        /// <summary>
        /// 防御
        /// </summary>
        Defend,

        /// <summary>
        /// 物品
        /// </summary>
        Item,

        /// <summary>
        /// 逃跑
        /// </summary>
        Escape
    }

    /// <summary>
    /// 技能选择参数
    /// </summary>
    public readonly struct SkillSelectArgs
    {
        /// <summary>
        /// 技能 ID
        /// </summary>
        public readonly string SkillId;

        /// <summary>
        /// 技能名称
        /// </summary>
        public readonly string SkillName;

        /// <summary>
        /// MP 消耗
        /// </summary>
        public readonly int MpCost;

        /// <summary>
        /// 是否可用
        /// </summary>
        public readonly bool IsUsable;

        /// <summary>
        /// 冷却回合数 (0 表示无冷却)
        /// </summary>
        public readonly int Cooldown;

        public SkillSelectArgs(string skillId, string skillName, int mpCost, bool isUsable, int cooldown = 0)
        {
            SkillId = skillId;
            SkillName = skillName;
            MpCost = mpCost;
            IsUsable = isUsable;
            Cooldown = cooldown;
        }
    }

    /// <summary>
    /// 目标选择参数
    /// </summary>
    public readonly struct TargetSelectArgs
    {
        /// <summary>
        /// 目标单位
        /// </summary>
        public readonly ITurnBasedCombatant Target;

        /// <summary>
        /// 是否是敌方
        /// </summary>
        public readonly bool IsEnemy;

        /// <summary>
        /// HP 百分比
        /// </summary>
        public readonly float HpPercent;

        /// <summary>
        /// 盾值 (破盾系统)
        /// </summary>
        public readonly int ShieldPoints;

        /// <summary>
        /// 是否已破盾
        /// </summary>
        public readonly bool IsBroken;

        public TargetSelectArgs(ITurnBasedCombatant target, bool isEnemy, float hpPercent, int shieldPoints = 0, bool isBroken = false)
        {
            Target = target;
            IsEnemy = isEnemy;
            HpPercent = hpPercent;
            ShieldPoints = shieldPoints;
            IsBroken = isBroken;
        }
    }
}
