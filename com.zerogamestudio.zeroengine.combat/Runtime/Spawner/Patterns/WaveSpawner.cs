using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Spawner
{
    /// <summary>
    /// 波次生成器 - 按波次配置生成敌人
    /// </summary>
    public class WaveSpawner : SpawnerBase
    {
        [Header("Wave Configuration")]
        [SerializeField] private List<WaveConfig> _waves = new();
        [SerializeField] private bool _autoStartNextWave = true;
        [SerializeField] private float _waveInterval = 5f;

        [Header("Wave Behavior")]
        [SerializeField] private bool _loopWaves = false;
        [SerializeField] private float _difficultyScalePerLoop = 1.2f;

        // 波次状态
        private int _currentWaveIndex = -1;
        private float _waveTimer;
        private float _waveDuration;
        private int _waveKillCount;
        private int _loopCount;
        private bool _isWaveActive;
        private bool _isWaitingForNextWave;

        #region Properties

        /// <summary>当前波次索引</summary>
        public int CurrentWaveIndex => _currentWaveIndex;

        /// <summary>当前波次配置</summary>
        public WaveConfig CurrentWave => _currentWaveIndex >= 0 && _currentWaveIndex < _waves.Count
            ? _waves[_currentWaveIndex] : null;

        /// <summary>总波次数</summary>
        public int TotalWaves => _waves.Count;

        /// <summary>是否为最后波次</summary>
        public bool IsLastWave => _currentWaveIndex >= _waves.Count - 1;

        /// <summary>波次进行中</summary>
        public bool IsWaveActive => _isWaveActive;

        /// <summary>循环次数</summary>
        public int LoopCount => _loopCount;

        /// <summary>当前难度系数</summary>
        public float CurrentDifficultyMultiplier =>
            Mathf.Pow(_difficultyScalePerLoop, _loopCount) * (CurrentWave?.DifficultyMultiplier ?? 1f);

        #endregion

        #region Unity Lifecycle

        protected override void Start()
        {
            // 不自动激活，等待 StartWaves 调用
            if (_activateOnStart)
            {
                StartWaves();
            }
        }

        #endregion

        #region Wave Control

        /// <summary>
        /// 开始波次
        /// </summary>
        public void StartWaves()
        {
            if (_waves.Count == 0)
            {
                Debug.LogWarning($"[WaveSpawner] No waves configured on {gameObject.name}");
                return;
            }

            _currentWaveIndex = -1;
            _loopCount = 0;
            _isActive = true;

            StartNextWave();
        }

        /// <summary>
        /// 开始下一波
        /// </summary>
        public void StartNextWave()
        {
            _currentWaveIndex++;

            // 检查循环
            if (_currentWaveIndex >= _waves.Count)
            {
                if (_loopWaves)
                {
                    _currentWaveIndex = 0;
                    _loopCount++;
                }
                else
                {
                    CompleteAllWaves();
                    return;
                }
            }

            var wave = _waves[_currentWaveIndex];

            // 设置波次数据
            if (wave.SpawnData != null)
            {
                _spawnData = wave.SpawnData;
            }

            _waveTimer = 0f;
            _waveDuration = 0f;
            _waveKillCount = 0;
            _spawnTimer = 0f;
            _totalSpawnCount = 0;
            _isWaveActive = true;
            _isWaitingForNextWave = false;

            // 发布波次开始事件
            SpawnEvents.InvokeWaveStart(new WaveStartEventArgs(
                this, _currentWaveIndex, wave, _waves.Count
            ));
        }

        /// <summary>
        /// 跳到指定波次
        /// </summary>
        public void SkipToWave(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= _waves.Count)
            {
                Debug.LogWarning($"[WaveSpawner] Invalid wave index: {waveIndex}");
                return;
            }

            // 清除当前实体
            ClearAllEntities(DespawnReason.Manual);

            _currentWaveIndex = waveIndex - 1; // StartNextWave 会 +1
            StartNextWave();
        }

        /// <summary>
        /// 完成当前波次
        /// </summary>
        private void CompleteCurrentWave()
        {
            if (!_isWaveActive) return;

            var wave = CurrentWave;
            _isWaveActive = false;

            // 发布波次结束事件
            SpawnEvents.InvokeWaveEnd(new WaveEndEventArgs(
                this, _currentWaveIndex, wave, IsLastWave, _waveKillCount, _waveDuration
            ));

            // 处理波次奖励
            if (wave?.Reward != null)
            {
                GrantWaveReward(wave.Reward);
            }

            // 自动开始下一波
            if (_autoStartNextWave && !IsLastWave)
            {
                _isWaitingForNextWave = true;
                _waveTimer = 0f;
            }
            else if (IsLastWave && !_loopWaves)
            {
                CompleteAllWaves();
            }
        }

        /// <summary>
        /// 完成所有波次
        /// </summary>
        private void CompleteAllWaves()
        {
            _isActive = false;
            _isWaveActive = false;

            SpawnEvents.InvokeAllCleared(new AllClearedEventArgs(
                this, _killCount, _totalSpawnCount, _runTime
            ));
        }

        /// <summary>
        /// 发放波次奖励
        /// </summary>
        protected virtual void GrantWaveReward(WaveReward reward)
        {
            // 子类可重写实现具体奖励逻辑
            // 示例：经验值、金币、物品掉落等
            Debug.Log($"[WaveSpawner] Wave {_currentWaveIndex + 1} reward: " +
                $"EXP={reward.Experience}, Gold={reward.Gold}, Items={reward.Items?.Count ?? 0}");
        }

        #endregion

        #region Spawn Logic

        protected override void UpdateSpawnLogic(float deltaTime)
        {
            // 等待下一波
            if (_isWaitingForNextWave)
            {
                _waveTimer += deltaTime;
                float interval = CurrentWave?.IntervalAfterWave ?? _waveInterval;

                if (_waveTimer >= interval)
                {
                    StartNextWave();
                }
                return;
            }

            if (!_isWaveActive) return;

            _waveDuration += deltaTime;
            var wave = CurrentWave;

            // 检查波次时间限制
            if (wave != null && wave.Duration > 0 && _waveDuration >= wave.Duration)
            {
                CompleteCurrentWave();
                return;
            }

            // 检查击杀完成条件
            if (wave != null && wave.RequiredKillCount > 0 && _waveKillCount >= wave.RequiredKillCount)
            {
                CompleteCurrentWave();
                return;
            }

            // 检查是否所有实体已清除 (且已达到生成限制)
            if (_spawnData != null && _spawnData.TotalSpawnLimit > 0 &&
                _totalSpawnCount >= _spawnData.TotalSpawnLimit && _activeEntities.Count == 0)
            {
                CompleteCurrentWave();
                return;
            }

            // 执行生成逻辑
            _spawnTimer -= deltaTime;
            if (_spawnTimer <= 0f && CanSpawn())
            {
                var entry = GetNextEntry();
                if (entry != null)
                {
                    int count = entry.GetActualCount();
                    for (int i = 0; i < count; i++)
                    {
                        DoSpawn(entry, transform.position, transform.rotation);
                    }
                }

                _spawnTimer = _spawnData?.GetActualInterval() ?? 1f;
            }
        }

        protected override void OnEntityKilled(GameObject entity)
        {
            base.OnEntityKilled(entity);
            _waveKillCount++;
        }

        protected override int GetCurrentWaveIndex() => _currentWaveIndex;

        #endregion

        #region Editor

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // 绘制波次信息
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"Waves: {_waves.Count}\nCurrent: {_currentWaveIndex + 1}");
            #endif
        }

        #endregion
    }
}
