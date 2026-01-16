// ============================================================================
// RealmInstance.cs
// 运行时境界实例
// 创建于: 2026-01-09
// ============================================================================

using System;
using UnityEngine;

namespace ZeroEngine.Character.Realm
{
    /// <summary>
    /// 运行时境界实例
    /// 记录角色的境界状态 (当前境界、修为、突破状态等)
    /// </summary>
    [Serializable]
    public class RealmInstance
    {
        /// <summary>当前境界</summary>
        public RealmType CurrentRealm { get; private set; }

        /// <summary>境界数据引用</summary>
        public RealmDataSO Data { get; private set; }

        /// <summary>当前修为</summary>
        public int Cultivation { get; private set; }

        /// <summary>累计修为 (历史总计)</summary>
        public int TotalCultivation { get; private set; }

        /// <summary>突破状态</summary>
        public BreakthroughStatus BreakthroughStatus { get; private set; }

        /// <summary>突破尝试次数</summary>
        public int BreakthroughAttempts { get; private set; }

        /// <summary>是否处于走火入魔状态</summary>
        public bool IsInDeviation { get; private set; }

        /// <summary>走火入魔剩余时间 (秒)</summary>
        public float DeviationTimeRemaining { get; private set; }

        /// <summary>境界等级 (数值)</summary>
        public int RealmLevel => RealmHelper.GetRealmLevel(CurrentRealm);

        /// <summary>境界分类</summary>
        public RealmCategory Category => RealmHelper.GetCategory(CurrentRealm);

        /// <summary>是否为最高境界</summary>
        public bool IsMaxRealm => RealmHelper.IsMaxRealm(CurrentRealm);

        /// <summary>是否可以尝试突破</summary>
        public bool CanAttemptBreakthrough =>
            !IsMaxRealm &&
            !IsInDeviation &&
            BreakthroughStatus != BreakthroughStatus.InProgress &&
            Data != null &&
            Cultivation >= Data.cultivationRequired;

        // ===== 事件 =====

        /// <summary>境界变化事件</summary>
        public event Action<RealmType, RealmType> OnRealmChanged;

        /// <summary>修为变化事件</summary>
        public event Action<int, int> OnCultivationChanged;

        /// <summary>突破状态变化事件</summary>
        public event Action<BreakthroughStatus> OnBreakthroughStatusChanged;

        /// <summary>走火入魔事件</summary>
        public event Action OnDeviationStarted;

        /// <summary>走火入魔恢复事件</summary>
        public event Action OnDeviationEnded;

        // ===== 构造 =====

        public RealmInstance(RealmDataSO data)
        {
            Data = data;
            CurrentRealm = data?.realmType ?? RealmType.Mortal_Beginner;
            Cultivation = 0;
            TotalCultivation = 0;
            BreakthroughStatus = BreakthroughStatus.Normal;
            BreakthroughAttempts = 0;
            IsInDeviation = false;
            DeviationTimeRemaining = 0;
        }

        /// <summary>
        /// 反序列化构造
        /// </summary>
        public RealmInstance(RealmSaveData saveData, RealmDataSO data)
        {
            Data = data;
            CurrentRealm = saveData.realm;
            Cultivation = saveData.cultivation;
            TotalCultivation = saveData.totalCultivation;
            BreakthroughStatus = saveData.breakthroughStatus;
            BreakthroughAttempts = saveData.breakthroughAttempts;
            IsInDeviation = saveData.isInDeviation;
            DeviationTimeRemaining = saveData.deviationTimeRemaining;
        }

        // ===== 修为操作 =====

        /// <summary>
        /// 增加修为
        /// </summary>
        public void AddCultivation(int amount)
        {
            if (amount <= 0 || IsInDeviation) return;

            // 应用修炼速度倍率
            if (Data != null)
            {
                amount = Mathf.RoundToInt(amount * Data.cultivationSpeedMultiplier);
            }

            int oldValue = Cultivation;
            Cultivation += amount;
            TotalCultivation += amount;

            OnCultivationChanged?.Invoke(oldValue, Cultivation);

            // 检查是否可以突破
            UpdateBreakthroughStatus();
        }

        /// <summary>
        /// 消耗修为
        /// </summary>
        public bool SpendCultivation(int amount)
        {
            if (amount <= 0 || Cultivation < amount) return false;

            int oldValue = Cultivation;
            Cultivation -= amount;

            OnCultivationChanged?.Invoke(oldValue, Cultivation);
            UpdateBreakthroughStatus();

            return true;
        }

        /// <summary>
        /// 更新突破状态
        /// </summary>
        private void UpdateBreakthroughStatus()
        {
            if (IsMaxRealm || IsInDeviation)
            {
                BreakthroughStatus = BreakthroughStatus.Normal;
                return;
            }

            var newStatus = CanAttemptBreakthrough
                ? BreakthroughStatus.Ready
                : BreakthroughStatus.Normal;

            if (newStatus != BreakthroughStatus)
            {
                BreakthroughStatus = newStatus;
                OnBreakthroughStatusChanged?.Invoke(BreakthroughStatus);
            }
        }

        // ===== 突破操作 =====

