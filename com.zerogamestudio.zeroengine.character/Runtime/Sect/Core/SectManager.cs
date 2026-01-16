// ============================================================================
// SectManager.cs
// 门派管理器 (MonoSingleton + ISaveable)
// 创建于: 2026-01-09
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Core;

namespace ZeroEngine.Character.Sect
{
    /// <summary>
    /// 门派管理器
    /// 管理角色门派、职位晋升、贡献度、武学学习
    /// </summary>
    public class SectManager : MonoSingleton<SectManager>
    {
        [Header("配置")]
        [Tooltip("门派数据库")]
        [SerializeField]
        private SectDatabaseSO _database;

        [Tooltip("是否允许叛出门派")]
        [SerializeField]
        private bool _allowBetray = true;

        [Tooltip("叛出门派的声望惩罚")]
        [SerializeField]
        private int _betrayReputationPenalty = -50;

        [Tooltip("是否允许同时加入多个门派")]
        [SerializeField]
        private bool _allowMultipleSects = false;

        [Header("运行时数据")]
        [SerializeField]
        private string _characterId = "player";

        [SerializeField]
        private SectInstance _currentSect;

        [SerializeField]
        private List<SectType> _unlockedSects = new List<SectType>();

        [SerializeField]
        private Dictionary<SectType, SectInstance> _sectHistory = new Dictionary<SectType, SectInstance>();

        // ===== 事件 =====

        /// <summary>加入门派事件</summary>
        public event Action<SectJoinedEventArgs> OnSectJoined;

        /// <summary>离开门派事件</summary>
        public event Action<SectLeftEventArgs> OnSectLeft;

        /// <summary>职位变化事件</summary>
        public event Action<SectRankChangedEventArgs> OnRankChanged;

        /// <summary>贡献度变化事件</summary>
        public event Action<SectContributionChangedEventArgs> OnContributionChanged;

        /// <summary>学习门派武学事件</summary>
        public event Action<SectMartialArtLearnedEventArgs> OnMartialArtLearned;

        // ===== 属性 =====

        /// <summary>门派数据库</summary>
        public SectDatabaseSO Database => _database;

        /// <summary>当前门派实例</summary>
        public SectInstance CurrentSect => _currentSect;

        /// <summary>是否有门派</summary>
        public bool HasSect => _currentSect != null && !_currentSect.IsExpelled;

        /// <summary>当前门派类型</summary>
        public SectType CurrentSectType => _currentSect?.SectType ?? SectType.None;

        /// <summary>当前职位</summary>
        public SectRank CurrentRank => _currentSect?.CurrentRank ?? SectRank.None;

        /// <summary>当前贡献度</summary>
        public int CurrentContribution => _currentSect?.Contribution ?? 0;

        /// <summary>已解锁门派列表</summary>
        public IReadOnlyList<SectType> UnlockedSects => _unlockedSects;

        // ===== 初始化 =====

        protected override void Awake()
        {
            base.Awake();
            InitializeDefaultUnlocks();
        }

        private void InitializeDefaultUnlocks()
        {
            if (_database == null) return;

            // 解锁所有不需要特殊条件的门派
            foreach (var sectData in _database.GetAllSects())
            {
                if (!sectData.requiresUnlock && !_unlockedSects.Contains(sectData.sectType))
                {
                    _unlockedSects.Add(sectData.sectType);
                }
            }
        }

        // ===== 门派操作 =====

        /// <summary>
        /// 加入门派
        /// </summary>
        public bool JoinSect(SectType sectType, SectRank initialRank = SectRank.Initiate)
        {
            // 检查是否已有门派
            if (HasSect && !_allowMultipleSects)
            {
                Debug.LogWarning($"[SectManager] 已有门派 {CurrentSectType}, 无法加入 {sectType}");
                return false;
            }

            // 检查门派是否解锁
            if (!_unlockedSects.Contains(sectType))
            {
                Debug.LogWarning($"[SectManager] 门派 {sectType} 未解锁");
                return false;
            }

            // 获取门派数据
            var sectData = _database?.GetSectData(sectType);
            if (sectData == null)
            {
                Debug.LogError($"[SectManager] 找不到门派数据: {sectType}");
                return false;
            }

            // 检查是否有历史记录 (重新加入)
            bool isRejoin = false;
            if (_sectHistory.TryGetValue(sectType, out var existingInstance))
            {
                if (existingInstance.IsExpelled)
                {
                    // 重新加入
                    existingInstance.Rejoin(initialRank);
                    _currentSect = existingInstance;
                    isRejoin = true;
                }
                else
                {
                    // 恢复之前的状态
                    _currentSect = existingInstance;
                    isRejoin = true;
                }
            }
            else
            {
                // 创建新实例
                _currentSect = new SectInstance(sectData, initialRank);
                _sectHistory[sectType] = _currentSect;
            }

            // 绑定事件
            BindSectEvents(_currentSect);

            // 触发事件
            OnSectJoined?.Invoke(new SectJoinedEventArgs
            {
                CharacterId = _characterId,
                SectType = sectType,
                InitialRank = initialRank,
                IsRejoin = isRejoin
            });

            // 广播全局事件
            EventManager.Trigger(SectEvents.OnSectJoined, new SectJoinedEventArgs
            {
                CharacterId = _characterId,
                SectType = sectType,
                InitialRank = initialRank,
                IsRejoin = isRejoin
            });

            Debug.Log($"[SectManager] {_characterId} 加入门派 {sectType}, 职位: {initialRank}");
            return true;
        }

