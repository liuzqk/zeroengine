using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Core;
using ZeroEngine.Save;

namespace ZeroEngine.Relationship
{
    /// <summary>
    /// 好感度系统管理器
    /// 负责NPC好感度追踪、礼物系统和事件触发
    /// </summary>
    public class RelationshipManager : MonoSingleton<RelationshipManager>, ISaveable
    {
        [Header("配置")]
        [Tooltip("所有NPC数据")]
        [SerializeField] private List<RelationshipDataSO> _allNpcs = new List<RelationshipDataSO>();

        [Tooltip("NPC组")]
        [SerializeField] private List<RelationshipGroupSO> _npcGroups = new List<RelationshipGroupSO>();

        [Header("调试")]
        [SerializeField] private bool _debugMode;

        // 进度数据
        private readonly Dictionary<string, RelationshipProgress> _progressData =
            new Dictionary<string, RelationshipProgress>();

        // 缓存：ID -> NPC数据
        private readonly Dictionary<string, RelationshipDataSO> _npcLookup =
            new Dictionary<string, RelationshipDataSO>();

        // 缓存：类型 -> NPC列表
        private readonly Dictionary<NpcType, List<RelationshipDataSO>> _typeCache =
            new Dictionary<NpcType, List<RelationshipDataSO>>();

        // 临时列表
        private readonly List<RelationshipDataSO> _tempNpcList = new List<RelationshipDataSO>(16);
        private readonly List<RelationshipEvent> _tempEventList = new List<RelationshipEvent>(8);

        // 当前日期（用于每日重置）- 使用整数避免每帧GC
        private string _currentDate;
        private int _currentDayOfYear;
        private int _currentYear;

        #region Events

        /// <summary>好感度事件</summary>
        public event Action<RelationshipEventArgs> OnRelationshipEvent;

        #endregion

        #region Properties

        /// <summary>NPC总数</summary>
        public int TotalNpcCount => _allNpcs.Count;

        #endregion

        #region ISaveable

        public string SaveKey => "RelationshipManager";

        public void Register()
        {
            SaveSlotManager.Instance?.Register(this);
        }

        public void Unregister()
        {
            SaveSlotManager.Instance?.Unregister(this);
        }

        public object ExportSaveData()
        {
            return new RelationshipSaveData
            {
                ProgressData = new Dictionary<string, RelationshipProgress>(_progressData),
                LastDate = _currentDate
            };
        }

        public void ImportSaveData(object data)
        {
            if (data is not RelationshipSaveData saveData) return;

            _progressData.Clear();
            if (saveData.ProgressData != null)
            {
                foreach (var kvp in saveData.ProgressData)
                {
                    _progressData[kvp.Key] = kvp.Value;
                }
            }

            // 检查是否需要每日重置
            CacheCurrentDate();
            if (saveData.LastDate != _currentDate)
            {
                ProcessDailyReset(saveData.LastDate, _currentDate);
            }
        }

        public void ResetToDefault()
        {
            _progressData.Clear();
            CacheCurrentDate();

            // 初始化所有NPC进度
            foreach (var npc in _allNpcs)
            {
                if (npc != null)
                {
                    InitializeProgress(npc);
                }
            }
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            BuildCaches();
            CacheCurrentDate();
        }

        private void CacheCurrentDate()
        {
            var now = DateTime.Now;
            _currentDayOfYear = now.DayOfYear;
            _currentYear = now.Year;
            _currentDate = now.ToString("yyyy-MM-dd");
        }

        private void Start()
        {
            Register();

            // 初始化进度
            if (_progressData.Count == 0)
            {
                ResetToDefault();
            }
        }

        private void Update()
        {
            // 检查日期变化（使用整数比较，零GC分配）
            var now = DateTime.Now;
            if (now.DayOfYear != _currentDayOfYear || now.Year != _currentYear)
            {
                string oldDate = _currentDate;
                _currentDayOfYear = now.DayOfYear;
                _currentYear = now.Year;
                _currentDate = now.ToString("yyyy-MM-dd");
                ProcessDailyReset(oldDate, _currentDate);
            }
        }

