using System;
using UnityEngine;

namespace ZeroEngine.Spawner
{
    /// <summary>
    /// 生成系统事件
    /// </summary>
    public static class SpawnEvents
    {
        /// <summary>实体生成时</summary>
        public static event Action<SpawnEventArgs> OnEntitySpawned;

        /// <summary>实体销毁时</summary>
        public static event Action<DespawnEventArgs> OnEntityDespawned;

        /// <summary>波次开始时</summary>
        public static event Action<WaveStartEventArgs> OnWaveStarted;

        /// <summary>波次结束时</summary>
        public static event Action<WaveEndEventArgs> OnWaveEnded;

        /// <summary>生成器激活时</summary>
        public static event Action<SpawnerActivateEventArgs> OnSpawnerActivated;

        /// <summary>生成器停用时</summary>
        public static event Action<SpawnerDeactivateEventArgs> OnSpawnerDeactivated;

        /// <summary>所有实体清除时</summary>
        public static event Action<AllClearedEventArgs> OnAllCleared;

        #region Invoke Methods

        internal static void InvokeSpawn(SpawnEventArgs args) => OnEntitySpawned?.Invoke(args);
        internal static void InvokeDespawn(DespawnEventArgs args) => OnEntityDespawned?.Invoke(args);
        internal static void InvokeWaveStart(WaveStartEventArgs args) => OnWaveStarted?.Invoke(args);
        internal static void InvokeWaveEnd(WaveEndEventArgs args) => OnWaveEnded?.Invoke(args);
        internal static void InvokeSpawnerActivate(SpawnerActivateEventArgs args) => OnSpawnerActivated?.Invoke(args);
        internal static void InvokeSpawnerDeactivate(SpawnerDeactivateEventArgs args) => OnSpawnerDeactivated?.Invoke(args);
        internal static void InvokeAllCleared(AllClearedEventArgs args) => OnAllCleared?.Invoke(args);

        #endregion
    }

    /// <summary>
    /// 生成事件参数
    /// </summary>
    public readonly struct SpawnEventArgs
    {
        /// <summary>生成器</summary>
        public readonly SpawnerBase Spawner;

        /// <summary>生成的实体</summary>
        public readonly GameObject Entity;

        /// <summary>生成位置</summary>
        public readonly Vector3 Position;

        /// <summary>生成旋转</summary>
        public readonly Quaternion Rotation;

        /// <summary>使用的生成条目</summary>
        public readonly SpawnEntry Entry;

        /// <summary>当前波次索引</summary>
        public readonly int WaveIndex;

        /// <summary>生成数据配置</summary>
        public readonly SpawnDataSO SpawnData;

        public SpawnEventArgs(
            SpawnerBase spawner,
            GameObject entity,
            Vector3 position,
            Quaternion rotation,
            SpawnEntry entry,
            int waveIndex,
            SpawnDataSO spawnData)
        {
            Spawner = spawner;
            Entity = entity;
            Position = position;
            Rotation = rotation;
            Entry = entry;
            WaveIndex = waveIndex;
            SpawnData = spawnData;
        }
    }

    /// <summary>
    /// 销毁事件参数
    /// </summary>
    public readonly struct DespawnEventArgs
    {
        /// <summary>生成器</summary>
        public readonly SpawnerBase Spawner;

        /// <summary>销毁的实体</summary>
        public readonly GameObject Entity;

        /// <summary>销毁原因</summary>
        public readonly DespawnReason Reason;

        /// <summary>击杀者 (如果是被击杀)</summary>
        public readonly GameObject Killer;

        public DespawnEventArgs(
            SpawnerBase spawner,
            GameObject entity,
            DespawnReason reason,
            GameObject killer = null)
        {
            Spawner = spawner;
            Entity = entity;
            Reason = reason;
            Killer = killer;
        }
    }

    /// <summary>
    /// 销毁原因
    /// </summary>
    public enum DespawnReason
    {
        /// <summary>被击杀</summary>
        Killed,

        /// <summary>手动销毁</summary>
        Manual,

