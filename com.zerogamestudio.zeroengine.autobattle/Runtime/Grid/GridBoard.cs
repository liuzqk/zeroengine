using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.AutoBattle.Grid
{
    /// <summary>
    /// 战斗棋盘，管理格子布局和单位站位
    /// </summary>
    public class GridBoard
    {
        /// <summary>
        /// 棋盘宽度（列数）
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// 棋盘高度（行数）
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// 所有格子
        /// </summary>
        private readonly GridCell[,] _cells;

        /// <summary>
        /// 格子变化事件
        /// </summary>
        public event Action<GridCell, IBattleUnit> OnUnitPlaced;
        public event Action<GridCell, IBattleUnit> OnUnitRemoved;

        public GridBoard(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new GridCell[width, height];

            // 初始化所有格子
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _cells[x, y] = new GridCell(x, y);
                }
            }
        }

        /// <summary>
        /// 获取指定位置的格子
        /// </summary>
        public GridCell GetCell(int x, int y)
        {
            if (!IsValidPosition(x, y))
                return null;
            return _cells[x, y];
        }

        /// <summary>
        /// 获取指定位置的格子
        /// </summary>
        public GridCell GetCell(Vector2Int position)
        {
            return GetCell(position.x, position.y);
        }

        /// <summary>
        /// 检查位置是否有效
        /// </summary>
        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>
        /// 检查位置是否有效
        /// </summary>
        public bool IsValidPosition(Vector2Int position)
        {
            return IsValidPosition(position.x, position.y);
        }

        /// <summary>
        /// 放置单位到指定格子
        /// </summary>
        public bool PlaceUnit(IBattleUnit unit, int x, int y)
        {
            var cell = GetCell(x, y);
            if (cell == null || cell.IsOccupied)
                return false;

            // 如果单位已经在其他格子上，先移除
            if (unit.CurrentCell != null)
            {
                RemoveUnit(unit);
            }

            cell.SetUnit(unit);
            unit.SetCell(cell);
            OnUnitPlaced?.Invoke(cell, unit);
            return true;
        }

        /// <summary>
        /// 放置单位到指定格子
        /// </summary>
        public bool PlaceUnit(IBattleUnit unit, Vector2Int position)
        {
            return PlaceUnit(unit, position.x, position.y);
        }

        /// <summary>
        /// 从棋盘移除单位
        /// </summary>
        public bool RemoveUnit(IBattleUnit unit)
        {
            if (unit.CurrentCell == null)
                return false;

            var cell = unit.CurrentCell;
            cell.ClearUnit();
            unit.SetCell(null);
            OnUnitRemoved?.Invoke(cell, unit);
            return true;
        }

        /// <summary>
        /// 移动单位到新位置
        /// </summary>
        public bool MoveUnit(IBattleUnit unit, int newX, int newY)
        {
            var targetCell = GetCell(newX, newY);
            if (targetCell == null || targetCell.IsOccupied)
                return false;

            RemoveUnit(unit);
            return PlaceUnit(unit, newX, newY);
        }

        /// <summary>
        /// 获取所有被占用的格子
        /// </summary>
        public IEnumerable<GridCell> GetOccupiedCells()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (_cells[x, y].IsOccupied)
                        yield return _cells[x, y];
                }
            }
        }

        /// <summary>
        /// 获取所有空闲格子
        /// </summary>
        public IEnumerable<GridCell> GetEmptyCells()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (!_cells[x, y].IsOccupied)
                        yield return _cells[x, y];
                }
            }
        }

        /// <summary>
        /// 获取指定列的所有格子
        /// </summary>
        public IEnumerable<GridCell> GetColumn(int column)
        {
            if (column < 0 || column >= Width)
                yield break;

            for (int y = 0; y < Height; y++)
            {
                yield return _cells[column, y];
            }
        }

        /// <summary>
        /// 获取指定行的所有格子
        /// </summary>
        public IEnumerable<GridCell> GetRow(int row)
        {
            if (row < 0 || row >= Height)
                yield break;

            for (int x = 0; x < Width; x++)
            {
                yield return _cells[x, row];
            }
        }

        /// <summary>
        /// 获取两个格子之间的曼哈顿距离
        /// </summary>
        public static int GetManhattanDistance(GridCell a, GridCell b)
        {
            return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
        }

        /// <summary>
        /// 清空棋盘上的所有单位
        /// </summary>
        public void Clear()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = _cells[x, y];
                    if (cell.IsOccupied)
                    {
                        var unit = cell.OccupyingUnit;
                        cell.ClearUnit();
                        unit.SetCell(null);
                        OnUnitRemoved?.Invoke(cell, unit);
                    }
                }
            }
        }
    }
}
