// ============================================================================
// ZeroEngine v2.6.0 - Encounter System
// 遭遇管理器
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using ZeroEngine.Core;
using Debug = UnityEngine.Debug;

namespace ZeroEngine.RPG.Encounter
{
    /// <summary>
    /// 遭遇结果数据
    /// </summary>
    public readonly struct EncounterResult
    {
        public readonly EncounterEntry Entry;
        public readonly List<string> EnemyIds;
        public readonly int EnemyCount;
        public readonly bool IsElite;
        public readonly bool IsBoss;
        public readonly int AreaLevel;

        public EncounterResult(EncounterEntry entry, int areaLevel)
        {
            Entry = entry;
            EnemyCount = entry.GetRandomCount();
            IsElite = entry.IsElite;
            IsBoss = entry.IsBoss;
            AreaLevel = areaLevel;

            // 根据数量生成敌人 ID 列表
            EnemyIds = new List<string>();
            for (int i = 0; i < EnemyCount; i++)
            {
                // 循环选取敌人 ID
                var enemyId = entry.EnemyIds[i % entry.EnemyIds.Count];
                EnemyIds.Add(enemyId);
            }
        }
    }

    /// <summary>
    /// 遭遇事件参数
    /// </summary>
    public readonly struct EncounterEventArgs
    {
        public readonly EncounterResult Result;
        public readonly EncounterTableSO Table;
        public readonly int StepCount;

        public EncounterEventArgs(EncounterResult result, EncounterTableSO table, int stepCount)
        {
            Result = result;
            Table = table;
            StepCount = stepCount;
        }
    }

    /// <summary>
    /// 遭遇管理器 - 管理随机遭遇的触发和生成
    /// </summary>
    public class EncounterManager : MonoSingleton<EncounterManager>
    {
        [Header("配置")]
        [Tooltip("当前区域的遭遇表")]
        [SerializeField] private EncounterTableSO _currentTable;

        [Tooltip("玩家当前等级 (供外部设置)")]
        [SerializeField] private int _playerLevel = 1;

        [Header("状态")]
        [SerializeField] private int _stepsSinceLastEncounter;
        [SerializeField] private int _totalSteps;
        [SerializeField] private bool _encountersEnabled = true;

        // 事件
        public event Action<EncounterEventArgs> OnEncounterTriggered;
        public event Action<float> OnEncounterRateChanged;
        public event Action OnEncounterAvoided;

        // 属性
        public EncounterTableSO CurrentTable => _currentTable;
        public int PlayerLevel => _playerLevel;
        public int StepsSinceLastEncounter => _stepsSinceLastEncounter;
        public int TotalSteps => _totalSteps;
        public bool EncountersEnabled => _encountersEnabled;

        /// <summary>
        /// 当前遭遇率
        /// </summary>
        public float CurrentEncounterRate
        {
            get
            {
                if (_currentTable == null || !_encountersEnabled) return 0f;
                return _currentTable.CalculateEncounterRate(_stepsSinceLastEncounter);
            }
        }

        // ========================================
        // 公共 API
        // ========================================

        /// <summary>
        /// 设置当前区域的遭遇表
        /// </summary>
        public void SetEncounterTable(EncounterTableSO table)
        {
            _currentTable = table;
            _stepsSinceLastEncounter = 0;
            LogDebug("切换遭遇表: ", table?.DisplayName ?? "无");
        }

        /// <summary>
        /// 设置玩家等级
        /// </summary>
        public void SetPlayerLevel(int level)
        {
            _playerLevel = Mathf.Max(1, level);
        }

        /// <summary>
        /// 启用/禁用遭遇
        /// </summary>
        public void SetEncountersEnabled(bool enabled)
        {
            _encountersEnabled = enabled;
            LogDebug("遭遇系统: ", enabled ? "启用" : "禁用");
        }

        /// <summary>
        /// 记录一步移动，检查是否触发遭遇
        /// </summary>
        /// <returns>如果触发遭遇返回结果，否则返回 null</returns>
        public EncounterResult? ProcessStep()
        {
            if (_currentTable == null || !_encountersEnabled)
            {
                return null;
            }

            _stepsSinceLastEncounter++;
            _totalSteps++;

            float rate = CurrentEncounterRate;
            OnEncounterRateChanged?.Invoke(rate);

            // 随机检测是否触发遭遇
            if (UnityEngine.Random.value < rate)
            {
                return TriggerRandomEncounter();
            }

            return null;
        }

