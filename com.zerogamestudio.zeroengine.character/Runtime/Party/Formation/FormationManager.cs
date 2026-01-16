using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Core;
using ZeroEngine.Save;

namespace ZeroEngine.Party
{
    /// <summary>
    /// 阵型管理器
    /// 管理队伍阵型、成员位置、阵型切换
    /// </summary>
    public class FormationManager : MonoSingleton<FormationManager>, ISaveable
    {
        [Header("配置")]
        [Tooltip("可用阵型列表")]
        [SerializeField] private List<FormationSO> _availableFormations = new List<FormationSO>();

        [Tooltip("默认阵型")]
        [SerializeField] private FormationSO _defaultFormation;

        [Tooltip("阵型锚点 (队伍位置参考点)")]
        [SerializeField] private Transform _formationAnchor;

        [Header("位置更新")]
        [Tooltip("位置更新模式")]
        [SerializeField] private PositionUpdateMode _updateMode = PositionUpdateMode.Instant;

        [Tooltip("移动速度 (Smooth 模式)")]
        [SerializeField] private float _moveSpeed = 5f;

        [Tooltip("旋转速度 (Smooth 模式)")]
        [SerializeField] private float _rotateSpeed = 360f;

        [Header("调试")]
        [SerializeField] private bool _debugMode;
        [SerializeField] private bool _drawGizmos = true;

        // 当前阵型
        private FormationSO _currentFormation;

        // 已解锁阵型
        private readonly HashSet<string> _unlockedFormations = new HashSet<string>();

        // 成员目标位置缓存
        private readonly Dictionary<IPartyMember, Vector3> _targetPositions = new Dictionary<IPartyMember, Vector3>();
        private readonly Dictionary<IPartyMember, Quaternion> _targetRotations = new Dictionary<IPartyMember, Quaternion>();

        // 性能优化
        private readonly List<IPartyMember> _tempMemberList = new List<IPartyMember>(16);

        #region Events

        /// <summary>阵型变更事件</summary>
        public event Action<FormationChangedEventArgs> OnFormationChanged;

        /// <summary>成员位置更新事件</summary>
        public event Action<MemberPositionUpdatedEventArgs> OnMemberPositionUpdated;

        /// <summary>阵型解锁事件</summary>
        public event Action<FormationSO> OnFormationUnlocked;

        #endregion

        #region Properties

        /// <summary>当前阵型</summary>
        public FormationSO CurrentFormation => _currentFormation;

        /// <summary>阵型锚点</summary>
        public Transform FormationAnchor
        {
            get => _formationAnchor;
            set => _formationAnchor = value;
        }

        /// <summary>可用阵型列表</summary>
        public IReadOnlyList<FormationSO> AvailableFormations => _availableFormations;

        /// <summary>位置更新模式</summary>
        public PositionUpdateMode UpdateMode
        {
            get => _updateMode;
            set => _updateMode = value;
        }

        #endregion

        #region ISaveable

        public string SaveKey => "Formation";

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
            return new FormationManagerSaveData
            {
                CurrentFormationName = _currentFormation?.name,
                UnlockedFormations = new List<string>(_unlockedFormations)
            };
        }

        public void ImportSaveData(object data)
        {
            if (data is not FormationManagerSaveData saveData) return;

            // 恢复解锁状态
            _unlockedFormations.Clear();
            foreach (var name in saveData.UnlockedFormations)
            {
                _unlockedFormations.Add(name);
            }

            // 恢复当前阵型
            if (!string.IsNullOrEmpty(saveData.CurrentFormationName))
            {
                var formation = FindFormationByName(saveData.CurrentFormationName);
                if (formation != null)
                {
                    SetFormation(formation, false);
                }
            }
        }

        public void ResetToDefault()
        {
            _unlockedFormations.Clear();
            InitializeUnlockedFormations();
            SetFormation(_defaultFormation, false);
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            InitializeUnlockedFormations();
        }

