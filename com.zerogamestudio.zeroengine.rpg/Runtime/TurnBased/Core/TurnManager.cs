using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZeroEngine.Core;

namespace ZeroEngine.RPG.TurnBased
{
    /// <summary>
    /// 回合管理器 - 管理回合制战斗的核心流程
    /// </summary>
    public class TurnManager : MonoSingleton<TurnManager>
    {
        #region 配置

        [Header("配置")]
        [SerializeField, Tooltip("行动顺序计算策略类型")]
        private TurnOrderCalculatorType _orderCalculatorType = TurnOrderCalculatorType.SpeedBased;

        [SerializeField, Tooltip("是否自动处理敌人AI")]
        private bool _autoProcessEnemyAI = true;

        [SerializeField, Tooltip("行动之间的延迟(秒)")]
        private float _actionDelay = 0.5f;

        #endregion

        #region 状态

        /// <summary>
        /// 战斗上下文
        /// </summary>
        public TurnBasedBattleContext Context { get; private set; } = new();

        /// <summary>
        /// 行动顺序计算器
        /// </summary>
        public ITurnOrderCalculator OrderCalculator { get; set; }

        /// <summary>
        /// 当前战斗阶段
        /// </summary>
        public BattlePhase CurrentPhase => Context.CurrentPhase;

        /// <summary>
        /// 当前回合数
        /// </summary>
        public int TurnCount => Context.TurnCount;

        /// <summary>
        /// 当前行动单位
        /// </summary>
        public ITurnBasedCombatant CurrentActor => Context.CurrentActor;

        /// <summary>
        /// 战斗是否进行中
        /// </summary>
        public bool IsInBattle => Context.IsInProgress;

        /// <summary>
        /// 待执行的行动 (玩家输入后设置)
        /// </summary>
        private BattleAction _pendingAction;

        #endregion

        #region 事件

        /// <summary>
        /// 战斗开始事件
        /// </summary>
        public event Action<BattleStartedEventArgs> OnBattleStarted;

        /// <summary>
        /// 战斗结束事件
        /// </summary>
        public event Action<BattleEndedEventArgs> OnBattleEnded;

        /// <summary>
        /// 回合开始事件
        /// </summary>
        public event Action<TurnEventArgs> OnTurnStarted;

        /// <summary>
        /// 回合结束事件
        /// </summary>
        public event Action<TurnEventArgs> OnTurnEnded;

        /// <summary>
        /// 阶段变化事件
        /// </summary>
        public event Action<PhaseChangedEventArgs> OnPhaseChanged;

        /// <summary>
        /// 单位回合开始事件
        /// </summary>
        public event Action<UnitTurnEventArgs> OnUnitTurnStarted;

        /// <summary>
        /// 单位回合结束事件
        /// </summary>
        public event Action<UnitTurnEventArgs> OnUnitTurnEnded;

        /// <summary>
        /// 行动执行事件
        /// </summary>
        public event Action<ActionExecutedEventArgs> OnActionExecuted;

        /// <summary>
        /// 等待玩家输入事件
        /// </summary>
        public event Action<ITurnBasedCombatant> OnWaitingForPlayerInput;

        #endregion

        #region Unity 生命周期

