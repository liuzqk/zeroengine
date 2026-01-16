// ============================================================================
// RealmManager.cs
// 境界管理器 (MonoSingleton + ISaveable)
// 创建于: 2026-01-09
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Core;

namespace ZeroEngine.Character.Realm
{
    /// <summary>
    /// 境界管理器
    /// 管理角色境界、修为、突破
    /// </summary>
    public class RealmManager : MonoSingleton<RealmManager>
    {
        [Header("配置")]
        [Tooltip("境界数据库")]
        [SerializeField]
        private RealmDatabaseSO _database;

        [Tooltip("初始境界")]
        [SerializeField]
        private RealmType _initialRealm = RealmType.Mortal_Beginner;

        [Header("运行时数据")]
        [SerializeField]
        private string _characterId = "player";

        private RealmInstance _realmInstance;

        // ===== 事件 =====

        /// <summary>境界变化事件</summary>
        public event Action<RealmBreakthroughEventArgs> OnRealmChanged;

        /// <summary>修为变化事件</summary>
        public event Action<CultivationChangedEventArgs> OnCultivationChanged;

        /// <summary>突破尝试事件</summary>
        public event Action<BreakthroughAttemptEventArgs> OnBreakthroughAttempt;

        /// <summary>走火入魔事件</summary>
        public event Action<DeviationEventArgs> OnDeviationStarted;

        // ===== 属性 =====

        /// <summary>境界数据库</summary>
        public RealmDatabaseSO Database => _database;

        /// <summary>当前境界实例</summary>
        public RealmInstance CurrentInstance => _realmInstance;

        /// <summary>当前境界</summary>
        public RealmType CurrentRealm => _realmInstance?.CurrentRealm ?? RealmType.None;

        /// <summary>当前修为</summary>
        public int Cultivation => _realmInstance?.Cultivation ?? 0;

        /// <summary>境界等级</summary>
        public int RealmLevel => _realmInstance?.RealmLevel ?? 0;

        /// <summary>是否可以突破</summary>
        public bool CanBreakthrough => _realmInstance?.CanAttemptBreakthrough ?? false;

        /// <summary>是否走火入魔中</summary>
        public bool IsInDeviation => _realmInstance?.IsInDeviation ?? false;

        /// <summary>突破进度 (0-1)</summary>
        public float BreakthroughProgress => _realmInstance?.GetBreakthroughProgress() ?? 0f;

        // ===== 初始化 =====

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void Initialize()
        {
            if (_realmInstance != null) return;

            var initialData = _database?.GetRealmData(_initialRealm);
            _realmInstance = new RealmInstance(initialData);

            BindEvents();
        }

        private void Update()
        {
            // 更新走火入魔状态
            _realmInstance?.UpdateDeviation(Time.deltaTime);
        }

        // ===== 修为操作 =====

        /// <summary>
        /// 增加修为
        /// </summary>
        public void AddCultivation(int amount, CultivationSource source = CultivationSource.Other)
        {
            if (_realmInstance == null || amount <= 0) return;

            int oldValue = _realmInstance.Cultivation;
            _realmInstance.AddCultivation(amount);

            var eventArgs = new CultivationChangedEventArgs
            {
                CharacterId = _characterId,
                OldValue = oldValue,
                NewValue = _realmInstance.Cultivation,
                Source = source
            };

            OnCultivationChanged?.Invoke(eventArgs);
            EventManager.Trigger(RealmEvents.OnCultivationChanged, eventArgs);
        }

        /// <summary>
        /// 消耗修为
        /// </summary>
        public bool SpendCultivation(int amount)
        {
            if (_realmInstance == null) return false;

            int oldValue = _realmInstance.Cultivation;
            if (!_realmInstance.SpendCultivation(amount))
                return false;

            var eventArgs = new CultivationChangedEventArgs
            {
                CharacterId = _characterId,
                OldValue = oldValue,
                NewValue = _realmInstance.Cultivation,
                Source = CultivationSource.Consumption
            };

            OnCultivationChanged?.Invoke(eventArgs);
            EventManager.Trigger(RealmEvents.OnCultivationChanged, eventArgs);

            return true;
        }

        // ===== 突破操作 =====