        private void Start()
        {
            Register();

            // 设置默认阵型
            if (_currentFormation == null && _defaultFormation != null)
            {
                SetFormation(_defaultFormation, false);
            }

            // 监听队伍变化
            if (PartyManager.Instance != null)
            {
                PartyManager.Instance.OnMemberJoined += OnMemberJoined;
                PartyManager.Instance.OnMemberLeft += OnMemberLeft;
                PartyManager.Instance.OnSlotChanged += OnSlotChanged;
            }
        }

        protected override void OnDestroy()
        {
            Unregister();

            if (PartyManager.Instance != null)
            {
                PartyManager.Instance.OnMemberJoined -= OnMemberJoined;
                PartyManager.Instance.OnMemberLeft -= OnMemberLeft;
                PartyManager.Instance.OnSlotChanged -= OnSlotChanged;
            }

            base.OnDestroy();
        }

        private void Update()
        {
            if (_updateMode == PositionUpdateMode.Smooth)
            {
                UpdateMemberPositionsSmooth();
            }
        }

        #endregion

        #region Initialization

        private void InitializeUnlockedFormations()
        {
            foreach (var formation in _availableFormations)
            {
                if (formation != null && formation.IsDefaultUnlocked)
                {
                    _unlockedFormations.Add(formation.name);
                }
            }
        }

        #endregion

        #region Public API - Formation

        /// <summary>
        /// 设置当前阵型
        /// </summary>
        public bool SetFormation(FormationSO formation, bool animate = true)
        {
            if (formation == null)
            {
                LogWarning("尝试设置空阵型");
                return false;
            }

            if (!IsFormationUnlocked(formation))
            {
                LogWarning($"阵型 {formation.FormationName} 未解锁");
                return false;
            }

            // 检查场景限制
            var partyState = PartyManager.Instance?.CurrentState ?? PartyState.Idle;
            if (partyState == PartyState.InCombat && !formation.UsableInCombat)
            {
                LogWarning($"阵型 {formation.FormationName} 不能在战斗中使用");
                return false;
            }

            if (partyState == PartyState.Exploring && !formation.UsableInExploration)
            {
                LogWarning($"阵型 {formation.FormationName} 不能在探索中使用");
                return false;
            }

            var oldFormation = _currentFormation;
            _currentFormation = formation;

            // 更新所有成员位置
            RefreshAllPositions(animate);

            OnFormationChanged?.Invoke(new FormationChangedEventArgs(oldFormation, formation));
            Log($"切换阵型: {oldFormation?.FormationName ?? "无"} -> {formation.FormationName}");

            return true;
        }

        /// <summary>
        /// 切换到下一个阵型
        /// </summary>
        public bool CycleFormation(bool forward = true)
        {
            if (_availableFormations.Count == 0) return false;

            int currentIndex = _availableFormations.IndexOf(_currentFormation);
            int nextIndex;

            if (forward)
            {
                nextIndex = (currentIndex + 1) % _availableFormations.Count;
            }
            else
            {
                nextIndex = (currentIndex - 1 + _availableFormations.Count) % _availableFormations.Count;
            }

            // 跳过未解锁的阵型
            int attempts = 0;
            while (!IsFormationUnlocked(_availableFormations[nextIndex]) && attempts < _availableFormations.Count)
            {
                nextIndex = forward
                    ? (nextIndex + 1) % _availableFormations.Count
                    : (nextIndex - 1 + _availableFormations.Count) % _availableFormations.Count;
                attempts++;
            }

            return SetFormation(_availableFormations[nextIndex]);
        }

        /// <summary>
        /// 解锁阵型
        /// </summary>
        public bool UnlockFormation(FormationSO formation)
        {
            if (formation == null) return false;

            if (_unlockedFormations.Contains(formation.name))
            {
                return false; // 已解锁
            }

            _unlockedFormations.Add(formation.name);
            OnFormationUnlocked?.Invoke(formation);
            Log($"解锁阵型: {formation.FormationName}");

            return true;
        }

        /// <summary>
        /// 检查阵型是否已解锁
        /// </summary>
        public bool IsFormationUnlocked(FormationSO formation)
        {
            if (formation == null) return false;
            return _unlockedFormations.Contains(formation.name);
        }

