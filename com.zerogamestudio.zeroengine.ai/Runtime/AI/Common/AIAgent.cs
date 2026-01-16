using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.AI
{
    /// <summary>
    /// AI Agent - AI 系统的主入口组件
    /// 管理多个 AI 大脑的协作和切换
    /// </summary>
    public class AIAgent : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Agent Settings")]
        [SerializeField] private bool _activateOnStart = true;
        [SerializeField] private float _decisionInterval = 0.1f;
        [SerializeField] private bool _pauseWhenInvisible = true;

        [Header("Debug")]
        [SerializeField] private bool _enableDebug = false;

        #endregion

        #region Runtime State

        private AIContext _context;
        private readonly List<IAIBrain> _brains = new();
        private IAIBrain _activeBrain;
        private float _decisionTimer;
        private bool _isActive;
        private bool _isVisible = true;

        #endregion

        #region Properties

        /// <summary>AI 上下文</summary>
        public AIContext Context => _context;

        /// <summary>黑板快捷访问</summary>
        public AIBlackboard Blackboard => _context?.Blackboard;

        /// <summary>是否激活</summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value) return;
                _isActive = value;

                if (_isActive)
                {
                    OnActivate();
                }
                else
                {
                    OnDeactivate();
                }
            }
        }

        /// <summary>当前活跃的大脑</summary>
        public IAIBrain ActiveBrain => _activeBrain;

        /// <summary>当前行动名称</summary>
        public string CurrentActionName => _activeBrain?.CurrentActionName ?? "None";

        /// <summary>决策间隔</summary>
        public float DecisionInterval
        {
            get => _decisionInterval;
            set => _decisionInterval = Mathf.Max(0.01f, value);
        }

        #endregion

        #region Events

        /// <summary>大脑切换事件</summary>
        public event Action<IAIBrain, IAIBrain> OnBrainChanged;

        /// <summary>激活状态变更事件</summary>
        public event Action<bool> OnActiveStateChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _context = new AIContext(this);
        }

        private void Start()
        {
            // 收集并初始化所有大脑组件
            CollectBrains();

            if (_activateOnStart)
            {
                IsActive = true;
            }
        }

        private void Update()
        {
            if (!_isActive) return;
            if (_pauseWhenInvisible && !_isVisible) return;

            float deltaTime = Time.deltaTime;

            // 决策定时器
            _decisionTimer -= deltaTime;
            if (_decisionTimer <= 0f)
            {
                _decisionTimer = _decisionInterval;
                UpdateDecision();
            }

            // 更新活跃大脑
            _activeBrain?.Tick(deltaTime);
        }

        private void OnBecameVisible()
        {
            _isVisible = true;
        }

        private void OnBecameInvisible()
        {
            _isVisible = false;
        }

        private void OnDestroy()
        {
            _brains.Clear();
            _activeBrain = null;
        }

        #endregion

        #region Brain Management

        /// <summary>
        /// 收集所有 AI 大脑组件
        /// </summary>
        private void CollectBrains()
        {
            _brains.Clear();

            // 获取所有实现 IAIBrain 的组件
            var brainComponents = GetComponents<MonoBehaviour>();
            foreach (var component in brainComponents)
            {
                if (component is IAIBrain brain && component != this)
                {
                    RegisterBrain(brain);
                }
            }

            if (_enableDebug)
            {
                Debug.Log($"[AIAgent] Collected {_brains.Count} brains on {gameObject.name}");
            }
        }

        /// <summary>
        /// 注册 AI 大脑
        /// </summary>
        public void RegisterBrain(IAIBrain brain)
        {
            if (brain == null || _brains.Contains(brain)) return;

            brain.Initialize(_context);
            _brains.Add(brain);

            if (_enableDebug)
            {
                Debug.Log($"[AIAgent] Registered brain: {brain.GetType().Name}");
            }
        }

        /// <summary>
        /// 注销 AI 大脑
        /// </summary>
        public void UnregisterBrain(IAIBrain brain)
        {
            if (brain == null) return;

            if (_activeBrain == brain)
            {
                SetActiveBrain(null);
            }

            _brains.Remove(brain);
        }

        /// <summary>
        /// 设置活跃大脑
        /// </summary>
        public void SetActiveBrain(IAIBrain brain)
        {
            if (_activeBrain == brain) return;

            var previousBrain = _activeBrain;

            // 停止当前大脑
            if (_activeBrain != null)
            {
                _activeBrain.StopCurrentAction();
                _activeBrain.IsActive = false;
            }

            _activeBrain = brain;

            // 激活新大脑
            if (_activeBrain != null)
            {
                _activeBrain.IsActive = true;
            }

            OnBrainChanged?.Invoke(previousBrain, _activeBrain);

            if (_enableDebug)
            {
                Debug.Log($"[AIAgent] Brain changed: {previousBrain?.GetType().Name ?? "None"} -> {_activeBrain?.GetType().Name ?? "None"}");
            }
        }

        /// <summary>
        /// 通过类型获取大脑
        /// </summary>
        public T GetBrain<T>() where T : class, IAIBrain
        {
            foreach (var brain in _brains)
            {
                if (brain is T typedBrain)
                {
                    return typedBrain;
                }
            }
            return null;
        }

        /// <summary>
        /// 切换到指定类型的大脑
        /// </summary>
        public bool SwitchToBrain<T>() where T : class, IAIBrain
        {
            var brain = GetBrain<T>();
            if (brain != null)
            {
                SetActiveBrain(brain);
                return true;
            }
            return false;
        }

        #endregion

        #region Decision

        /// <summary>
        /// 更新决策
        /// </summary>
        private void UpdateDecision()
        {
            _context.LastDecisionTime = Time.time;

            // 如果没有活跃大脑，尝试激活第一个
            if (_activeBrain == null && _brains.Count > 0)
            {
                SetActiveBrain(_brains[0]);
            }
        }

        /// <summary>
        /// 强制重新评估决策
        /// </summary>
        public void ForceReevaluate()
        {
            _activeBrain?.ForceReevaluate();
            _decisionTimer = 0f;
        }

        #endregion

        #region Activation

        private void OnActivate()
        {
            OnActiveStateChanged?.Invoke(true);

            if (_enableDebug)
            {
                Debug.Log($"[AIAgent] Activated: {gameObject.name}");
            }
        }

        private void OnDeactivate()
        {
            _activeBrain?.StopCurrentAction();
            OnActiveStateChanged?.Invoke(false);

            if (_enableDebug)
            {
                Debug.Log($"[AIAgent] Deactivated: {gameObject.name}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 重置 AI 状态
        /// </summary>
        public void ResetAI()
        {
            _activeBrain?.Reset();
            _context?.Reset();
            _decisionTimer = 0f;

            if (_enableDebug)
            {
                Debug.Log($"[AIAgent] Reset: {gameObject.name}");
            }
        }

        /// <summary>
        /// 暂停 AI
        /// </summary>
        public void Pause()
        {
            IsActive = false;
        }

        /// <summary>
        /// 恢复 AI
        /// </summary>
        public void Resume()
        {
            IsActive = true;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!_enableDebug) return;
            if (_context?.CurrentTarget == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _context.CurrentTarget.position);
            Gizmos.DrawWireSphere(_context.CurrentTarget.position, 0.5f);
        }

        #endregion
    }
}