        /// <summary>
        /// 尝试突破
        /// </summary>
        public BreakthroughResult AttemptBreakthrough(int bonusChance = 0)
        {
            if (_realmInstance == null || !CanBreakthrough)
                return BreakthroughResult.Failed;

            var oldRealm = CurrentRealm;
            var targetRealm = RealmHelper.GetNextRealm(oldRealm);
            int successChance = _realmInstance.Data?.CalculateBreakthroughChance(bonusChance) ?? 50;

            var result = _realmInstance.AttemptBreakthrough(bonusChance);

            // 触发突破尝试事件
            var attemptArgs = new BreakthroughAttemptEventArgs
            {
                CharacterId = _characterId,
                CurrentRealm = oldRealm,
                TargetRealm = targetRealm,
                SuccessChance = successChance,
                Result = result
            };

            OnBreakthroughAttempt?.Invoke(attemptArgs);
            EventManager.Trigger(RealmEvents.OnBreakthroughAttempt, attemptArgs);

            // 如果成功，触发境界变化事件
            if (result == BreakthroughResult.Success || result == BreakthroughResult.CriticalSuccess)
            {
                // 更新境界数据引用
                var newData = _database?.GetRealmData(_realmInstance.CurrentRealm);
                _realmInstance.UpdateData(newData);

                var breakthroughArgs = new RealmBreakthroughEventArgs
                {
                    CharacterId = _characterId,
                    OldRealm = oldRealm,
                    NewRealm = _realmInstance.CurrentRealm,
                    Result = result,
                    Attempts = _realmInstance.BreakthroughAttempts
                };

                OnRealmChanged?.Invoke(breakthroughArgs);
                EventManager.Trigger(RealmEvents.OnRealmBreakthrough, breakthroughArgs);

                Debug.Log($"[RealmManager] {_characterId} 突破成功: {oldRealm} -> {_realmInstance.CurrentRealm}");
            }
            else if (result == BreakthroughResult.FailedWithDeviation)
            {
                var deviationArgs = new DeviationEventArgs
                {
                    CharacterId = _characterId,
                    CurrentRealm = CurrentRealm,
                    Duration = _realmInstance.DeviationTimeRemaining,
                    Cause = DeviationCause.BreakthroughFailed
                };

                OnDeviationStarted?.Invoke(deviationArgs);
                EventManager.Trigger(RealmEvents.OnDeviationStarted, deviationArgs);

                Debug.LogWarning($"[RealmManager] {_characterId} 突破失败，走火入魔!");
            }
            else if (result == BreakthroughResult.FailedWithRegression)
            {
                // 更新境界数据引用
                var newData = _database?.GetRealmData(_realmInstance.CurrentRealm);
                _realmInstance.UpdateData(newData);

                EventManager.Trigger(RealmEvents.OnRealmRegression, new RealmBreakthroughEventArgs
                {
                    CharacterId = _characterId,
                    OldRealm = oldRealm,
                    NewRealm = _realmInstance.CurrentRealm,
                    Result = result
                });

                Debug.LogWarning($"[RealmManager] {_characterId} 突破失败，境界跌落: {oldRealm} -> {_realmInstance.CurrentRealm}");
            }

            return result;
        }

        /// <summary>
        /// 强制设置境界 (GM/调试用)
        /// </summary>
        public void SetRealm(RealmType realm)
        {
            if (_realmInstance == null) return;

            var oldRealm = CurrentRealm;
            var newData = _database?.GetRealmData(realm);

            // 创建新实例
            _realmInstance = new RealmInstance(newData);
            BindEvents();

            if (oldRealm != realm)
            {
                var eventArgs = new RealmBreakthroughEventArgs
                {
                    CharacterId = _characterId,
                    OldRealm = oldRealm,
                    NewRealm = realm,
                    Result = BreakthroughResult.Success,
                    Attempts = 0
                };

                OnRealmChanged?.Invoke(eventArgs);
            }

            Debug.Log($"[RealmManager] 强制设置境界: {realm}");
        }

        // ===== 走火入魔 =====

        /// <summary>
        /// 治疗走火入魔
        /// </summary>
        public bool CureDeviation()
        {
            if (_realmInstance == null || !IsInDeviation)
                return false;

            _realmInstance.EndDeviation();

            EventManager.Trigger(RealmEvents.OnDeviationEnded, new
            {
                CharacterId = _characterId,
                CurrentRealm = CurrentRealm
            });

            Debug.Log($"[RealmManager] {_characterId} 走火入魔已治愈");
            return true;
        }

        // ===== 属性计算 =====

        /// <summary>
        /// 获取当前境界的属性倍率
        /// </summary>
        public RealmStatMultiplier GetCurrentStatMultiplier()
        {
            return _realmInstance?.GetCurrentStatMultiplier() ?? new RealmStatMultiplier();
        }

        /// <summary>
        /// 检查境界是否满足要求
        /// </summary>
        public bool MeetsRealmRequirement(RealmType requiredRealm)
        {
            return RealmLevel >= RealmHelper.GetRealmLevel(requiredRealm);
        }

        /// <summary>
        /// 检查境界是否满足等级要求
        /// </summary>
        public bool MeetsRealmLevelRequirement(int requiredLevel)
        {
            return RealmLevel >= requiredLevel;
        }

        // ===== 事件绑定 =====

        private void BindEvents()
        {
            if (_realmInstance == null) return;

            _realmInstance.OnRealmChanged += HandleRealmChanged;
            _realmInstance.OnCultivationChanged += HandleCultivationChanged;
            _realmInstance.OnDeviationStarted += HandleDeviationStarted;
            _realmInstance.OnDeviationEnded += HandleDeviationEnded;
        }

        private void HandleRealmChanged(RealmType oldRealm, RealmType newRealm)
        {
            // 已在 AttemptBreakthrough 中处理
        }

        private void HandleCultivationChanged(int oldValue, int newValue)
        {
            // 已在 AddCultivation/SpendCultivation 中处理
        }

        private void HandleDeviationStarted()
        {
            // 已在 AttemptBreakthrough 中处理
        }

        private void HandleDeviationEnded()
        {
            EventManager.Trigger(RealmEvents.OnDeviationEnded, new
            {
                CharacterId = _characterId,
                CurrentRealm = CurrentRealm
            });
        }

        // ===== 存档 =====

        /// <summary>
        /// 获取存档数据
        /// </summary>
        public RealmManagerSaveData GetSaveData()
        {
            return new RealmManagerSaveData
            {
                characterId = _characterId,
                realmData = _realmInstance?.ToSaveData()
            };
        }

        /// <summary>
        /// 加载存档数据
        /// </summary>
        public void LoadSaveData(RealmManagerSaveData saveData)
        {
            if (saveData == null) return;

            _characterId = saveData.characterId;

            if (saveData.realmData != null)
            {
                var realmData = _database?.GetRealmData(saveData.realmData.realm);
                _realmInstance = new RealmInstance(saveData.realmData, realmData);
                BindEvents();
            }
        }
    }

    /// <summary>
    /// 境界管理器存档数据
    /// </summary>
    [Serializable]
    public class RealmManagerSaveData
    {
        public string characterId;
        public RealmSaveData realmData;
    }
}