        protected override void OnDestroy()
        {
            Unregister();
            base.OnDestroy();
        }

        #endregion

        #region Public API

        /// <summary>
        /// 获取NPC好感度数据
        /// </summary>
        public RelationshipDataSO GetNpcData(string npcId)
        {
            _npcLookup.TryGetValue(npcId, out var npc);
            return npc;
        }

        /// <summary>
        /// 获取NPC进度
        /// </summary>
        public RelationshipProgress GetProgress(string npcId)
        {
            if (!_progressData.TryGetValue(npcId, out var progress))
            {
                var npc = GetNpcData(npcId);
                if (npc != null)
                {
                    progress = InitializeProgress(npc);
                }
            }
            return progress;
        }

        /// <summary>
        /// 获取好感度点数
        /// </summary>
        public int GetPoints(string npcId)
        {
            var progress = GetProgress(npcId);
            return progress?.Points ?? 0;
        }

        /// <summary>
        /// 获取好感度等级
        /// </summary>
        public RelationshipLevel GetLevel(string npcId)
        {
            var progress = GetProgress(npcId);
            return progress?.Level ?? RelationshipLevel.Stranger;
        }

        /// <summary>
        /// 增加好感度
        /// </summary>
        public void AddPoints(string npcId, int points, bool ignoreLimit = false)
        {
            var npc = GetNpcData(npcId);
            var progress = GetProgress(npcId);

            if (npc == null || progress == null) return;

            int oldPoints = progress.Points;
            RelationshipLevel oldLevel = progress.Level;

            progress.Points += points;

            // 限制最低为0
            if (progress.Points < 0)
            {
                progress.Points = 0;
            }

            // 更新等级
            progress.Level = npc.GetLevelForPoints(progress.Points);

            // 触发事件
            if (progress.Points != oldPoints)
            {
                OnRelationshipEvent?.Invoke(
                    RelationshipEventArgs.PointsChanged(npcId, npc.DisplayName, oldPoints, progress.Points));
                Log($"{npc.DisplayName} 好感度: {oldPoints} -> {progress.Points}");

                // 检查等级变化
                if (progress.Level != oldLevel)
                {
                    if ((int)progress.Level > (int)oldLevel)
                    {
                        OnRelationshipEvent?.Invoke(
                            RelationshipEventArgs.LevelUp(npcId, npc.DisplayName, oldLevel, progress.Level));
                        Log($"{npc.DisplayName} 好感等级提升: {oldLevel} -> {progress.Level}");

                        // 触发成就 (需要 Narrative 包支持)
#if ZEROENGINE_ACHIEVEMENT
                        var achievementMgr = Achievement.AchievementManager.Instance;
                        achievementMgr?.TriggerEvent("RelationshipLevelUp", npcId);
#endif
                    }
                    else
                    {
                        OnRelationshipEvent?.Invoke(
                            RelationshipEventArgs.LevelDown(npcId, npc.DisplayName, oldLevel, progress.Level));
                    }
                }
            }
        }

