using System;

namespace ZeroEngine.RPG.TurnBased
{
    /// <summary>
    /// 回合制战斗事件定义
    /// </summary>
    public static class TurnBasedEvents
    {
        // 事件名称常量 (用于 EventManager)
        public const string BattleStarted = "TurnBased.BattleStarted";
        public const string BattleEnded = "TurnBased.BattleEnded";
        public const string TurnStarted = "TurnBased.TurnStarted";
        public const string TurnEnded = "TurnBased.TurnEnded";
        public const string PhaseChanged = "TurnBased.PhaseChanged";
        public const string UnitTurnStarted = "TurnBased.UnitTurnStarted";
        public const string UnitTurnEnded = "TurnBased.UnitTurnEnded";
        public const string ActionExecuted = "TurnBased.ActionExecuted";
        public const string ActionQueued = "TurnBased.ActionQueued";
    }

    /// <summary>
    /// 战斗开始事件参数
    /// </summary>
    public readonly struct BattleStartedEventArgs
    {
        public TurnBasedBattleContext Context { get; }

        public BattleStartedEventArgs(TurnBasedBattleContext context)
        {
            Context = context;
        }
    }

    /// <summary>
    /// 战斗结束事件参数
    /// </summary>
    public readonly struct BattleEndedEventArgs
    {
        public TurnBasedBattleContext Context { get; }
        public BattleResult Result { get; }
        public int TotalTurns { get; }

        public BattleEndedEventArgs(TurnBasedBattleContext context, BattleResult result)
        {
            Context = context;
            Result = result;
            TotalTurns = context.TurnCount;
        }
    }

    /// <summary>
    /// 回合开始/结束事件参数
    /// </summary>
    public readonly struct TurnEventArgs
    {
        public TurnBasedBattleContext Context { get; }
        public int TurnNumber { get; }

        public TurnEventArgs(TurnBasedBattleContext context)
        {
            Context = context;
            TurnNumber = context.TurnCount;
        }
    }

    /// <summary>
    /// 阶段变化事件参数
    /// </summary>
    public readonly struct PhaseChangedEventArgs
    {
        public BattlePhase OldPhase { get; }
        public BattlePhase NewPhase { get; }
        public TurnBasedBattleContext Context { get; }

        public PhaseChangedEventArgs(BattlePhase oldPhase, BattlePhase newPhase, TurnBasedBattleContext context)
        {
            OldPhase = oldPhase;
            NewPhase = newPhase;
            Context = context;
        }
    }

    /// <summary>
    /// 单位回合事件参数
    /// </summary>
    public readonly struct UnitTurnEventArgs
    {
        public ITurnBasedCombatant Combatant { get; }
        public TurnBasedBattleContext Context { get; }

        public UnitTurnEventArgs(ITurnBasedCombatant combatant, TurnBasedBattleContext context)
        {
            Combatant = combatant;
            Context = context;
        }
    }

    /// <summary>
    /// 行动执行事件参数
    /// </summary>
    public readonly struct ActionExecutedEventArgs
    {
        public BattleAction Action { get; }
        public TurnBasedBattleContext Context { get; }
        public bool WasSuccessful { get; }

        public ActionExecutedEventArgs(BattleAction action, TurnBasedBattleContext context, bool wasSuccessful)
        {
            Action = action;
            Context = context;
            WasSuccessful = wasSuccessful;
        }
    }
}