        /// <summary>
        /// 获取已解锁的阵型
        /// </summary>
        public IEnumerable<FormationSO> GetUnlockedFormations()
        {
            foreach (var formation in _availableFormations)
            {
                if (IsFormationUnlocked(formation))
                {
                    yield return formation;
                }
            }
        }

        #endregion

        #region Public API - Positions

        /// <summary>
        /// 刷新所有成员位置
        /// </summary>
        public void RefreshAllPositions(bool animate = true)
        {
            if (_currentFormation == null || PartyManager.Instance == null) return;

            _tempMemberList.Clear();
            _tempMemberList.AddRange(PartyManager.Instance.GetActiveMembers());

            foreach (var member in _tempMemberList)
            {
                UpdateMemberPosition(member, animate);
            }

            _tempMemberList.Clear();
        }

        /// <summary>
        /// 更新单个成员位置
        /// </summary>
        public void UpdateMemberPosition(IPartyMember member, bool animate = true)
        {
            if (member == null || _currentFormation == null) return;

            var slot = _currentFormation.GetSlot(member.PartySlotIndex);
            if (slot == null)
            {
                Log($"成员 {member.DisplayName} 没有对应的阵型槽位");
                return;
            }

            var targetPos = slot.GetWorldPosition(_formationAnchor) * _currentFormation.SpacingScale;
            var targetRot = slot.GetWorldRotation(_formationAnchor);

            // 处理领袖跟随
            if (_currentFormation.RotateWithLeader && PartyManager.Instance.Leader != null)
            {
                var leaderForward = PartyManager.Instance.Leader.Transform.forward;
                var leaderRot = Quaternion.LookRotation(leaderForward);
                targetRot = leaderRot * Quaternion.Euler(slot.LocalRotation);
            }

            var oldPos = member.Transform.position;

            if (animate && _updateMode == PositionUpdateMode.Smooth)
            {
                // 平滑模式：记录目标位置
                _targetPositions[member] = targetPos;
                _targetRotations[member] = targetRot;
            }
            else
            {
                // 即时模式：直接设置
                member.Transform.position = targetPos;
                member.Transform.rotation = targetRot;

                OnMemberPositionUpdated?.Invoke(new MemberPositionUpdatedEventArgs(
                    member, oldPos, targetPos, slot));
            }
        }

        /// <summary>
        /// 获取成员在当前阵型中的槽位
        /// </summary>
        public FormationSlot GetMemberSlot(IPartyMember member)
        {
            if (member == null || _currentFormation == null) return null;
            return _currentFormation.GetSlot(member.PartySlotIndex);
        }

        /// <summary>
        /// 获取成员的目标位置
        /// </summary>
        public Vector3 GetMemberTargetPosition(IPartyMember member)
        {
            if (_targetPositions.TryGetValue(member, out var pos))
            {
                return pos;
            }

            var slot = GetMemberSlot(member);
            if (slot != null)
            {
                return slot.GetWorldPosition(_formationAnchor) * _currentFormation.SpacingScale;
            }

            return member?.Transform.position ?? Vector3.zero;
        }

        #endregion

        #region Public API - Modifiers

        /// <summary>
        /// 获取成员的防御修正
        /// </summary>
        public float GetDefenseModifier(IPartyMember member)
        {
            var slot = GetMemberSlot(member);
            return slot?.DefenseModifier ?? 0f;
        }

        /// <summary>
        /// 获取成员的攻击修正
        /// </summary>
        public float GetAttackModifier(IPartyMember member)
        {
            var slot = GetMemberSlot(member);
            return slot?.AttackModifier ?? 0f;
        }

        /// <summary>
        /// 获取成员的仇恨权重
        /// </summary>
        public float GetThreatWeight(IPartyMember member)
        {
            var slot = GetMemberSlot(member);
            return slot?.ThreatWeight ?? 1f;
        }

        #endregion

        #region Event Handlers

        private void OnMemberJoined(MemberJoinedEventArgs args)
        {
            if (args.SlotType == PartySlotType.Active)
            {
                UpdateMemberPosition(args.Member, true);
            }
        }

