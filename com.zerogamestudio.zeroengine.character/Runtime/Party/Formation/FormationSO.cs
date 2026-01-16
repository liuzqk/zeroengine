using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Party
{
    /// <summary>
    /// 阵型配置 ScriptableObject
    /// 定义一种队伍阵型的所有位置和属性
    /// </summary>
    [CreateAssetMenu(fileName = "Formation", menuName = "ZeroEngine/Party/Formation")]
    public class FormationSO : ScriptableObject
    {
        [Header("基本信息")]
        [Tooltip("阵型名称")]
        public string FormationName;

        [Tooltip("阵型描述")]
        [TextArea(2, 4)]
        public string Description;

        [Tooltip("阵型图标")]
        public Sprite Icon;

        [Tooltip("阵型类型")]
        public FormationType FormationType = FormationType.Standard;

        [Header("槽位配置")]
        [Tooltip("阵型槽位列表")]
        public List<FormationSlot> Slots = new List<FormationSlot>();

        [Header("阵型属性")]
        [Tooltip("整体间距缩放")]
        [Range(0.5f, 2f)]
        public float SpacingScale = 1f;

        [Tooltip("阵型旋转跟随领袖")]
        public bool RotateWithLeader = true;

        [Tooltip("战斗中是否可用")]
        public bool UsableInCombat = true;

        [Tooltip("探索中是否可用")]
        public bool UsableInExploration = true;

        [Header("解锁条件")]
        [Tooltip("解锁所需队伍等级")]
        public int RequiredPartyLevel = 1;

        [Tooltip("是否默认解锁")]
        public bool IsDefaultUnlocked = true;

        /// <summary>
        /// 获取指定槽位的配置
        /// </summary>
        public FormationSlot GetSlot(int slotIndex)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].SlotIndex == slotIndex)
                    return Slots[i];
            }
            return null;
        }

        /// <summary>
        /// 获取指定位置类型的所有槽位
        /// </summary>
        public IEnumerable<FormationSlot> GetSlotsByPosition(FormationPosition position)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].PositionType == position)
                    yield return Slots[i];
            }
        }

        /// <summary>
        /// 槽位数量
        /// </summary>
        public int SlotCount => Slots.Count;

        /// <summary>
        /// 验证配置
        /// </summary>
        private void OnValidate()
        {
            // 确保槽位索引不重复
            var usedIndices = new HashSet<int>();
            foreach (var slot in Slots)
            {
                if (usedIndices.Contains(slot.SlotIndex))
                {
                    Debug.LogWarning($"[FormationSO] {name}: 槽位索引 {slot.SlotIndex} 重复!");
                }
                usedIndices.Add(slot.SlotIndex);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器中可视化绘制
        /// </summary>
        [ContextMenu("创建默认4人阵型")]
        private void CreateDefault4MemberFormation()
        {
            Slots.Clear();

            // 前排 2 人
            Slots.Add(new FormationSlot
            {
                SlotIndex = 0,
                LocalPosition = new Vector3(-1f, 0, 1f),
                PositionType = FormationPosition.Front,
                DefenseModifier = -0.1f,
                AttackModifier = 0.1f,
                ThreatWeight = 1.2f
            });

            Slots.Add(new FormationSlot
            {
                SlotIndex = 1,
                LocalPosition = new Vector3(1f, 0, 1f),
                PositionType = FormationPosition.Front,
                DefenseModifier = -0.1f,
                AttackModifier = 0.1f,
                ThreatWeight = 1.2f
            });

            // 后排 2 人
            Slots.Add(new FormationSlot
            {
                SlotIndex = 2,
                LocalPosition = new Vector3(-1f, 0, -1f),
                PositionType = FormationPosition.Back,
                DefenseModifier = 0.1f,
                AttackModifier = -0.1f,
                ThreatWeight = 0.8f
            });

            Slots.Add(new FormationSlot
            {
                SlotIndex = 3,
                LocalPosition = new Vector3(1f, 0, -1f),
                PositionType = FormationPosition.Back,
                DefenseModifier = 0.1f,
                AttackModifier = -0.1f,
                ThreatWeight = 0.8f
            });

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[FormationSO] 已创建默认4人阵型: {name}");
        }

        [ContextMenu("创建V形阵型")]
        private void CreateVFormation()
        {
            Slots.Clear();

            // 尖端 (领袖)
            Slots.Add(new FormationSlot
            {
                SlotIndex = 0,
                LocalPosition = new Vector3(0, 0, 2f),
                PositionType = FormationPosition.Front,
                DefenseModifier = -0.2f,
                AttackModifier = 0.2f,
                ThreatWeight = 1.5f
            });

            // 两翼
            Slots.Add(new FormationSlot
            {
                SlotIndex = 1,
                LocalPosition = new Vector3(-1.5f, 0, 0.5f),
                PositionType = FormationPosition.Flank,
                DefenseModifier = 0f,
                AttackModifier = 0f,
                ThreatWeight = 1f
            });

            Slots.Add(new FormationSlot
            {
                SlotIndex = 2,
                LocalPosition = new Vector3(1.5f, 0, 0.5f),
                PositionType = FormationPosition.Flank,
                DefenseModifier = 0f,
                AttackModifier = 0f,
                ThreatWeight = 1f
            });

            // 后方
            Slots.Add(new FormationSlot
            {
                SlotIndex = 3,
                LocalPosition = new Vector3(0, 0, -1f),
                PositionType = FormationPosition.Back,
                DefenseModifier = 0.2f,
                AttackModifier = -0.1f,
                ThreatWeight = 0.6f
            });

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[FormationSO] 已创建V形阵型: {name}");
        }
#endif
    }

    /// <summary>
    /// 阵型类型
    /// </summary>
    public enum FormationType
    {
        /// <summary>标准阵型</summary>
        Standard,

        /// <summary>进攻阵型</summary>
        Offensive,

        /// <summary>防御阵型</summary>
        Defensive,

        /// <summary>包围阵型</summary>
        Surround,

        /// <summary>纵列阵型</summary>
        Column,

        /// <summary>横列阵型</summary>
        Line,

        /// <summary>V形阵型</summary>
        Wedge,

        /// <summary>圆形阵型</summary>
        Circle,

        /// <summary>自定义阵型</summary>
        Custom
    }
}