        /// <summary>
        /// 离开门派
        /// </summary>
        public bool LeaveSect(SectLeaveReason reason = SectLeaveReason.Voluntary)
        {
            if (!HasSect)
            {
                Debug.LogWarning("[SectManager] 当前没有门派");
                return false;
            }

            // 叛出检查
            if (reason == SectLeaveReason.Betrayed && !_allowBetray)
            {
                Debug.LogWarning("[SectManager] 不允许叛出门派");
                return false;
            }

            var leftSectType = CurrentSectType;
            var details = "";

            // 处理不同离开原因
            switch (reason)
            {
                case SectLeaveReason.Expelled:
                    _currentSect.Expel("被逐出师门");
                    details = "被逐出师门";
                    break;

                case SectLeaveReason.Betrayed:
                    _currentSect.ModifyReputation(_betrayReputationPenalty);
                    details = "叛出师门";
                    break;

                case SectLeaveReason.Voluntary:
                    details = "主动退出";
                    break;

                case SectLeaveReason.SwitchedSect:
                    details = "转投他派";
                    break;
            }

            // 解绑事件
            UnbindSectEvents(_currentSect);

            // 触发事件
            OnSectLeft?.Invoke(new SectLeftEventArgs
            {
                CharacterId = _characterId,
                SectType = leftSectType,
                Reason = reason,
                Details = details
            });

            // 广播全局事件
            EventManager.Trigger(SectEvents.OnSectLeft, new SectLeftEventArgs
            {
                CharacterId = _characterId,
                SectType = leftSectType,
                Reason = reason,
                Details = details
            });

            // 清除当前门派 (但保留历史记录)
            _currentSect = null;

            Debug.Log($"[SectManager] {_characterId} 离开门派 {leftSectType}, 原因: {reason}");
            return true;
        }

        /// <summary>
        /// 转投门派 (离开当前门派并加入新门派)
        /// </summary>
        public bool SwitchSect(SectType newSectType, SectRank initialRank = SectRank.Initiate)
        {
            if (HasSect)
            {
                if (!LeaveSect(SectLeaveReason.SwitchedSect))
                    return false;
            }

            return JoinSect(newSectType, initialRank);
        }

        // ===== 贡献度操作 =====

        /// <summary>
        /// 增加贡献度
        /// </summary>
        public void AddContribution(int amount, ContributionChangeReason reason = ContributionChangeReason.Other)
        {
            if (!HasSect || amount <= 0) return;

            int oldValue = _currentSect.Contribution;
            _currentSect.AddContribution(amount);

            OnContributionChanged?.Invoke(new SectContributionChangedEventArgs
            {
                CharacterId = _characterId,
                SectType = CurrentSectType,
                OldValue = oldValue,
                NewValue = _currentSect.Contribution,
                Reason = reason
            });
        }

        /// <summary>
        /// 消耗贡献度
        /// </summary>
        public bool SpendContribution(int amount, ContributionChangeReason reason = ContributionChangeReason.Other)
        {
            if (!HasSect || amount <= 0) return false;

            int oldValue = _currentSect.Contribution;
            if (!_currentSect.SpendContribution(amount))
                return false;

            OnContributionChanged?.Invoke(new SectContributionChangedEventArgs
            {
                CharacterId = _characterId,
                SectType = CurrentSectType,
                OldValue = oldValue,
                NewValue = _currentSect.Contribution,
                Reason = reason
            });

            return true;
        }

        // ===== 声望操作 =====

        /// <summary>
        /// 修改声望
        /// </summary>
        public void ModifyReputation(int delta)
        {
            if (!HasSect || delta == 0) return;
            _currentSect.ModifyReputation(delta);
        }

        // ===== 武学操作 =====

        /// <summary>
        /// 学习门派武学
        /// </summary>
        public bool LearnSectMartialArt(string martialArtId)
        {
            if (!HasSect) return false;

            var entry = _currentSect.Data?.martialArts.Find(m => m.martialArtId == martialArtId);
            int cost = entry?.contributionCost ?? 0;

            if (!_currentSect.LearnMartialArt(martialArtId))
                return false;

            OnMartialArtLearned?.Invoke(new SectMartialArtLearnedEventArgs
            {
                CharacterId = _characterId,
                SectType = CurrentSectType,
                MartialArtId = martialArtId,
                ContributionCost = cost
            });

            return true;
        }

