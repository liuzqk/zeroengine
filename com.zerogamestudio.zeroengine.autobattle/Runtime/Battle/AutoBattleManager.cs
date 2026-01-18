using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.AutoBattle.Grid;

namespace ZeroEngine.AutoBattle.Battle
{
    /// <summary>
    /// 自动战斗管理器，控制战斗流程
    /// </summary>
    public class AutoBattleManager
    {
        /// <summary>
        /// 玩家方棋盘
        /// </summary>
        public GridBoard PlayerBoard { get; private set; }

        /// <summary>
        /// 敌方棋盘
        /// </summary>
        public GridBoard EnemyBoard { get; private set; }

        /// <summary>
        /// 当前战斗状态
        /// </summary>
        public BattleState State { get; private set; } = BattleState.Idle;

        /// <summary>
        /// 战斗时间（秒）
        /// </summary>
        public float BattleTime { get; private set; }

        /// <summary>
        /// 最大战斗时长（秒）
        /// </summary>
        public float MaxBattleDuration { get; set; } = 60f;

        /// <summary>
        /// 战斗速度倍率
        /// </summary>
        public float TimeScale { get; set; } = 1f;

        /// <summary>
        /// 玩家方单位列表
        /// </summary>
        private readonly List<IBattleUnit> _playerUnits = new();

        /// <summary>
        /// 敌方单位列表
        /// </summary>
        private readonly List<IBattleUnit> _enemyUnits = new();

        // 事件
        public event Action OnBattleStart;
        public event Action<BattleResult> OnBattleEnd;
        public event Action<IBattleUnit> OnUnitDeath;
        public event Action<float> OnBattleTick;

        /// <summary>
        /// 棋盘默认尺寸
        /// </summary>
        public const int DefaultBoardWidth = 5;
        public const int DefaultBoardHeight = 5;

        public AutoBattleManager() : this(DefaultBoardWidth, DefaultBoardHeight) { }

        public AutoBattleManager(int boardWidth, int boardHeight)
        {
            PlayerBoard = new GridBoard(boardWidth, boardHeight);
            EnemyBoard = new GridBoard(boardWidth, boardHeight);
        }

        /// <summary>
        /// 添加玩家单位
        /// </summary>
        public void AddPlayerUnit(IBattleUnit unit, Vector2Int position)
        {
            if (State != BattleState.Idle && State != BattleState.Preparing)
            {
                Debug.LogWarning("[AutoBattle] 只能在准备阶段添加单位");
                return;
            }

            if (PlayerBoard.PlaceUnit(unit, position))
            {
                _playerUnits.Add(unit);
            }
        }

        /// <summary>
        /// 添加敌方单位
        /// </summary>
        public void AddEnemyUnit(IBattleUnit unit, Vector2Int position)
        {
            if (State != BattleState.Idle && State != BattleState.Preparing)
            {
                Debug.LogWarning("[AutoBattle] 只能在准备阶段添加单位");
                return;
            }

            if (EnemyBoard.PlaceUnit(unit, position))
            {
                _enemyUnits.Add(unit);
            }
        }

        /// <summary>
        /// 进入准备阶段
        /// </summary>
        public void EnterPreparation()
        {
            if (State != BattleState.Idle)
            {
                Debug.LogWarning("[AutoBattle] 只能从空闲状态进入准备阶段");
                return;
            }

            State = BattleState.Preparing;
            BattleTime = 0f;
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartBattle()
        {
            if (State != BattleState.Preparing)
            {
                Debug.LogWarning("[AutoBattle] 只能从准备阶段开始战斗");
                return;
            }

            if (_playerUnits.Count == 0 || _enemyUnits.Count == 0)
            {
                Debug.LogWarning("[AutoBattle] 双方都需要至少一个单位");
                return;
            }

            State = BattleState.Fighting;
            OnBattleStart?.Invoke();
        }

        /// <summary>
        /// 战斗更新（每帧调用）
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (State != BattleState.Fighting)
                return;

            float scaledDelta = deltaTime * TimeScale;
            BattleTime += scaledDelta;

            // 检查战斗结束条件
            var result = CheckBattleEnd();
            if (result != BattleResult.None)
            {
                EndBattle(result);
                return;
            }

            // 触发战斗更新事件
            OnBattleTick?.Invoke(scaledDelta);
        }

        /// <summary>
        /// 检查战斗结束条件
        /// </summary>
        private BattleResult CheckBattleEnd()
        {
            // 超时判定
            if (BattleTime >= MaxBattleDuration)
            {
                return BattleResult.Timeout;
            }

            // 检查存活单位
            int playerAlive = 0;
            int enemyAlive = 0;

            foreach (var unit in _playerUnits)
            {
                if (unit.IsAlive) playerAlive++;
            }

            foreach (var unit in _enemyUnits)
            {
                if (unit.IsAlive) enemyAlive++;
            }

            if (enemyAlive == 0)
                return BattleResult.Victory;

            if (playerAlive == 0)
                return BattleResult.Defeat;

            return BattleResult.None;
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        private void EndBattle(BattleResult result)
        {
            State = BattleState.Ended;
            OnBattleEnd?.Invoke(result);
        }

        /// <summary>
        /// 通知单位死亡
        /// </summary>
        public void NotifyUnitDeath(IBattleUnit unit)
        {
            OnUnitDeath?.Invoke(unit);

            // 从棋盘移除
            if (unit.Team == BattleTeam.Player)
            {
                PlayerBoard.RemoveUnit(unit);
            }
            else
            {
                EnemyBoard.RemoveUnit(unit);
            }
        }

        /// <summary>
        /// 获取玩家存活单位
        /// </summary>
        public IReadOnlyList<IBattleUnit> GetAlivePlayerUnits()
        {
            var result = new List<IBattleUnit>();
            foreach (var unit in _playerUnits)
            {
                if (unit.IsAlive) result.Add(unit);
            }
            return result;
        }

        /// <summary>
        /// 获取敌方存活单位
        /// </summary>
        public IReadOnlyList<IBattleUnit> GetAliveEnemyUnits()
        {
            var result = new List<IBattleUnit>();
            foreach (var unit in _enemyUnits)
            {
                if (unit.IsAlive) result.Add(unit);
            }
            return result;
        }

        /// <summary>
        /// 重置战斗
        /// </summary>
        public void Reset()
        {
            State = BattleState.Idle;
            BattleTime = 0f;
            TimeScale = 1f;

            PlayerBoard.Clear();
            EnemyBoard.Clear();

            _playerUnits.Clear();
            _enemyUnits.Clear();
        }
    }

    /// <summary>
    /// 战斗状态
    /// </summary>
    public enum BattleState
    {
        /// <summary>
        /// 空闲
        /// </summary>
        Idle,

        /// <summary>
        /// 准备阶段（布阵）
        /// </summary>
        Preparing,

        /// <summary>
        /// 战斗中
        /// </summary>
        Fighting,

        /// <summary>
        /// 战斗结束
        /// </summary>
        Ended
    }

    /// <summary>
    /// 战斗结果
    /// </summary>
    public enum BattleResult
    {
        /// <summary>
        /// 无结果（战斗进行中）
        /// </summary>
        None,

        /// <summary>
        /// 胜利
        /// </summary>
        Victory,

        /// <summary>
        /// 失败
        /// </summary>
        Defeat,

        /// <summary>
        /// 超时
        /// </summary>
        Timeout
    }
}
