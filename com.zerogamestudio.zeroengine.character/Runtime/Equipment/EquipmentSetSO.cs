using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.StatSystem;

namespace ZeroEngine.Equipment
{
    /// <summary>
    /// 套装效果
    /// </summary>
    [Serializable]
    public class SetEffect
    {
        [Tooltip("需要的套装件数")]
        public int RequiredPieces = 2;

        [Tooltip("效果描述")]
        [TextArea]
        public string Description;

        [Tooltip("属性加成")]
        public List<StatModifierData> StatBonuses = new List<StatModifierData>();

        [Tooltip("触发的 Buff ID（可选）")]
        public string BuffId;

        /// <summary>
        /// 创建属性修饰器
        /// </summary>
        public IEnumerable<StatModifier> CreateModifiers(object source)
        {
            foreach (var stat in StatBonuses)
            {
                yield return stat.CreateModifier(0, source);
            }
        }
    }

    /// <summary>
    /// 套装定义
    /// </summary>
    [CreateAssetMenu(fileName = "NewEquipmentSet", menuName = "ZeroEngine/Equipment/Equipment Set")]
    public class EquipmentSetSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("套装唯一 ID")]
        public string SetId;

        [Tooltip("套装名称")]
        public string SetName;

        [Tooltip("套装描述")]
        [TextArea]
        public string Description;

        [Tooltip("套装图标")]
        public Sprite Icon;

        [Header("套装件")]
        [Tooltip("套装包含的装备列表")]
        public List<EquipmentDataSO> Pieces = new List<EquipmentDataSO>();

        [Header("套装效果")]
        [Tooltip("套装效果列表（2/4/6 件套等）")]
        public List<SetEffect> Effects = new List<SetEffect>();

        /// <summary>
        /// 获取套装总件数
        /// </summary>
        public int TotalPieces => Pieces.Count;

        /// <summary>
        /// 获取指定件数激活的效果
        /// </summary>
        public IEnumerable<SetEffect> GetActiveEffects(int equippedCount)
        {
            foreach (var effect in Effects)
            {
                if (equippedCount >= effect.RequiredPieces)
                {
                    yield return effect;
                }
            }
        }

        /// <summary>
        /// 获取下一个效果阈值
        /// </summary>
        public int GetNextThreshold(int currentCount)
        {
            foreach (var effect in Effects)
            {
                if (effect.RequiredPieces > currentCount)
                {
                    return effect.RequiredPieces;
                }
            }
            return -1; // 已激活所有效果
        }

        /// <summary>
        /// 检查装备是否属于此套装
        /// </summary>
        public bool ContainsEquipment(EquipmentDataSO equipment)
        {
            if (equipment == null) return false;
            return Pieces.Contains(equipment);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(SetId))
            {
                SetId = name.ToLowerInvariant().Replace(" ", "_");
            }
            if (string.IsNullOrEmpty(SetName))
            {
                SetName = name;
            }

            // 按件数排序效果
            Effects.Sort((a, b) => a.RequiredPieces.CompareTo(b.RequiredPieces));
        }
    }
}
