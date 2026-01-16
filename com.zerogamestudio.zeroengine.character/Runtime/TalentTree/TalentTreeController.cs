using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Save;

namespace ZeroEngine.TalentTree
{
    /// <summary>
    /// 天赋树运行时控制器
    /// 管理天赋的解锁、重置和效果应用
    /// </summary>
    public class TalentTreeController : MonoBehaviour, ISaveable
    {
        [Header("配置")]
        [Tooltip("天赋树定义")]
        [SerializeField] private TalentTreeSO _talentTree;

        [Tooltip("初始可用点数")]
        [SerializeField] private int _initialPoints = 0;

        [Tooltip("效果应用目标（默认为自身）")]
        [SerializeField] private GameObject _effectTarget;

        [Header("调试")]
        [SerializeField] private bool _debugMode;

        // 已解锁节点及其等级
        private readonly Dictionary<string, int> _unlockedNodes = new Dictionary<string, int>();

        // 可用点数
        private int _availablePoints;

        // 已花费点数
        private int _spentPoints;

        // === 性能优化：缓存列表和常量字符串 ===
        private readonly List<(TalentNodeSO, int)> _tempUnlockedList = new List<(TalentNodeSO, int)>(16);

        private static class StatusStrings
        {
            public const string Invalid = "无效";
            public const string MaxLevel = "已满级";
            public const string CanUnlock = "可解锁";
            public const string Locked = "已锁定";
            public const string PrereqNotMet = "前置未满足";
        }

        #region Events

        /// <summary>天赋事件</summary>
        public event Action<TalentEventArgs> OnTalentEvent;

        #endregion

        #region Properties

        /// <summary>天赋树定义</summary>
        public TalentTreeSO TalentTree => _talentTree;

        /// <summary>可用点数</summary>
        public int AvailablePoints => _availablePoints;

        /// <summary>已花费点数</summary>
        public int SpentPoints => _spentPoints;

        /// <summary>已解锁节点数量</summary>
        public int UnlockedNodeCount => _unlockedNodes.Count;

        /// <summary>效果应用目标</summary>
        public GameObject EffectTarget => _effectTarget != null ? _effectTarget : gameObject;

        #endregion

        #region ISaveable

        public string SaveKey => $"TalentTree_{(_talentTree != null ? _talentTree.TreeId : "default")}";

        public void Register()
        {
            SaveSlotManager.Instance?.Register(this);
        }

        public void Unregister()
        {
            SaveSlotManager.Instance?.Unregister(this);
        }

        public object ExportSaveData()
        {
            return new TalentTreeSaveData
            {
                TreeId = _talentTree?.TreeId,
                AvailablePoints = _availablePoints,
                SpentPoints = _spentPoints,
                UnlockedNodes = new Dictionary<string, int>(_unlockedNodes)
            };
        }

        public void ImportSaveData(object data)
        {
            if (data is not TalentTreeSaveData saveData) return;

            // 先重置
            ResetTree(refundPoints: false);

            _availablePoints = saveData.AvailablePoints;
            _spentPoints = saveData.SpentPoints;

            // 恢复已解锁节点
            foreach (var kvp in saveData.UnlockedNodes)
            {
                var node = _talentTree?.GetNode(kvp.Key);
                if (node != null)
                {
                    _unlockedNodes[kvp.Key] = kvp.Value;
                    node.ApplyEffects(EffectTarget, kvp.Value);
                }
            }
        }

        public void ResetToDefault()
        {
            ResetTree(refundPoints: true);
            _availablePoints = _initialPoints;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_effectTarget == null)
            {
                _effectTarget = gameObject;
            }
            _availablePoints = _initialPoints;
        }

        private void Start()
        {
            Register();
        }

