using UnityEngine;

namespace ZeroEngine.Spawner
{
    /// <summary>
    /// 时间条件 - 基于时间的生成条件
    /// </summary>
    public class TimeCondition : SpawnConditionBase
    {
        [Header("Time Settings")]
        [SerializeField] private TimeConditionType _conditionType = TimeConditionType.AfterDelay;
        [SerializeField] private float _targetTime = 5f;
        [SerializeField] private float _endTime = 10f; // 用于 WithinTimeRange
        [SerializeField] private bool _useUnscaledTime = false;

        [Header("Periodic Settings")]
        [SerializeField] private float _period = 10f;
        [SerializeField] private float _activeWindow = 5f; // 周期内激活窗口

        // 运行时状态
        private float _startTime;
        private bool _hasStarted;

        #region Properties

        /// <summary>条件类型</summary>
        public TimeConditionType ConditionType
        {
            get => _conditionType;
            set => _conditionType = value;
        }

        /// <summary>目标时间</summary>
        public float TargetTime
        {
            get => _targetTime;
            set => _targetTime = value;
        }

        /// <summary>已经过时间</summary>
        public float ElapsedTime => _hasStarted ? CurrentTime - _startTime : 0f;

        /// <summary>剩余时间</summary>
        public float RemainingTime => Mathf.Max(0f, _targetTime - ElapsedTime);

        private float CurrentTime => _useUnscaledTime ? Time.unscaledTime : Time.time;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            StartTimer();
        }

        #endregion

        #region Condition Logic

        protected override bool CheckCondition()
        {
            if (!_hasStarted) return false;

            float elapsed = ElapsedTime;

            return _conditionType switch
            {
                TimeConditionType.AfterDelay => elapsed >= _targetTime,
                TimeConditionType.BeforeDeadline => elapsed < _targetTime,
                TimeConditionType.WithinTimeRange => elapsed >= _targetTime && elapsed <= _endTime,
                TimeConditionType.Periodic => CheckPeriodic(elapsed),
                TimeConditionType.ExactTime => Mathf.Abs(elapsed - _targetTime) < Time.deltaTime,
                _ => false
            };
        }

        /// <summary>
        /// 检查周期条件
        /// </summary>
        private bool CheckPeriodic(float elapsed)
        {
            if (_period <= 0) return false;

            float positionInPeriod = elapsed % _period;
            return positionInPeriod < _activeWindow;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 开始计时
        /// </summary>
        public void StartTimer()
        {
            _startTime = CurrentTime;
            _hasStarted = true;
        }

        /// <summary>
        /// 停止计时
        /// </summary>
        public void StopTimer()
        {
            _hasStarted = false;
        }

        /// <summary>
        /// 重置计时器
        /// </summary>
        public override void ResetCondition()
        {
            StartTimer();
        }

        /// <summary>
        /// 设置延迟条件
        /// </summary>
        public void SetDelay(float delay)
        {
            _conditionType = TimeConditionType.AfterDelay;
            _targetTime = delay;
            StartTimer();
        }

        /// <summary>
        /// 设置时间范围条件
        /// </summary>
        public void SetTimeRange(float startTime, float endTime)
        {
            _conditionType = TimeConditionType.WithinTimeRange;
            _targetTime = startTime;
            _endTime = endTime;
            StartTimer();
        }

        /// <summary>
        /// 设置周期条件
        /// </summary>
        public void SetPeriodic(float period, float activeWindow)
        {
            _conditionType = TimeConditionType.Periodic;
            _period = period;
            _activeWindow = activeWindow;
            StartTimer();
        }

        public override string GetDescription()
        {
            return _conditionType switch
            {
                TimeConditionType.AfterDelay => $"After {_targetTime}s",
                TimeConditionType.BeforeDeadline => $"Before {_targetTime}s",
                TimeConditionType.WithinTimeRange => $"Between {_targetTime}s - {_endTime}s",
                TimeConditionType.Periodic => $"Every {_period}s for {_activeWindow}s",
                TimeConditionType.ExactTime => $"At {_targetTime}s",
                _ => base.GetDescription()
            };
        }

        public override float GetProgress()
        {
            if (!_hasStarted) return 0f;

            return _conditionType switch
            {
                TimeConditionType.AfterDelay => Mathf.Clamp01(ElapsedTime / _targetTime),
                TimeConditionType.BeforeDeadline => Mathf.Clamp01(1f - (ElapsedTime / _targetTime)),
                TimeConditionType.WithinTimeRange => ElapsedTime < _targetTime ?
                    Mathf.Clamp01(ElapsedTime / _targetTime) :
                    Mathf.Clamp01(1f - ((ElapsedTime - _targetTime) / (_endTime - _targetTime))),
                TimeConditionType.Periodic => _period > 0 ?
                    Mathf.Clamp01((ElapsedTime % _period) / _activeWindow) : 0f,
                _ => base.GetProgress()
            };
        }

        #endregion
    }

    /// <summary>
    /// 时间条件类型
    /// </summary>
    public enum TimeConditionType
    {
        /// <summary>延迟后满足</summary>
        AfterDelay,
        /// <summary>截止时间前满足</summary>
        BeforeDeadline,
        /// <summary>时间范围内满足</summary>
        WithinTimeRange,
        /// <summary>周期性满足</summary>
        Periodic,
        /// <summary>精确时间点满足</summary>
        ExactTime
    }
}