        /// <summary>
        /// 强制触发随机遭遇
        /// </summary>
        public EncounterResult? TriggerRandomEncounter()
        {
            if (_currentTable == null) return null;

            // 检查是否触发精英遭遇
            bool isElite = _totalSteps >= _currentTable.EliteMinSteps &&
                           UnityEngine.Random.value < _currentTable.EliteChance;

            EncounterEntry entry;

            if (isElite)
            {
                var eliteEntries = _currentTable.GetValidEliteEntries(_playerLevel);
                entry = _currentTable.SelectWeightedEntry(eliteEntries);

                // 如果没有有效精英遭遇，回退到普通遭遇
                if (entry == null)
                {
                    var normalEntries = _currentTable.GetValidNormalEntries(_playerLevel);
                    entry = _currentTable.SelectWeightedEntry(normalEntries);
                }
            }
            else
            {
                var normalEntries = _currentTable.GetValidNormalEntries(_playerLevel);
                entry = _currentTable.SelectWeightedEntry(normalEntries);
            }

            if (entry == null)
            {
                Debug.LogWarning("[Encounter] 没有有效的遭遇条目");
                return null;
            }

            // 计算区域等级
            int areaLevel = Mathf.Clamp(
                _playerLevel,
                _currentTable.LevelRange.x,
                _currentTable.LevelRange.y
            );

            var result = new EncounterResult(entry, areaLevel);

            // 重置计步
            int stepCount = _stepsSinceLastEncounter;
            _stepsSinceLastEncounter = 0;

            // 触发事件
            OnEncounterTriggered?.Invoke(new EncounterEventArgs(result, _currentTable, stepCount));

            LogDebugFormat("触发遭遇: {0} (等级 {1}, {2} 只敌人)", entry.EntryId, areaLevel, result.EnemyCount);

            return result;
        }

        /// <summary>
        /// 强制触发 Boss 遭遇
        /// </summary>
        public EncounterResult? TriggerBossEncounter(string bossEntryId)
        {
            if (_currentTable == null) return null;

            var entry = _currentTable.GetBossEntry(bossEntryId);
            if (entry == null)
            {
                Debug.LogError($"[Encounter] Boss 遭遇不存在: {bossEntryId}");
                return null;
            }

            int areaLevel = Mathf.Clamp(
                _playerLevel,
                _currentTable.LevelRange.x,
                _currentTable.LevelRange.y
            );

            var result = new EncounterResult(entry, areaLevel);

            // 重置计步
            int stepCount = _stepsSinceLastEncounter;
            _stepsSinceLastEncounter = 0;

            // 触发事件
            OnEncounterTriggered?.Invoke(new EncounterEventArgs(result, _currentTable, stepCount));

            LogDebug("触发 Boss 遭遇: ", entry.EntryId);

            return result;
        }

        /// <summary>
        /// 使用逃跑道具 (重置遭遇率)
        /// </summary>
        public void UseRepelItem(int effectSteps = 100)
        {
            _stepsSinceLastEncounter = -effectSteps;
            OnEncounterAvoided?.Invoke();
            LogDebugFormat("使用驱虫剂，效果持续 {0} 步", effectSteps);
        }

        /// <summary>
        /// 重置步数计数
        /// </summary>
        public void ResetStepCount()
        {
            _stepsSinceLastEncounter = 0;
            _totalSteps = 0;
        }

        // ========================================
        // 存档支持
        // ========================================

        /// <summary>
        /// 导出存档数据
        /// </summary>
        public Dictionary<string, object> ExportSaveData()
        {
            return new Dictionary<string, object>
            {
                ["stepsSinceLastEncounter"] = _stepsSinceLastEncounter,
                ["totalSteps"] = _totalSteps,
                ["encountersEnabled"] = _encountersEnabled,
                ["tableId"] = _currentTable?.TableId
            };
        }

        /// <summary>
        /// 导入存档数据
        /// </summary>
        public void ImportSaveData(Dictionary<string, object> data)
        {
            if (data.TryGetValue("stepsSinceLastEncounter", out var steps))
                _stepsSinceLastEncounter = Convert.ToInt32(steps);

            if (data.TryGetValue("totalSteps", out var total))
                _totalSteps = Convert.ToInt32(total);

            if (data.TryGetValue("encountersEnabled", out var enabled))
                _encountersEnabled = Convert.ToBoolean(enabled);
        }

        // ========================================
        // 条件编译 Debug 日志 (零 GC)
        // ========================================

        /// <summary>
        /// 条件编译 Debug 日志 (仅 Development Build 有效)
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void LogDebug(string prefix, string message)
        {
            Debug.Log(string.Concat("[Encounter] ", prefix, message));
        }

        /// <summary>
        /// 条件编译格式化 Debug 日志 (仅 Development Build 有效)
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void LogDebugFormat(string format, object arg0)
        {
            Debug.LogFormat("[Encounter] " + format, arg0);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void LogDebugFormat(string format, object arg0, object arg1, object arg2)
        {
            Debug.LogFormat("[Encounter] " + format, arg0, arg1, arg2);
        }
    }
}
