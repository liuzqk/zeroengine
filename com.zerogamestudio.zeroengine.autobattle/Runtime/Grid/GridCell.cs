using UnityEngine;

namespace ZeroEngine.AutoBattle.Grid
{
    /// <summary>
    /// 棋盘格子，可容纳一个战斗单位
    /// </summary>
    public class GridCell
    {
        /// <summary>
        /// 格子X坐标（列）
        /// </summary>
        public int X { get; }

        /// <summary>
        /// 格子Y坐标（行）
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// 格子位置
        /// </summary>
        public Vector2Int Position => new Vector2Int(X, Y);

        /// <summary>
        /// 当前占用此格子的单位
        /// </summary>
        public IBattleUnit OccupyingUnit { get; private set; }

        /// <summary>
        /// 格子是否被占用
        /// </summary>
        public bool IsOccupied => OccupyingUnit != null;

        /// <summary>
        /// 格子地形类型
        /// </summary>
        public TerrainType Terrain { get; set; } = TerrainType.Normal;

        /// <summary>
        /// 是否可通行
        /// </summary>
        public bool IsWalkable { get; set; } = true;

        public GridCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// 设置占用单位
        /// </summary>
        internal void SetUnit(IBattleUnit unit)
        {
            OccupyingUnit = unit;
        }

        /// <summary>
        /// 清除占用单位
        /// </summary>
        internal void ClearUnit()
        {
            OccupyingUnit = null;
        }

        public override string ToString()
        {
            return $"Cell({X}, {Y}) {(IsOccupied ? "[Occupied]" : "[Empty]")}";
        }
    }

    /// <summary>
    /// 地形类型
    /// </summary>
    public enum TerrainType
    {
        /// <summary>
        /// 普通地形
        /// </summary>
        Normal,

        /// <summary>
        /// 火焰地形 - 造成持续伤害
        /// </summary>
        Fire,

        /// <summary>
        /// 冰冻地形 - 降低移动速度
        /// </summary>
        Ice,

        /// <summary>
        /// 毒沼地形 - 造成中毒效果
        /// </summary>
        Poison,

        /// <summary>
        /// 治疗地形 - 持续恢复生命
        /// </summary>
        Healing,

        /// <summary>
        /// 障碍物 - 不可通行
        /// </summary>
        Obstacle
    }
}