        /// <summary>
        /// 送礼
        /// </summary>
        public bool TryGiveGift(string npcId, Inventory.InventoryItemSO item)
        {
            var npc = GetNpcData(npcId);
            var progress = GetProgress(npcId);

            if (npc == null || progress == null || item == null)
            {
                return false;
            }

            // 检查今日送礼次数
            if (progress.GiftCountToday >= npc.MaxGiftsPerDay)
            {
                Log($"{npc.DisplayName} 今日已达送礼上限");
                return false;
            }

            // 检查物品
            var inventory = Inventory.InventoryManager.Instance;
            if (inventory == null || inventory.GetItemCount(item) <= 0)
            {
                return false;
            }

            // 消耗物品
            inventory.RemoveItem(item, 1);

            // 计算好感度变化
            int pointsChange = npc.CalculateGiftPoints(item, out GiftPreference preference);

            // 增加好感度
            AddPoints(npcId, pointsChange);

            // 更新送礼次数
            progress.GiftCountToday++;
            progress.LastInteractionDate = _currentDate;

            // 触发事件
            OnRelationshipEvent?.Invoke(
                RelationshipEventArgs.GiftReceived(npcId, npc.DisplayName, item, preference, pointsChange));

            // 触发成就 (需要 Narrative 包支持)
#if ZEROENGINE_ACHIEVEMENT
            var achievementMgr = Achievement.AchievementManager.Instance;
            achievementMgr?.TriggerEvent("GiveGift", npcId);
            achievementMgr?.TriggerEvent($"GiveGift_{preference}", npcId);
#endif

            Log($"{npc.DisplayName} 收到礼物 {item.ItemName} ({preference}), 好感度 +{pointsChange}");

            return true;
        }

        /// <summary>
        /// 对话（每日获得好感度）
        /// </summary>
        public bool TryTalk(string npcId)
        {
            var npc = GetNpcData(npcId);
            var progress = GetProgress(npcId);

            if (npc == null || progress == null)
            {
                return false;
            }

            // 检查今日对话次数
            if (progress.TalkCountToday >= npc.MaxTalksPerDay)
            {
                Log($"{npc.DisplayName} 今日已达对话上限");
                return false;
            }

            // 增加好感度
            AddPoints(npcId, npc.TalkPoints);

            // 更新对话次数
            progress.TalkCountToday++;
            progress.LastInteractionDate = _currentDate;

            // 触发成就 (需要 Narrative 包支持)
#if ZEROENGINE_ACHIEVEMENT
            var achievementMgr = Achievement.AchievementManager.Instance;
            achievementMgr?.TriggerEvent("TalkToNpc", npcId);
#endif

            Log($"{npc.DisplayName} 对话, 好感度 +{npc.TalkPoints}");

            return true;
        }

        /// <summary>
        /// 应用对话效果
        /// </summary>
        public void ApplyDialogueEffect(DialogueEffect effect)
        {
            if (effect == null || string.IsNullOrEmpty(effect.NpcId)) return;

            if (effect.PointsChange != 0)
            {
                AddPoints(effect.NpcId, effect.PointsChange);
            }

            if (!string.IsNullOrEmpty(effect.TriggerEventId))
            {
                TriggerEvent(effect.NpcId, effect.TriggerEventId);
            }
        }

        /// <summary>
        /// 触发好感度事件
        /// </summary>
        public bool TriggerEvent(string npcId, string eventId)
        {
            var npc = GetNpcData(npcId);
            var progress = GetProgress(npcId);

            if (npc == null || progress == null) return false;

            // 查找事件
            RelationshipEvent evt = null;
            for (int i = 0; i < npc.Events.Count; i++)
            {
                if (npc.Events[i].EventId == eventId)
                {
                    evt = npc.Events[i];
                    break;
                }
            }

            if (evt == null) return false;

            // 检查是否已触发
            if (evt.OneTime && progress.TriggeredEvents.Contains(eventId))
            {
                return false;
            }

            // 标记已触发
            if (evt.OneTime)
            {
                progress.TriggeredEvents.Add(eventId);
            }

            Log($"{npc.DisplayName} 触发事件: {evt.DisplayName}");

            return true;
        }

        /// <summary>
        /// 获取可触发的事件
        /// </summary>
        public void GetAvailableEvents(string npcId, List<RelationshipEvent> results)
        {
            results.Clear();

            var npc = GetNpcData(npcId);
            var progress = GetProgress(npcId);

            if (npc != null && progress != null)
            {
                npc.GetAvailableEvents(progress, results);
            }
        }

