using System;
using UnityEngine;

namespace ZeroEngine.AI.GOAP
{
    /// <summary>
    /// GOAP 桥接器 - 连接 crashkonijn/GOAP 与 ZeroEngine
    /// 需要安装 com.crashkonijn.goap 包
    /// </summary>
    /// <remarks>
    /// 安装方式: 在 manifest.json 中添加:
    /// "com.crashkonijn.goap": "https://github.com/crashkonijn/GOAP.git?path=Package#3.0.0"
    /// </remarks>
    public static class GOAPBridge
    {
        /// <summary>GOAP 包是否已安装</summary>
        public static bool IsGOAPInstalled
        {
            get
            {
#if CRASHKONIJN_GOAP
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 检查 GOAP 依赖
        /// </summary>
        public static void ValidateDependency()
        {
            if (!IsGOAPInstalled)
            {
                Debug.LogWarning("[ZeroEngine.AI.GOAP] crashkonijn/GOAP package is not installed. " +
                    "Add to manifest.json: \"com.crashkonijn.goap\": \"https://github.com/crashkonijn/GOAP.git?path=Package#3.0.0\"");
            }
        }
    }

#if CRASHKONIJN_GOAP
    using CrashKonijn.Goap.Behaviours;
    using CrashKonijn.Goap.Interfaces;
    using CrashKonijn.Goap.Classes;

    /// <summary>
    /// ZeroEngine GOAP Agent 包装器
    /// 将 crashkonijn/GOAP 集成到 ZeroEngine AI 系统
    /// </summary>
    public class ZeroGOAPAgent : MonoBehaviour, IAIBrain
    {
        #region Serialized Fields

        [Header("GOAP Settings")]
        [SerializeField] private AgentBehaviour _goapAgent;
        [SerializeField] private bool _autoInitialize = true;

        [Header("ZeroEngine Integration")]
        [SerializeField] private bool _syncBlackboard = true;
        [SerializeField] private float _blackboardSyncInterval = 0.1f;

        #endregion

        #region Runtime State

        private AIContext _context;
        private float _syncTimer;
        private bool _isActive;

        #endregion

        #region Properties

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                if (_goapAgent != null)
                {
                    _goapAgent.enabled = value;
                }
            }
        }

        public string CurrentActionName
        {
            get
            {
                if (_goapAgent?.CurrentAction != null)
                {
                    return _goapAgent.CurrentAction.GetType().Name;
                }
                return "None";
            }
        }

        public AgentBehaviour GOAPAgent => _goapAgent;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_goapAgent == null)
            {
                _goapAgent = GetComponent<AgentBehaviour>();
            }
        }

        #endregion

        #region IAIBrain Implementation

        public void Initialize(AIContext context)
        {
            _context = context;

            if (_autoInitialize && _goapAgent != null)
            {
                // GOAP Agent 初始化由 GoapRunnerBehaviour 处理
            }
        }

        public void Tick(float deltaTime)
        {
            if (!_isActive || _context == null) return;

            // 同步黑板
            if (_syncBlackboard)
            {
                _syncTimer -= deltaTime;
                if (_syncTimer <= 0f)
                {
                    _syncTimer = _blackboardSyncInterval;
                    SyncBlackboard();
                }
            }
        }

        public void ForceReevaluate()
        {
            // crashkonijn/GOAP 会自动重新评估
        }

        public void StopCurrentAction()
        {
            // 由 GOAP 系统管理
        }

        public void Reset()
        {
            _syncTimer = 0f;
        }

        #endregion

        #region Blackboard Sync

        /// <summary>
        /// 同步 ZeroEngine 黑板到 GOAP
        /// </summary>
        private void SyncBlackboard()
        {
            if (_context?.Blackboard == null) return;

            // 同步常用值到 GOAP WorldData
            // 具体实现取决于项目需求
        }

        /// <summary>
        /// 从 ZeroEngine 黑板设置 GOAP 世界状态
        /// </summary>
        public void SetWorldStateFromBlackboard(string blackboardKey, string worldKey)
        {
            if (_context?.Blackboard == null) return;

            // 示例: 将黑板值传递给 GOAP
            // 需要根据实际的 GOAP 配置实现
        }

        #endregion
    }

    /// <summary>
    /// GOAP 目标基类 - 与 ZeroEngine 集成
    /// </summary>
    public abstract class ZeroGOAPGoal : GoalBase
    {
        protected AIContext Context { get; private set; }

        public void SetContext(AIContext context)
        {
            Context = context;
        }
    }

    /// <summary>
    /// GOAP 行动基类 - 与 ZeroEngine 集成
    /// </summary>
    public abstract class ZeroGOAPAction : ActionBase
    {
        protected AIContext Context { get; private set; }

        public void SetContext(AIContext context)
        {
            Context = context;
        }

        /// <summary>
        /// 从黑板获取值
        /// </summary>
        protected T GetBlackboardValue<T>(string key, T defaultValue = default)
        {
            return Context?.Blackboard?.Get(key, defaultValue) ?? defaultValue;
        }

        /// <summary>
        /// 设置黑板值
        /// </summary>
        protected void SetBlackboardValue<T>(string key, T value)
        {
            Context?.Blackboard?.Set(key, value);
        }
    }

#else
    // 当 GOAP 未安装时的占位符

    /// <summary>
    /// GOAP 未安装时的占位 Agent
    /// </summary>
    public class ZeroGOAPAgent : MonoBehaviour, IAIBrain
    {
        public bool IsActive { get; set; }
        public string CurrentActionName => "GOAP Not Installed";

        public void Initialize(AIContext context)
        {
            GOAPBridge.ValidateDependency();
        }

        public void Tick(float deltaTime) { }
        public void ForceReevaluate() { }
        public void StopCurrentAction() { }
        public void Reset() { }
    }
#endif
}
