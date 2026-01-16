using UnityEngine;

namespace ZeroEngine.Spawner
{
    /// <summary>
    /// 击杀计数条件 - 基于击杀数量的生成条件
    /// </summary>
    public class KillCountCondition : SpawnConditionBase
    {
        [Header("Kill Count Settings")]
        [SerializeField] private KillCountType _conditionType = KillCountType.MinimumKills;
        [SerializeField] private int _targetKillCount = 10;
        [SerializeField] private int _maxKillCount = 20; // 用于 WithinRange

        [Header("Source")]
        [SerializeField] private KillCountSource _source = KillCountSource.Spawner;
        [SerializeField] private SpawnerBase _targetSpawner;
        [SerializeField] private string _targetTag = "";

        // 运行时状态
        private int _killCount;
        private SpawnerBase _attachedSpawner;

        #region Properties

        /// <summary>当前击杀数</summary>
        public int KillCount => GetCurrentKillCount();

        /// <summary>目标击杀数</summary>
        public int TargetKillCount
        {
            get => _targetKillCount;
            set => _targetKillCount = value;
        }

        /// <summary>剩余击杀数</summary>
        public int RemainingKills => Mathf.Max(0, _targetKillCount - KillCount);

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // 获取附加的生成器
            _attachedSpawner = GetComponent<SpawnerBase>();

            // 订阅击杀事件
            if (_source == KillCountSource.Global)
            {
                SpawnEvents.OnEntityDespawned += OnEntityDespawned;
            }
        }

        private void OnDestroy()
        {
            SpawnEvents.OnEntityDespawned -= OnEntityDespawned;
        }

        #endregion

        #region Condition Logic

        protected override bool CheckCondition()
        {
            int kills = GetCurrentKillCount();

            return _conditionType switch
            {
                KillCountType.MinimumKills => kills >= _targetKillCount,
                KillCountType.MaximumKills => kills <= _targetKillCount,
                KillCountType.ExactKills => kills == _targetKillCount,
                KillCountType.WithinRange => kills >= _targetKillCount && kills <= _maxKillCount,
                KillCountType.MultipleOf => _targetKillCount > 0 && kills > 0 && kills % _targetKillCount == 0,
                _ => false
            };
        }

        /// <summary>
        /// 获取当前击杀数
        /// </summary>
        private int GetCurrentKillCount()
        {
            return _source switch
            {
                KillCountSource.Spawner => GetSpawnerKillCount(),
                KillCountSource.Manual => _killCount,
                KillCountSource.Global => _killCount,
                _ => 0
            };
        }

        /// <summary>
        /// 获取生成器击杀数
        /// </summary>
        private int GetSpawnerKillCount()
        {
            var spawner = _targetSpawner != null ? _targetSpawner : _attachedSpawner;
            return spawner != null ? spawner.KillCount : 0;
        }

        /// <summary>
        /// 实体销毁事件处理
        /// </summary>
        private void OnEntityDespawned(DespawnEventArgs args)
        {
            if (args.Reason != DespawnReason.Killed) return;

            // 检查标签
            if (!string.IsNullOrEmpty(_targetTag) &&
                args.Entity != null &&
                !args.Entity.CompareTag(_targetTag))
            {
                return;
            }

            _killCount++;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 增加击杀数
        /// </summary>
        public void AddKill(int count = 1)
        {
            _killCount += count;
        }

        /// <summary>
        /// 设置击杀数
        /// </summary>
        public void SetKillCount(int count)
        {
            _killCount = count;
        }

        /// <summary>
        /// 重置击杀数
        /// </summary>
        public override void ResetCondition()
        {
            _killCount = 0;
        }

        /// <summary>
        /// 设置目标生成器
        /// </summary>
        public void SetTargetSpawner(SpawnerBase spawner)
        {
            _targetSpawner = spawner;
            _source = KillCountSource.Spawner;
        }

        public override string GetDescription()
        {
            return _conditionType switch
            {
                KillCountType.MinimumKills => $"Kill at least {_targetKillCount}",
                KillCountType.MaximumKills => $"Kill at most {_targetKillCount}",
                KillCountType.ExactKills => $"Kill exactly {_targetKillCount}",
                KillCountType.WithinRange => $"Kills between {_targetKillCount} - {_maxKillCount}",
                KillCountType.MultipleOf => $"Kills multiple of {_targetKillCount}",
                _ => base.GetDescription()
            };
        }

        public override float GetProgress()
        {
            if (_targetKillCount <= 0) return 1f;

            return _conditionType switch
            {
                KillCountType.MinimumKills => Mathf.Clamp01((float)KillCount / _targetKillCount),
                KillCountType.MaximumKills => Mathf.Clamp01(1f - ((float)KillCount / _targetKillCount)),
                KillCountType.ExactKills => KillCount == _targetKillCount ? 1f :
                    Mathf.Clamp01((float)KillCount / _targetKillCount),
                KillCountType.WithinRange => Mathf.Clamp01((float)(KillCount - _targetKillCount) /
                    (_maxKillCount - _targetKillCount)),
                _ => base.GetProgress()
            };
        }

        #endregion
    }

    /// <summary>
    /// 击杀计数类型
    /// </summary>
    public enum KillCountType
    {
        /// <summary>最少击杀数</summary>
        MinimumKills,
        /// <summary>最多击杀数</summary>
        MaximumKills,
        /// <summary>精确击杀数</summary>
        ExactKills,
        /// <summary>击杀数范围</summary>
        WithinRange,
        /// <summary>击杀数是某数的倍数</summary>
        MultipleOf
    }

    /// <summary>
    /// 击杀计数来源
    /// </summary>
    public enum KillCountSource
    {
        /// <summary>从生成器获取</summary>
        Spawner,
        /// <summary>手动设置</summary>
        Manual,
        /// <summary>全局统计</summary>
        Global
    }
}
