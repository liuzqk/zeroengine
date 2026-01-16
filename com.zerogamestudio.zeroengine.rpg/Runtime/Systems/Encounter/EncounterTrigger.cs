// ============================================================================
// ZeroEngine v2.6.0 - Encounter System
// 遭遇触发器组件
// ============================================================================

using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ZeroEngine.RPG.Encounter
{
    /// <summary>
    /// 触发器类型
    /// </summary>
    public enum EncounterTriggerType
    {
        /// <summary>按步数触发 (RPG 地图移动)</summary>
        StepBased,
        /// <summary>按时间触发 (实时探索)</summary>
        TimeBased,
        /// <summary>进入区域触发 (Collider)</summary>
        ZoneBased,
        /// <summary>强制触发 (Boss/剧情)</summary>
        Forced
    }

    /// <summary>
    /// 遭遇触发器 - 挂载到区域/地图上
    /// </summary>
    public class EncounterTrigger : MonoBehaviour
    {
        [Header("触发器配置")]
        [Tooltip("触发器类型")]
        [SerializeField] private EncounterTriggerType _triggerType = EncounterTriggerType.StepBased;

        [Tooltip("此区域的遭遇表")]
        [SerializeField] private EncounterTableSO _encounterTable;

        [Tooltip("进入区域时自动设置遭遇表")]
        [SerializeField] private bool _autoSetTableOnEnter = true;

        [Header("步数触发配置")]
        [Tooltip("每移动多少距离算一步")]
        [SerializeField] private float _stepDistance = 1f;

        [Header("时间触发配置")]
        [Tooltip("检测间隔 (秒)")]
        [SerializeField] private float _checkInterval = 2f;

        [Header("区域触发配置")]
        [Tooltip("触发标签 (通常是 Player)")]
        [SerializeField] private string _triggerTag = "Player";

        [Tooltip("进入区域时的触发概率")]
        [Range(0f, 1f)]
        [SerializeField] private float _zoneTriggerChance = 0.5f;

        [Header("强制触发配置")]
        [Tooltip("强制触发的遭遇 ID (Boss)")]
        [SerializeField] private string _forcedEncounterId;

        [Tooltip("只触发一次")]
        [SerializeField] private bool _triggerOnce = true;

        // 运行时状态
        private Vector3 _lastPosition;
        private float _accumulatedDistance;
        private float _timeSinceLastCheck;
        private bool _hasTriggered;
        private Transform _trackedTransform;

        // 事件
        public event Action<EncounterResult> OnEncounterReady;

        // 属性
        public EncounterTriggerType TriggerType => _triggerType;
        public EncounterTableSO EncounterTable => _encounterTable;
        public bool HasTriggered => _hasTriggered;

        // ========================================
        // Unity 生命周期
        // ========================================

        private void OnEnable()
        {
            _hasTriggered = false;
            _accumulatedDistance = 0f;
            _timeSinceLastCheck = 0f;
        }

        private void Update()
        {
            if (_hasTriggered && _triggerOnce) return;

            switch (_triggerType)
            {
                case EncounterTriggerType.StepBased:
                    UpdateStepBased();
                    break;
                case EncounterTriggerType.TimeBased:
                    UpdateTimeBased();
                    break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggerType != EncounterTriggerType.ZoneBased &&
                _triggerType != EncounterTriggerType.Forced)
                return;

            if (!string.IsNullOrEmpty(_triggerTag) && !other.CompareTag(_triggerTag))
                return;

            HandleZoneEnter(other.transform);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggerType != EncounterTriggerType.ZoneBased &&
                _triggerType != EncounterTriggerType.Forced)
                return;

            if (!string.IsNullOrEmpty(_triggerTag) && !other.CompareTag(_triggerTag))
                return;

            HandleZoneEnter(other.transform);
        }

        // ========================================
        // 触发逻辑
        // ========================================

        private void UpdateStepBased()
        {
            if (_trackedTransform == null) return;

            float distance = Vector3.Distance(_trackedTransform.position, _lastPosition);
            _accumulatedDistance += distance;
            _lastPosition = _trackedTransform.position;

            // 每累积一步距离，调用 EncounterManager.ProcessStep()
            while (_accumulatedDistance >= _stepDistance)
            {
                _accumulatedDistance -= _stepDistance;

                if (EncounterManager.Instance != null)
                {
                    var result = EncounterManager.Instance.ProcessStep();
                    if (result.HasValue)
                    {
                        OnEncounterReady?.Invoke(result.Value);
                        if (_triggerOnce) _hasTriggered = true;
                    }
                }
            }
        }

        private void UpdateTimeBased()
        {
            _timeSinceLastCheck += Time.deltaTime;

            if (_timeSinceLastCheck >= _checkInterval)
            {
                _timeSinceLastCheck = 0f;

                if (EncounterManager.Instance != null)
                {
                    var result = EncounterManager.Instance.ProcessStep();
                    if (result.HasValue)
                    {
                        OnEncounterReady?.Invoke(result.Value);
                        if (_triggerOnce) _hasTriggered = true;
                    }
                }
            }
        }

        private void HandleZoneEnter(Transform other)
        {
            // 设置遭遇表
            if (_autoSetTableOnEnter && _encounterTable != null && EncounterManager.Instance != null)
            {
                EncounterManager.Instance.SetEncounterTable(_encounterTable);
            }

            // 强制触发
            if (_triggerType == EncounterTriggerType.Forced)
            {
                if (_hasTriggered && _triggerOnce) return;

                if (EncounterManager.Instance != null && !string.IsNullOrEmpty(_forcedEncounterId))
                {
                    var result = EncounterManager.Instance.TriggerBossEncounter(_forcedEncounterId);
                    if (result.HasValue)
                    {
                        OnEncounterReady?.Invoke(result.Value);
                        _hasTriggered = true;
                    }
                }
                return;
            }

            // 区域概率触发
            if (_triggerType == EncounterTriggerType.ZoneBased)
            {
                if (_hasTriggered && _triggerOnce) return;

                if (UnityEngine.Random.value < _zoneTriggerChance)
                {
                    if (EncounterManager.Instance != null)
                    {
                        var result = EncounterManager.Instance.TriggerRandomEncounter();
                        if (result.HasValue)
                        {
                            OnEncounterReady?.Invoke(result.Value);
                            _hasTriggered = true;
                        }
                    }
                }
            }

            // 步数/时间触发开始追踪
            if (_triggerType == EncounterTriggerType.StepBased ||
                _triggerType == EncounterTriggerType.TimeBased)
            {
                StartTracking(other);
            }
        }

        // ========================================
        // 公共 API
        // ========================================

        /// <summary>
        /// 开始追踪目标 (用于步数/时间触发)
        /// </summary>
        public void StartTracking(Transform target)
        {
            _trackedTransform = target;
            _lastPosition = target.position;
            _accumulatedDistance = 0f;
            LogDebug("开始追踪: ", target.name);
        }

        /// <summary>
        /// 停止追踪
        /// </summary>
        public void StopTracking()
        {
            _trackedTransform = null;
            LogDebug("停止追踪", null);
        }

        /// <summary>
        /// 手动触发遭遇检测
        /// </summary>
        public EncounterResult? ManualTrigger()
        {
            if (_hasTriggered && _triggerOnce) return null;

            if (EncounterManager.Instance == null) return null;

            var result = EncounterManager.Instance.TriggerRandomEncounter();
            if (result.HasValue)
            {
                OnEncounterReady?.Invoke(result.Value);
                if (_triggerOnce) _hasTriggered = true;
            }

            return result;
        }

        /// <summary>
        /// 重置触发状态
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
            _accumulatedDistance = 0f;
            _timeSinceLastCheck = 0f;
        }

        // ========================================
        // Gizmos
        // ========================================