        private void OnDestroy()
        {
            Unregister();

            // 清理效果
            foreach (var kvp in _unlockedNodes)
            {
                var node = _talentTree?.GetNode(kvp.Key);
                node?.RemoveEffects(EffectTarget);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 设置天赋树
        /// </summary>
        public void SetTalentTree(TalentTreeSO tree)
        {
            if (_talentTree != null)
            {
                ResetTree(refundPoints: true);
            }
            _talentTree = tree;
        }

        /// <summary>
        /// 添加可用点数
        /// </summary>
        public void AddPoints(int points)
        {
            if (points <= 0) return;
            _availablePoints += points;
            OnTalentEvent?.Invoke(TalentEventArgs.PointsChanged(_availablePoints));
            Log($"添加 {points} 点，当前可用: {_availablePoints}");
        }

        /// <summary>
        /// 获取节点当前等级
        /// </summary>
        public int GetNodeLevel(TalentNodeSO node)
        {
            if (node == null) return 0;
            return _unlockedNodes.TryGetValue(node.NodeId, out var level) ? level : 0;
        }

        /// <summary>
        /// 检查是否可以解锁/升级节点
        /// </summary>
        public bool CanUnlock(TalentNodeSO node, int characterLevel = 0)
        {
            if (node == null || _talentTree == null) return false;
            if (!_talentTree.ContainsNode(node)) return false;

            int currentLevel = GetNodeLevel(node);

            // 已达最大等级
            if (currentLevel >= node.MaxLevel) return false;

            // 检查点数
            int cost = node.GetUpgradeCost(currentLevel);
            if (_availablePoints < cost) return false;

            // 检查前置条件
            if (!node.CheckPrerequisites(GetNodeLevel, characterLevel))
                return false;

            return true;
        }

        /// <summary>
        /// 尝试解锁/升级节点
        /// </summary>
        public bool TryUnlock(TalentNodeSO node, int characterLevel = 0)
        {
            if (!CanUnlock(node, characterLevel))
            {
                Log($"无法解锁 {node?.DisplayName}");
                return false;
            }

            int oldLevel = GetNodeLevel(node);
            int newLevel = oldLevel + 1;
            int cost = node.GetUpgradeCost(oldLevel);

            // 扣除点数
            _availablePoints -= cost;
            _spentPoints += cost;

            // 更新等级
            _unlockedNodes[node.NodeId] = newLevel;

            // 应用效果
            if (oldLevel > 0)
            {
                // 升级：先移除旧效果
                node.RemoveEffects(EffectTarget);
            }
            node.ApplyEffects(EffectTarget, newLevel);

            // 触发事件
            if (oldLevel == 0)
            {
                OnTalentEvent?.Invoke(TalentEventArgs.Unlocked(node, newLevel, cost));
                Log($"解锁 {node.DisplayName} (Lv.{newLevel})，花费 {cost} 点");
            }
            else
            {
                OnTalentEvent?.Invoke(TalentEventArgs.LevelUp(node, oldLevel, newLevel, cost));
                Log($"升级 {node.DisplayName} (Lv.{oldLevel} -> Lv.{newLevel})，花费 {cost} 点");
            }

            return true;
        }

        /// <summary>
        /// 重置单个节点
        /// </summary>
        public bool TryResetNode(TalentNodeSO node)
        {
            if (node == null) return false;
            if (!_unlockedNodes.TryGetValue(node.NodeId, out var level))
                return false;

            // 检查是否有后继节点依赖此节点
            foreach (var successor in _talentTree.GetSuccessors(node))
            {
                if (GetNodeLevel(successor) > 0)
                {
                    Log($"无法重置 {node.DisplayName}，有依赖节点");
                    return false;
                }
            }

            // 移除效果
            node.RemoveEffects(EffectTarget);

            // 退还点数
            int refund = node.GetTotalCost(level);
            _availablePoints += refund;
            _spentPoints -= refund;

            // 移除记录
            _unlockedNodes.Remove(node.NodeId);

            // 触发事件
            OnTalentEvent?.Invoke(TalentEventArgs.Reset(node, level, refund));
            Log($"重置 {node.DisplayName}，退还 {refund} 点");

            return true;
        }

        /// <summary>
        /// 重置整棵天赋树
        /// </summary>
        public void ResetTree(bool refundPoints = true)
        {
            int totalRefund = 0;

            // 移除所有效果
            foreach (var kvp in _unlockedNodes)
            {
                var node = _talentTree?.GetNode(kvp.Key);
                node?.RemoveEffects(EffectTarget);

                if (refundPoints)
                {
                    totalRefund += node?.GetTotalCost(kvp.Value) ?? 0;
                }
            }

            // 清空记录
            _unlockedNodes.Clear();

            // 退还点数
            if (refundPoints)
            {
                _availablePoints += totalRefund;
            }
            _spentPoints = 0;

            // 触发事件
            OnTalentEvent?.Invoke(TalentEventArgs.TreeReset(totalRefund));
            Log($"重置天赋树，退还 {totalRefund} 点");
        }

        /// <summary>
        /// 获取所有已解锁节点（零分配版本，使用内部缓存）
        /// </summary>
        public IReadOnlyList<(TalentNodeSO Node, int Level)> GetUnlockedNodes()
        {
            _tempUnlockedList.Clear();
            foreach (var kvp in _unlockedNodes)
            {
                var node = _talentTree?.GetNode(kvp.Key);
                if (node != null)
                {
                    _tempUnlockedList.Add((node, kvp.Value));
                }
            }
            return _tempUnlockedList;
        }

        /// <summary>
        /// 将已解锁节点填充到外部列表（零分配版本）
        /// </summary>
        public void GetUnlockedNodes(List<(TalentNodeSO Node, int Level)> results)
        {
            results.Clear();
            foreach (var kvp in _unlockedNodes)
            {
                var node = _talentTree?.GetNode(kvp.Key);
                if (node != null)
                {
                    results.Add((node, kvp.Value));
                }
            }
        }

        /// <summary>
        /// 检查节点是否已解锁
        /// </summary>
        public bool IsUnlocked(TalentNodeSO node)
        {
            return node != null && _unlockedNodes.ContainsKey(node.NodeId);
        }

        /// <summary>
        /// 获取节点状态描述
        /// </summary>
        public string GetNodeStatus(TalentNodeSO node, int characterLevel = 0)
        {
            if (node == null) return StatusStrings.Invalid;

            int level = GetNodeLevel(node);
            if (level >= node.MaxLevel) return StatusStrings.MaxLevel;

            // 有进度的节点需要动态字符串（非热路径，可接受分配）
            if (level > 0) return string.Concat("Lv.", level.ToString(), "/", node.MaxLevel.ToString());

            if (CanUnlock(node, characterLevel)) return StatusStrings.CanUnlock;

            // 检查具体原因
            int cost = node.GetUpgradeCost(level);
            if (_availablePoints < cost) return string.Concat("点数不足 (", cost.ToString(), ")");
            if (!node.CheckPrerequisites(GetNodeLevel, characterLevel)) return StatusStrings.PrereqNotMet;

            return StatusStrings.Locked;
        }

        #endregion

        #region Logging

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("ZEROENGINE_DEBUG")]
        private void Log(string message)
        {
            if (_debugMode)
            {
                Debug.Log(string.Concat("[TalentTree] ", message));
            }
        }

        #endregion
    }

    #region Save Data

    [Serializable]
    public class TalentTreeSaveData
    {
        public string TreeId;
        public int AvailablePoints;
        public int SpentPoints;
        public Dictionary<string, int> UnlockedNodes = new Dictionary<string, int>();
    }

    #endregion
}
