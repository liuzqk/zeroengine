using UnityEngine;

namespace ZeroEngine.Equipment
{
    /// <summary>
    /// 装备槽位类型定义
    /// 通过 ScriptableObject 实现完全可配置的槽位系统
    /// </summary>
    [CreateAssetMenu(fileName = "NewSlotType", menuName = "ZeroEngine/Equipment/Slot Type")]
    public class EquipmentSlotType : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("槽位唯一标识")]
        public string SlotId;

        [Tooltip("显示名称")]
        public string DisplayName;

        [Tooltip("槽位图标")]
        public Sprite Icon;

        [Tooltip("槽位描述")]
        [TextArea]
        public string Description;

        [Header("配置")]
        [Tooltip("是否允许为空")]
        public bool AllowEmpty = true;

        [Tooltip("排序优先级（用于 UI 显示顺序）")]
        public int SortOrder;

        [Header("限制")]
        [Tooltip("允许的装备类型标签（空表示不限制）")]
        public string[] AllowedTags;

        /// <summary>
        /// 检查装备是否可以装备到此槽位
        /// </summary>
        public bool CanEquip(EquipmentDataSO equipment)
        {
            if (equipment == null) return false;
            if (equipment.SlotType != this) return false;
            return true;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(SlotId))
            {
                SlotId = name.ToLowerInvariant().Replace(" ", "_");
            }
            if (string.IsNullOrEmpty(DisplayName))
            {
                DisplayName = name;
            }
        }
    }
}
