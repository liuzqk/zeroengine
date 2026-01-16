using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Spawner
{
    /// <summary>
    /// 生成数据配置
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpawnData", menuName = "ZeroEngine/Spawner/Spawn Data")]
    public class SpawnDataSO : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("配置ID")]
        public string SpawnId;

        [Tooltip("显示名称")]
        public string DisplayName;

        [Header("Spawn Entries")]
        [Tooltip("生成条目列表")]
        public List<SpawnEntry> Entries = new();

        [Header("Spawn Settings")]
        [Tooltip("生成模式")]
        public SpawnMode Mode = SpawnMode.Sequential;

        [Tooltip("最大同时存在数量 (0=无限制)")]
        [Min(0)] public int MaxActiveCount = 0;

        [Tooltip("总生成数量限制 (0=无限制)")]
        [Min(0)] public int TotalSpawnLimit = 0;

        [Header("Timing")]
        [Tooltip("生成间隔 (秒)")]
        [Min(0)] public float SpawnInterval = 1f;

        [Tooltip("间隔随机变化范围")]
        [Min(0)] public float IntervalVariance = 0f;

        [Tooltip("首次生成延迟")]
        [Min(0)] public float InitialDelay = 0f;

        [Header("Position")]
        [Tooltip("位置偏移")]
        public Vector3 PositionOffset = Vector3.zero;

        [Tooltip("位置随机范围")]
        public Vector3 PositionRandomRange = Vector3.zero;

        [Tooltip("旋转偏移")]
        public Vector3 RotationOffset = Vector3.zero;

        [Tooltip("是否随机旋转Y轴")]
        public bool RandomYRotation = false;

        [Header("Pool")]
        [Tooltip("是否使用对象池")]
        public bool UsePooling = true;

        [Tooltip("预热池大小")]
        [Min(0)] public int PoolWarmupSize = 5;

        [Header("Events")]
        [Tooltip("生成时触发的事件Tag")]
        public string OnSpawnEventTag;

        [Tooltip("全部清除时触发的事件Tag")]
        public string OnAllClearedEventTag;

        /// <summary>
        /// 根据权重随机选择一个条目
        /// </summary>
        public SpawnEntry GetRandomEntry()
        {
            if (Entries == null || Entries.Count == 0) return null;

            float totalWeight = 0f;
            foreach (var entry in Entries)
            {
                if (entry.IsEnabled)
                {
                    totalWeight += entry.Weight;
                }
            }

            if (totalWeight <= 0) return Entries[0];

            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var entry in Entries)
            {
                if (!entry.IsEnabled) continue;

                cumulative += entry.Weight;
                if (random <= cumulative)
                {
                    return entry;
                }
            }

            return Entries[0];
        }

        /// <summary>
        /// 获取实际的生成间隔 (含随机变化)
        /// </summary>
        public float GetActualInterval()
        {
            if (IntervalVariance <= 0) return SpawnInterval;
            return SpawnInterval + Random.Range(-IntervalVariance, IntervalVariance);
        }

        /// <summary>
        /// 获取实际的生成位置 (含随机偏移)
        /// </summary>
        public Vector3 GetSpawnPosition(Vector3 basePosition)
        {
            Vector3 pos = basePosition + PositionOffset;

            if (PositionRandomRange != Vector3.zero)
            {
                pos.x += Random.Range(-PositionRandomRange.x, PositionRandomRange.x);
                pos.y += Random.Range(-PositionRandomRange.y, PositionRandomRange.y);
                pos.z += Random.Range(-PositionRandomRange.z, PositionRandomRange.z);
            }

            return pos;
        }

        /// <summary>
        /// 获取实际的生成旋转
        /// </summary>
        public Quaternion GetSpawnRotation(Quaternion baseRotation)
        {
            Quaternion rot = baseRotation * Quaternion.Euler(RotationOffset);

            if (RandomYRotation)
            {
                rot *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            return rot;
        }
    }

    /// <summary>
    /// 生成条目
    /// </summary>
    [System.Serializable]
    public class SpawnEntry
    {
        [Tooltip("是否启用")]
        public bool IsEnabled = true;

        [Tooltip("要生成的预制体")]
        public GameObject Prefab;

        [Tooltip("权重 (用于随机选择)")]
        [Min(0)] public float Weight = 1f;

        [Tooltip("每次生成的数量")]
        [Min(1)] public int SpawnCount = 1;

        [Tooltip("数量随机变化")]
        [Min(0)] public int CountVariance = 0;

        [Tooltip("生成时的缩放")]
        public Vector3 Scale = Vector3.one;

        [Tooltip("缩放随机变化")]
        [Min(0)] public float ScaleVariance = 0f;

        [Tooltip("自定义标签")]
        public string Tag;

        /// <summary>
        /// 获取实际生成数量
        /// </summary>
        public int GetActualCount()
        {
            if (CountVariance <= 0) return SpawnCount;
            return SpawnCount + Random.Range(-CountVariance, CountVariance + 1);
        }

        /// <summary>
        /// 获取实际缩放
        /// </summary>
        public Vector3 GetActualScale()
        {
            if (ScaleVariance <= 0) return Scale;

            float variance = Random.Range(-ScaleVariance, ScaleVariance);
            return Scale * (1f + variance);
        }
    }

    /// <summary>
    /// 生成模式
    /// </summary>
    public enum SpawnMode
    {
        /// <summary>按顺序生成</summary>
        Sequential,

        /// <summary>随机生成 (基于权重)</summary>
        Random,

        /// <summary>同时生成所有</summary>
        Burst,

        /// <summary>循环生成</summary>
        Loop
    }

    /// <summary>
    /// 波次配置
    /// </summary>
    [System.Serializable]
    public class WaveConfig
    {
        [Tooltip("波次名称")]
        public string WaveName;

        [Tooltip("波次描述")]
        [TextArea(1, 2)]
        public string Description;

        [Tooltip("波次使用的生成数据")]
        public SpawnDataSO SpawnData;

        [Tooltip("波次持续时间 (0=直到全部清除)")]
        [Min(0)] public float Duration = 0f;

        [Tooltip("波次间隔")]
        [Min(0)] public float IntervalAfterWave = 3f;

        [Tooltip("完成条件: 击杀数量 (0=不检查)")]
        [Min(0)] public int RequiredKillCount = 0;

        [Tooltip("波次奖励")]
        public WaveReward Reward;

        [Tooltip("波次难度系数")]
        [Min(0.1f)] public float DifficultyMultiplier = 1f;
    }

    /// <summary>
    /// 波次奖励
    /// </summary>
    [System.Serializable]
    public class WaveReward
    {
        [Tooltip("经验值")]
        public int Experience;

        [Tooltip("金币")]
        public int Gold;

        [Tooltip("奖励物品")]
        public List<RewardItem> Items = new();
    }

    /// <summary>
    /// 奖励物品
    /// </summary>
    [System.Serializable]
    public class RewardItem
    {
        public Inventory.InventoryItemSO Item;
        public int Amount = 1;
        public float DropChance = 1f;
    }
}