        /// <summary>
        /// 获取可学习的门派武学
        /// </summary>
        public List<SectMartialArtEntry> GetAvailableMartialArts()
        {
            if (!HasSect) return new List<SectMartialArtEntry>();
            return _currentSect.GetAvailableMartialArts();
        }

        // ===== 门派解锁 =====

        /// <summary>
        /// 解锁门派
        /// </summary>
        public bool UnlockSect(SectType sectType)
        {
            if (_unlockedSects.Contains(sectType))
                return false;

            _unlockedSects.Add(sectType);
            Debug.Log($"[SectManager] 解锁门派: {sectType}");
            return true;
        }

        /// <summary>
        /// 检查门派是否已解锁
        /// </summary>
        public bool IsSectUnlocked(SectType sectType)
        {
            return _unlockedSects.Contains(sectType);
        }

        // ===== 查询 =====

        /// <summary>
        /// 获取门派数据
        /// </summary>
        public SectDataSO GetSectData(SectType sectType)
        {
            return _database?.GetSectData(sectType);
        }

        /// <summary>
        /// 获取门派历史记录
        /// </summary>
        public SectInstance GetSectHistory(SectType sectType)
        {
            _sectHistory.TryGetValue(sectType, out var instance);
            return instance;
        }

        /// <summary>
        /// 检查是否曾加入过某门派
        /// </summary>
        public bool HasJoinedSect(SectType sectType)
        {
            return _sectHistory.ContainsKey(sectType);
        }

        // ===== 事件绑定 =====

        private void BindSectEvents(SectInstance sect)
        {
            if (sect == null) return;

            sect.OnRankChanged += HandleRankChanged;
            sect.OnContributionChanged += HandleContributionChanged;
            sect.OnReputationChanged += HandleReputationChanged;
            sect.OnMartialArtLearned += HandleMartialArtLearned;
        }

        private void UnbindSectEvents(SectInstance sect)
        {
            if (sect == null) return;

            sect.OnRankChanged -= HandleRankChanged;
            sect.OnContributionChanged -= HandleContributionChanged;
            sect.OnReputationChanged -= HandleReputationChanged;
            sect.OnMartialArtLearned -= HandleMartialArtLearned;
        }

        private void HandleRankChanged(SectRank oldRank, SectRank newRank)
        {
            OnRankChanged?.Invoke(new SectRankChangedEventArgs
            {
                CharacterId = _characterId,
                SectType = CurrentSectType,
                OldRank = oldRank,
                NewRank = newRank
            });

            string eventName = (int)newRank > (int)oldRank
                ? SectEvents.OnRankPromoted
                : SectEvents.OnRankDemoted;

            EventManager.Trigger(eventName, new SectRankChangedEventArgs
            {
                CharacterId = _characterId,
                SectType = CurrentSectType,
                OldRank = oldRank,
                NewRank = newRank
            });
        }

        private void HandleContributionChanged(int oldValue, int newValue)
        {
            // 已在 AddContribution/SpendContribution 中处理
        }

        private void HandleReputationChanged(int oldValue, int newValue)
        {
            EventManager.Trigger(SectEvents.OnReputationChanged, new
            {
                CharacterId = _characterId,
                SectType = CurrentSectType,
                OldValue = oldValue,
                NewValue = newValue
            });
        }

        private void HandleMartialArtLearned(string martialArtId)
        {
            // 已在 LearnSectMartialArt 中处理
        }

        // ===== 存档 =====

        /// <summary>
        /// 获取存档数据
        /// </summary>
        public SectManagerSaveData GetSaveData()
        {
            var historyData = new List<SectSaveData>();
            foreach (var kvp in _sectHistory)
            {
                historyData.Add(kvp.Value.ToSaveData());
            }

            return new SectManagerSaveData
            {
                characterId = _characterId,
                currentSectType = CurrentSectType,
                unlockedSects = _unlockedSects.ToArray(),
                sectHistory = historyData.ToArray()
            };
        }

        /// <summary>
        /// 加载存档数据
        /// </summary>
        public void LoadSaveData(SectManagerSaveData saveData)
        {
            if (saveData == null) return;

            _characterId = saveData.characterId;
            _unlockedSects = new List<SectType>(saveData.unlockedSects);

            // 恢复门派历史
            _sectHistory.Clear();
            foreach (var sectSave in saveData.sectHistory)
            {
                var sectData = _database?.GetSectData(sectSave.sectType);
                if (sectData != null)
                {
                    var instance = new SectInstance(sectSave, sectData);
                    _sectHistory[sectSave.sectType] = instance;
                }
            }

            // 恢复当前门派
            if (saveData.currentSectType != SectType.None &&
                _sectHistory.TryGetValue(saveData.currentSectType, out var currentInstance))
            {
                _currentSect = currentInstance;
                BindSectEvents(_currentSect);
            }
        }
    }

    /// <summary>
    /// 门派管理器存档数据
    /// </summary>
    [Serializable]
    public class SectManagerSaveData
    {
        public string characterId;
        public SectType currentSectType;
        public SectType[] unlockedSects;
        public SectSaveData[] sectHistory;
    }
}