        /// <summary>生命周期结束</summary>
        Lifetime,

        /// <summary>生成器停用</summary>
        SpawnerDeactivated,

        /// <summary>超出范围</summary>
        OutOfRange,

        /// <summary>场景卸载</summary>
        SceneUnload
    }

    /// <summary>
    /// 波次开始事件参数
    /// </summary>
    public readonly struct WaveStartEventArgs
    {
        /// <summary>生成器</summary>
        public readonly SpawnerBase Spawner;

        /// <summary>波次索引</summary>
        public readonly int WaveIndex;

        /// <summary>波次配置</summary>
        public readonly WaveConfig WaveConfig;

        /// <summary>总波次数</summary>
        public readonly int TotalWaves;

        public WaveStartEventArgs(
            SpawnerBase spawner,
            int waveIndex,
            WaveConfig waveConfig,
            int totalWaves)
        {
            Spawner = spawner;
            WaveIndex = waveIndex;
            WaveConfig = waveConfig;
            TotalWaves = totalWaves;
        }
    }

    /// <summary>
    /// 波次结束事件参数
    /// </summary>
    public readonly struct WaveEndEventArgs
    {
        /// <summary>生成器</summary>
        public readonly SpawnerBase Spawner;

        /// <summary>波次索引</summary>
        public readonly int WaveIndex;

        /// <summary>波次配置</summary>
        public readonly WaveConfig WaveConfig;

        /// <summary>是否为最后波次</summary>
        public readonly bool IsLastWave;

        /// <summary>波次击杀数</summary>
        public readonly int KillCount;

        /// <summary>波次持续时间</summary>
        public readonly float Duration;

        public WaveEndEventArgs(
            SpawnerBase spawner,
            int waveIndex,
            WaveConfig waveConfig,
            bool isLastWave,
            int killCount,
            float duration)
        {
            Spawner = spawner;
            WaveIndex = waveIndex;
            WaveConfig = waveConfig;
            IsLastWave = isLastWave;
            KillCount = killCount;
            Duration = duration;
        }
    }

    /// <summary>
    /// 生成器激活事件参数
    /// </summary>
    public readonly struct SpawnerActivateEventArgs
    {
        /// <summary>生成器</summary>
        public readonly SpawnerBase Spawner;

        /// <summary>生成数据配置</summary>
        public readonly SpawnDataSO SpawnData;

        public SpawnerActivateEventArgs(SpawnerBase spawner, SpawnDataSO spawnData)
        {
            Spawner = spawner;
            SpawnData = spawnData;
        }
    }

    /// <summary>
    /// 生成器停用事件参数
    /// </summary>
    public readonly struct SpawnerDeactivateEventArgs
    {
        /// <summary>生成器</summary>
        public readonly SpawnerBase Spawner;

        /// <summary>停用原因</summary>
        public readonly string Reason;

        /// <summary>总生成数量</summary>
        public readonly int TotalSpawned;

        /// <summary>运行时长</summary>
        public readonly float RunTime;

        public SpawnerDeactivateEventArgs(
            SpawnerBase spawner,
            string reason,
            int totalSpawned,
            float runTime)
        {
            Spawner = spawner;
            Reason = reason;
            TotalSpawned = totalSpawned;
            RunTime = runTime;
        }
    }

    /// <summary>
    /// 全部清除事件参数
    /// </summary>
    public readonly struct AllClearedEventArgs
    {
        /// <summary>生成器</summary>
        public readonly SpawnerBase Spawner;

        /// <summary>总击杀数</summary>
        public readonly int TotalKillCount;

        /// <summary>总生成数</summary>
        public readonly int TotalSpawnCount;

        /// <summary>运行时长</summary>
        public readonly float TotalTime;

        public AllClearedEventArgs(
            SpawnerBase spawner,
            int totalKillCount,
            int totalSpawnCount,
            float totalTime)
        {
            Spawner = spawner;
            TotalKillCount = totalKillCount;
            TotalSpawnCount = totalSpawnCount;
            TotalTime = totalTime;
        }
    }
}