#if UNITY_EDITOR
        // Editor 缓存，避免每帧 GetComponent
        [System.NonSerialized] private Collider _cachedCollider;
        [System.NonSerialized] private Collider2D _cachedCollider2D;
        [System.NonSerialized] private bool _collidersCached;

        private void CacheColliders()
        {
            if (_collidersCached) return;
            _cachedCollider = GetComponent<Collider>();
            _cachedCollider2D = GetComponent<Collider2D>();
            _collidersCached = true;
        }

        private void OnDrawGizmos()
        {
            CacheColliders();

            Gizmos.color = _triggerType switch
            {
                EncounterTriggerType.StepBased => new Color(0f, 1f, 0f, 0.3f),
                EncounterTriggerType.TimeBased => new Color(1f, 1f, 0f, 0.3f),
                EncounterTriggerType.ZoneBased => new Color(1f, 0.5f, 0f, 0.3f),
                EncounterTriggerType.Forced => new Color(1f, 0f, 0f, 0.3f),
                _ => new Color(1f, 1f, 1f, 0.3f)
            };

            if (_cachedCollider is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (_cachedCollider is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
            else if (_cachedCollider2D is BoxCollider2D box2D)
            {
                Vector3 center = transform.position + (Vector3)box2D.offset;
                Vector3 size = new Vector3(box2D.size.x, box2D.size.y, 0.1f);
                Gizmos.DrawCube(center, size);
                Gizmos.DrawWireCube(center, size);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 选中时显示更多信息
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"Encounter: {_triggerType}\n" +
                $"Table: {(_encounterTable != null ? _encounterTable.DisplayName : "None")}"
            );
        }

        private void OnValidate()
        {
            // Inspector 修改时刷新缓存
            _collidersCached = false;
        }
#endif

        // ========================================
        // 条件编译 Debug 日志 (零 GC)
        // ========================================

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void LogDebug(string prefix, string message)
        {
            if (message != null)
                Debug.Log(string.Concat("[EncounterTrigger] ", prefix, message));
            else
                Debug.Log(string.Concat("[EncounterTrigger] ", prefix));
        }
    }
}