        private void OnMemberLeft(MemberLeftEventArgs args)
        {
            _targetPositions.Remove(args.Member);
            _targetRotations.Remove(args.Member);
        }

        private void OnSlotChanged(SlotChangedEventArgs args)
        {
            // 成员切换到出战队伍时更新位置
            if (args.NewSlotType == PartySlotType.Active)
            {
                UpdateMemberPosition(args.Member, true);
            }
        }

        #endregion

        #region Position Update

        private void UpdateMemberPositionsSmooth()
        {
            if (_targetPositions.Count == 0) return;

            float deltaTime = Time.deltaTime;
            float moveStep = _moveSpeed * deltaTime;
            float rotateStep = _rotateSpeed * deltaTime;

            _tempMemberList.Clear();

            foreach (var kvp in _targetPositions)
            {
                var member = kvp.Key;
                var targetPos = kvp.Value;

                if (member?.Transform == null)
                {
                    _tempMemberList.Add(member);
                    continue;
                }

                var currentPos = member.Transform.position;
                var newPos = Vector3.MoveTowards(currentPos, targetPos, moveStep);
                member.Transform.position = newPos;

                // 旋转
                if (_targetRotations.TryGetValue(member, out var targetRot))
                {
                    member.Transform.rotation = Quaternion.RotateTowards(
                        member.Transform.rotation, targetRot, rotateStep);
                }

                // 到达目标后移除
                if (Vector3.Distance(newPos, targetPos) < 0.01f)
                {
                    _tempMemberList.Add(member);

                    var slot = GetMemberSlot(member);
                    OnMemberPositionUpdated?.Invoke(new MemberPositionUpdatedEventArgs(
                        member, currentPos, newPos, slot));
                }
            }

            // 清理已到达的成员
            foreach (var member in _tempMemberList)
            {
                _targetPositions.Remove(member);
                _targetRotations.Remove(member);
            }

            _tempMemberList.Clear();
        }

        #endregion

        #region Helper Methods

        private FormationSO FindFormationByName(string formationName)
        {
            foreach (var formation in _availableFormations)
            {
                if (formation != null && formation.name == formationName)
                {
                    return formation;
                }
            }
            return null;
        }

        #endregion

        #region Logging

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("ZEROENGINE_DEBUG")]
        private void Log(string message)
        {
            if (_debugMode)
            {
                Debug.Log($"[FormationManager] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[FormationManager] {message}");
        }

        #endregion

        #region Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_drawGizmos || _currentFormation == null) return;

            var anchor = _formationAnchor != null ? _formationAnchor : transform;

            foreach (var slot in _currentFormation.Slots)
            {
                var worldPos = slot.GetWorldPosition(anchor) * _currentFormation.SpacingScale;

                // 根据位置类型选择颜色
                Gizmos.color = slot.PositionType switch
                {
                    FormationPosition.Front => Color.red,
                    FormationPosition.Back => Color.blue,
                    FormationPosition.Flank => Color.yellow,
                    FormationPosition.Center => Color.green,
                    _ => Color.white
                };

                Gizmos.DrawWireSphere(worldPos, 0.3f);

                // 绘制朝向
                var worldRot = slot.GetWorldRotation(anchor);
                Gizmos.DrawRay(worldPos, worldRot * Vector3.forward * 0.5f);

                // 绘制槽位索引
                UnityEditor.Handles.Label(worldPos + Vector3.up * 0.5f, $"#{slot.SlotIndex}");
            }
        }
#endif

        #endregion
    }

    /// <summary>
    /// 位置更新模式
    /// </summary>
    public enum PositionUpdateMode
    {
        /// <summary>即时更新</summary>
        Instant,

        /// <summary>平滑移动</summary>
        Smooth,

        /// <summary>手动控制 (由外部系统处理)</summary>
        Manual
    }

    #region Save Data

    [Serializable]
    public class FormationManagerSaveData
    {
        public string CurrentFormationName;
        public List<string> UnlockedFormations = new List<string>();
    }

    #endregion
}
