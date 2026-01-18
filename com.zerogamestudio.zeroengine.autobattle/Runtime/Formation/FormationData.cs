using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.AutoBattle.Grid;

namespace ZeroEngine.AutoBattle.Formation
{
    /// <summary>
    /// 阵型数据
    /// </summary>
    [Serializable]
    public class FormationData
    {
        /// <summary>
        /// 阵型唯一ID
        /// </summary>
        public string FormationId { get; set; }

        /// <summary>
        /// 阵型名称
        /// </summary>
        public string FormationName { get; set; }

        /// <summary>
        /// 阵型槽位列表
        /// </summary>
        public List<FormationSlot> Slots { get; set; } = new();

        /// <summary>
        /// 最大单位数量
        /// </summary>
        public int MaxUnits { get; set; } = 6;

        public FormationData() { }

        public FormationData(string id, string name, int maxUnits = 6)
        {
            FormationId = id;
            FormationName = name;
            MaxUnits = maxUnits;
        }

        /// <summary>
        /// 添加槽位
        /// </summary>
        public FormationSlot AddSlot(Vector2Int position)
        {
            if (Slots.Count >= MaxUnits)
                return null;

            var slot = new FormationSlot
            {
                SlotIndex = Slots.Count,
                Position = position
            };
            Slots.Add(slot);
            return slot;
        }

        /// <summary>
        /// 获取指定索引的槽位
        /// </summary>
        public FormationSlot GetSlot(int index)
        {
            if (index < 0 || index >= Slots.Count)
                return null;
            return Slots[index];
        }

        /// <summary>
        /// 检查位置是否已被占用
        /// </summary>
        public bool IsPositionOccupied(Vector2Int position)
        {
            foreach (var slot in Slots)
            {
                if (slot.Position == position)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 清空所有槽位的单位
        /// </summary>
        public void ClearAllUnits()
        {
            foreach (var slot in Slots)
            {
                slot.UnitId = null;
            }
        }
    }

    /// <summary>
    /// 阵型槽位
    /// </summary>
    [Serializable]
    public class FormationSlot
    {
        /// <summary>
        /// 槽位索引
        /// </summary>
        public int SlotIndex { get; set; }

        /// <summary>
        /// 槽位在棋盘上的位置
        /// </summary>
        public Vector2Int Position { get; set; }

        /// <summary>
        /// 占用此槽位的单位ID
        /// </summary>
        public string UnitId { get; set; }

        /// <summary>
        /// 槽位是否被占用
        /// </summary>
        public bool IsOccupied => !string.IsNullOrEmpty(UnitId);

        /// <summary>
        /// 推荐的职业类型（可选）
        /// </summary>
        public string RecommendedRole { get; set; }
    }
}