        /// <summary>
        /// 尝试突破
        /// </summary>
        /// <param name="bonusChance">额外成功率加成</param>
        /// <returns>突破结果</returns>
        public BreakthroughResult AttemptBreakthrough(int bonusChance = 0)
        {
            if (!CanAttemptBreakthrough)
                return BreakthroughResult.Failed;

            BreakthroughStatus = BreakthroughStatus.InProgress;
            OnBreakthroughStatusChanged?.Invoke(BreakthroughStatus);

            BreakthroughAttempts++;

            // 计算成功率
            int successChance = Data?.CalculateBreakthroughChance(bonusChance) ?? 50;
            int roll = UnityEngine.Random.Range(0, 100);

            BreakthroughResult result;

            if (roll < successChance)
            {
                // 成功
                result = roll < successChance / 2
                    ? BreakthroughResult.CriticalSuccess
                    : BreakthroughResult.Success;

                PerformBreakthrough();
            }
            else
            {
                // 失败
                result = HandleBreakthroughFailure();
            }

            // 重置状态
            BreakthroughStatus = BreakthroughStatus.Normal;
            UpdateBreakthroughStatus();

            return result;
        }

        /// <summary>
        /// 执行突破
        /// </summary>
        private void PerformBreakthrough()
        {
            var oldRealm = CurrentRealm;
            var newRealm = RealmHelper.GetNextRealm(CurrentRealm);

            if (newRealm == oldRealm) return; // 已是最高境界

            CurrentRealm = newRealm;
            Cultivation = 0; // 突破后修为清零
            BreakthroughAttempts = 0;

            OnRealmChanged?.Invoke(oldRealm, newRealm);
        }

        /// <summary>
        /// 处理突破失败
        /// </summary>
        private BreakthroughResult HandleBreakthroughFailure()
        {
            if (Data == null)
                return BreakthroughResult.Failed;

            // 检查走火入魔
            if (Data.canDeviate)
            {
                int deviationRoll = UnityEngine.Random.Range(0, 100);
                if (deviationRoll < Data.deviationChance)
                {
                    StartDeviation();
                    return BreakthroughResult.FailedWithDeviation;
                }
            }

            // 检查跌境
            if (Data.canRegress)
            {
                int regressionRoll = UnityEngine.Random.Range(0, 100);
                if (regressionRoll < Data.regressionChance)
                {
                    Regress();
                    return BreakthroughResult.FailedWithRegression;
                }
            }

            return BreakthroughResult.Failed;
        }

        /// <summary>
        /// 境界跌落
        /// </summary>
        public void Regress()
        {
            var oldRealm = CurrentRealm;
            var newRealm = RealmHelper.GetPreviousRealm(CurrentRealm);

            if (newRealm == oldRealm) return; // 已是最低境界

            CurrentRealm = newRealm;
            Cultivation = 0;

            OnRealmChanged?.Invoke(oldRealm, newRealm);
        }

        // ===== 走火入魔 =====

        /// <summary>
        /// 开始走火入魔
        /// </summary>
        public void StartDeviation(float duration = 3600f) // 默认 1 小时
        {
            if (IsInDeviation) return;

            IsInDeviation = true;
            DeviationTimeRemaining = duration;
            BreakthroughStatus = BreakthroughStatus.Deviation;

            OnDeviationStarted?.Invoke();
            OnBreakthroughStatusChanged?.Invoke(BreakthroughStatus);
        }

        /// <summary>
        /// 更新走火入魔状态
        /// </summary>
        public void UpdateDeviation(float deltaTime)
        {
            if (!IsInDeviation) return;

            DeviationTimeRemaining -= deltaTime;

            if (DeviationTimeRemaining <= 0)
            {
                EndDeviation();
            }
        }

        /// <summary>
        /// 结束走火入魔
        /// </summary>
        public void EndDeviation()
        {
            if (!IsInDeviation) return;

            IsInDeviation = false;
            DeviationTimeRemaining = 0;
            BreakthroughStatus = BreakthroughStatus.Normal;

            OnDeviationEnded?.Invoke();
            UpdateBreakthroughStatus();
        }

        // ===== 属性计算 =====

        /// <summary>
        /// 获取当前境界的属性倍率
        /// </summary>
        public RealmStatMultiplier GetCurrentStatMultiplier()
        {
            return Data?.statMultiplier ?? new RealmStatMultiplier();
        }

        /// <summary>
        /// 获取突破进度 (0-1)
        /// </summary>
        public float GetBreakthroughProgress()
        {
            if (Data == null || Data.cultivationRequired <= 0)
                return 0f;

            return Mathf.Clamp01((float)Cultivation / Data.cultivationRequired);
        }

        // ===== 数据更新 =====

        /// <summary>
        /// 更新境界数据引用
        /// </summary>
        public void UpdateData(RealmDataSO newData)
        {
            Data = newData;
            UpdateBreakthroughStatus();
        }

        // ===== 序列化 =====

        /// <summary>
        /// 转换为存档数据
        /// </summary>
        public RealmSaveData ToSaveData()
        {
            return new RealmSaveData
            {
                realm = CurrentRealm,
                cultivation = Cultivation,
                totalCultivation = TotalCultivation,
                breakthroughStatus = BreakthroughStatus,
                breakthroughAttempts = BreakthroughAttempts,
                isInDeviation = IsInDeviation,
                deviationTimeRemaining = DeviationTimeRemaining
            };
        }
    }

    /// <summary>
    /// 境界存档数据
    /// </summary>
    [Serializable]
    public class RealmSaveData
    {
        public RealmType realm;
        public int cultivation;
        public int totalCultivation;
        public BreakthroughStatus breakthroughStatus;
        public int breakthroughAttempts;
        public bool isInDeviation;
        public float deviationTimeRemaining;
    }
}