        protected override void Awake()
        {
            base.Awake();
            InitializeOrderCalculator();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartBattle(IEnumerable<ITurnBasedCombatant> playerUnits,
            IEnumerable<ITurnBasedCombatant> enemyUnits)
        {
            if (IsInBattle)
            {
                Debug.LogWarning("[TurnManager] 战斗已在进行中，无法开始新战斗");
                return;
            }

            // 初始化上下文
            Context.Initialize(playerUnits, enemyUnits);
            OrderCalculator?.Reset();

            // 通知所有单位进入战斗
            foreach (var unit in Context.AllCombatants)
            {
                unit.OnEnterCombat();
            }

            Debug.Log($"[TurnManager] 战斗开始 - 玩家: {Context.PlayerUnits.Count}, 敌人: {Context.EnemyUnits.Count}");

            // 触发战斗开始事件
            OnBattleStarted?.Invoke(new BattleStartedEventArgs(Context));

            // 开始第一回合
            StartNewTurn();
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        public void EndBattle(BattleResult result)
        {
            if (!IsInBattle) return;

            Context.Result = result;
            ChangePhase(BattlePhase.BattleEnd);

            // 通知所有单位离开战斗
            foreach (var unit in Context.AllCombatants)
            {
                unit.OnExitCombat();
            }

            Debug.Log($"[TurnManager] 战斗结束 - 结果: {result}, 回合数: {Context.TurnCount}");

            // 触发战斗结束事件
            OnBattleEnded?.Invoke(new BattleEndedEventArgs(Context, result));
        }

        /// <summary>
        /// 提交玩家行动 (UI调用)
        /// </summary>
        public void SubmitPlayerAction(BattleAction action)
        {
            if (CurrentPhase != BattlePhase.PlayerCommand)
            {
                Debug.LogWarning("[TurnManager] 当前不是玩家输入阶段");
                return;
            }

            if (action.Actor != CurrentActor)
            {
                Debug.LogWarning("[TurnManager] 行动执行者与当前单位不匹配");
                return;
            }

            _pendingAction = action;
            ExecuteCurrentAction();
        }

        /// <summary>
        /// 获取当前行动顺序
        /// </summary>
        public IEnumerable<ITurnBasedCombatant> GetTurnOrder()
        {
            return Context.ActionQueue.ToList();
        }

        /// <summary>
        /// 获取预览行动顺序
        /// </summary>
        public IEnumerable<ITurnBasedCombatant> GetFutureTurnOrder(int previewTurns = 5)
        {
            return OrderCalculator?.GetFutureOrder(Context.AliveUnits, previewTurns)
                ?? Enumerable.Empty<ITurnBasedCombatant>();
        }

        /// <summary>
        /// 设置行动顺序计算器
        /// </summary>
        public void SetOrderCalculator(ITurnOrderCalculator calculator)
        {
            OrderCalculator = calculator;
            if (IsInBattle)
            {
                RefreshActionQueue();
            }
        }

        /// <summary>
        /// 强制结束当前单位回合 (跳过)
        /// </summary>
        public void ForceEndCurrentUnitTurn()
        {
            if (CurrentActor == null) return;

            CurrentActor.HasActed = true;
            ProcessNextUnit();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化行动顺序计算器
        /// </summary>
        private void InitializeOrderCalculator()
        {
            OrderCalculator = _orderCalculatorType switch
            {
                TurnOrderCalculatorType.SpeedBased => new Variants.SpeedBasedTurnOrder(),
                _ => new Variants.SpeedBasedTurnOrder()
            };
        }

        /// <summary>
        /// 开始新回合
        /// </summary>
        private void StartNewTurn()
        {
            Context.TurnCount++;
            ChangePhase(BattlePhase.TurnStart);

            Debug.Log($"[TurnManager] 回合 {Context.TurnCount} 开始");

            // 重置所有单位的回合状态
            foreach (var unit in Context.AliveUnits)
            {
                unit.ResetTurnState();
                unit.OnTurnStart();
            }

            // 触发回合开始事件
            OnTurnStarted?.Invoke(new TurnEventArgs(Context));

            // 刷新行动队列
            RefreshActionQueue();

            // 处理第一个单位
            ProcessNextUnit();
        }

        /// <summary>
        /// 刷新行动队列
        /// </summary>
        private void RefreshActionQueue()
        {
            Context.ActionQueue.Clear();

            var orderedUnits = OrderCalculator?.CalculateOrder(Context.AliveUnits, Context)
                ?? Context.AliveUnits.OrderByDescending(u => u.Speed);

            foreach (var unit in orderedUnits)
            {
                Context.ActionQueue.Enqueue(unit);
            }
        }

        /// <summary>
        /// 处理下一个单位
        /// </summary>
        private void ProcessNextUnit()
        {
            // 检查战斗是否结束
            var endResult = Context.CheckBattleEnd();
            if (endResult != BattleResult.None)
            {
                EndBattle(endResult);
                return;
            }

            // 检查是否还有单位需要行动
            while (Context.ActionQueue.Count > 0)
            {
                var nextUnit = Context.ActionQueue.Dequeue();

                // 跳过死亡或已行动的单位
                if (!nextUnit.IsAlive || nextUnit.HasActed)
                {
                    continue;
                }

                // 跳过无法行动的单位 (眩晕、破盾等)
                if (!nextUnit.CanAct)
                {
                    Debug.Log($"[TurnManager] {nextUnit.DisplayName} 无法行动，跳过");
                    nextUnit.HasActed = true;
                    continue;
                }

                // 设置当前行动单位
                Context.CurrentActor = nextUnit;
                OnUnitTurnStarted?.Invoke(new UnitTurnEventArgs(nextUnit, Context));

                // 根据单位类型选择阶段
                if (nextUnit.IsPlayerControlled)
                {
                    ChangePhase(BattlePhase.PlayerCommand);
                    OnWaitingForPlayerInput?.Invoke(nextUnit);
                }
                else
                {
                    ChangePhase(BattlePhase.EnemyAI);
                    if (_autoProcessEnemyAI)
                    {
                        ProcessEnemyAI(nextUnit);
                    }
                }

                return;
            }

            // 所有单位都行动完毕，结束回合
            EndCurrentTurn();
        }

        /// <summary>
        /// 执行当前行动
        /// </summary>
        private void ExecuteCurrentAction()
        {
            if (_pendingAction == null) return;

            ChangePhase(BattlePhase.ActionExecution);

            var action = _pendingAction;
            _pendingAction = null;

            // 执行行动 (由外部系统处理具体逻辑)
            action.IsExecuted = true;
            action.IsSuccessful = true; // 默认成功，具体判断由外部系统处理

            // 通知行动顺序计算器
            OrderCalculator?.OnCombatantActed(action.Actor, Context);

            // 记录行动
            Context.RecordAction(action);

            // 标记单位已行动
            action.Actor.HasActed = true;

            Debug.Log($"[TurnManager] {action.Actor.DisplayName} 执行了 {action.ActionType}");

            // 触发行动执行事件
            OnActionExecuted?.Invoke(new ActionExecutedEventArgs(action, Context, action.IsSuccessful));

            // 处理行动结算
            ChangePhase(BattlePhase.ActionResolution);

            // 触发单位回合结束
            OnUnitTurnEnded?.Invoke(new UnitTurnEventArgs(action.Actor, Context));

            // 处理下一个单位
            ProcessNextUnit();
        }

        /// <summary>
        /// 处理敌人AI
        /// </summary>
        private void ProcessEnemyAI(ITurnBasedCombatant enemy)
        {
            // 简单AI: 随机选择一个存活的玩家单位进行攻击
            var aliveTargets = Context.AlivePlayerUnits.ToList();
            if (aliveTargets.Count == 0)
            {
                _pendingAction = BattleAction.Skip(enemy);
            }
            else
            {
                var target = aliveTargets[UnityEngine.Random.Range(0, aliveTargets.Count)];
                _pendingAction = BattleAction.Attack(enemy, target);
            }

            // 延迟执行 (给UI时间显示)
            if (_actionDelay > 0)
            {
                Invoke(nameof(ExecuteCurrentAction), _actionDelay);
            }
            else
            {
                ExecuteCurrentAction();
            }
        }

        /// <summary>
        /// 结束当前回合
        /// </summary>
        private void EndCurrentTurn()
        {
            ChangePhase(BattlePhase.TurnEnd);

            // 触发所有单位的回合结束
            foreach (var unit in Context.AliveUnits)
            {
                unit.OnTurnEnd();
            }

            Debug.Log($"[TurnManager] 回合 {Context.TurnCount} 结束");

            // 触发回合结束事件
            OnTurnEnded?.Invoke(new TurnEventArgs(Context));

            // 检查战斗是否结束
            var endResult = Context.CheckBattleEnd();
            if (endResult != BattleResult.None)
            {
                EndBattle(endResult);
                return;
            }

            // 开始新回合
            StartNewTurn();
        }

        /// <summary>
        /// 改变战斗阶段
        /// </summary>
        private void ChangePhase(BattlePhase newPhase)
        {
            var oldPhase = Context.CurrentPhase;
            Context.CurrentPhase = newPhase;

            OnPhaseChanged?.Invoke(new PhaseChangedEventArgs(oldPhase, newPhase, Context));
        }

        #endregion
    }

    /// <summary>
    /// 行动顺序计算器类型
    /// </summary>
    public enum TurnOrderCalculatorType
    {
        /// <summary>速度排序制 (八方旅人)</summary>
        SpeedBased = 0,
        /// <summary>ATB 制 (最终幻想)</summary>
        ATB = 1,
        /// <summary>CTB 制 (FFX)</summary>
        CTB = 2,
        /// <summary>行动点制</summary>
        ActionPoint = 3
    }
}