        /// <summary>
        /// 检查是否达到指定等级
        /// </summary>
        public bool HasReachedLevel(string npcId, RelationshipLevel level)
        {
            return (int)GetLevel(npcId) >= (int)level;
        }

        /// <summary>
        /// 获取所有NPC
        /// </summary>
        public IReadOnlyList<RelationshipDataSO> GetAllNpcs() => _allNpcs;

        /// <summary>
        /// 获取指定类型的NPC
        /// </summary>
        public void GetNpcsByType(NpcType type, List<RelationshipDataSO> results)
        {
            results.Clear();
            if (_typeCache.TryGetValue(type, out var cached))
            {
                results.AddRange(cached);
            }
        }

        /// <summary>
        /// 获取NPC组列表
        /// </summary>
        public IReadOnlyList<RelationshipGroupSO> GetNpcGroups() => _npcGroups;

        /// <summary>
        /// 设置自定义数据
        /// </summary>
        public void SetCustomData(string npcId, string key, string value)
        {
            var progress = GetProgress(npcId);
            if (progress != null)
            {
                progress.CustomData[key] = value;
            }
        }

        /// <summary>
        /// 获取自定义数据
        /// </summary>
        public string GetCustomData(string npcId, string key, string defaultValue = null)
        {
            var progress = GetProgress(npcId);
            if (progress != null && progress.CustomData.TryGetValue(key, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        /// <summary>
        /// 强制设置等级（调试用）
        /// </summary>
        public void ForceSetLevel(string npcId, RelationshipLevel level)
        {
            var npc = GetNpcData(npcId);
            var progress = GetProgress(npcId);

            if (npc == null || progress == null) return;

            int requiredPoints = npc.GetThreshold(level)?.RequiredPoints ?? 0;
            progress.Points = requiredPoints;
            progress.Level = level;
        }

        #endregion

        #region Internal

        private void BuildCaches()
        {
            _npcLookup.Clear();
            _typeCache.Clear();

            foreach (var npc in _allNpcs)
            {
                if (npc == null) continue;

                _npcLookup[npc.NpcId] = npc;

                if (!_typeCache.TryGetValue(npc.NpcType, out var list))
                {
                    list = new List<RelationshipDataSO>();
                    _typeCache[npc.NpcType] = list;
                }
                list.Add(npc);
            }
        }

        private RelationshipProgress InitializeProgress(RelationshipDataSO npc)
        {
            var progress = new RelationshipProgress
            {
                NpcId = npc.NpcId,
                Points = npc.InitialPoints,
                Level = npc.GetLevelForPoints(npc.InitialPoints),
                LastInteractionDate = _currentDate
            };

            _progressData[npc.NpcId] = progress;
            return progress;
        }

        private void ProcessDailyReset(string oldDate, string newDate)
        {
            Log($"日期变更: {oldDate} -> {newDate}");

            foreach (var kvp in _progressData)
            {
                var progress = kvp.Value;
                var npc = GetNpcData(kvp.Key);

                // 重置每日计数
                progress.GiftCountToday = 0;
                progress.TalkCountToday = 0;

                // 好感度衰减
                if (npc != null && npc.DailyDecay > 0)
                {
                    int oldPoints = progress.Points;
                    progress.Points = Mathf.Max(0, progress.Points - npc.DailyDecay);

                    if (progress.Points != oldPoints)
                    {
                        progress.Level = npc.GetLevelForPoints(progress.Points);
                        Log($"{npc.DisplayName} 每日衰减: {oldPoints} -> {progress.Points}");
                    }
                }
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("ZEROENGINE_DEBUG")]
        private void Log(string message)
        {
            if (_debugMode)
            {
                Debug.Log($"[Relationship] {message}");
            }
        }

        #endregion
    }

    #region Save Data

    [Serializable]
    public class RelationshipSaveData
    {
        public Dictionary<string, RelationshipProgress> ProgressData;
        public string LastDate;
    }

    #endregion
}