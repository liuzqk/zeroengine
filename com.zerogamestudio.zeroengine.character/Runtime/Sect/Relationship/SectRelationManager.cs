// ============================================================================
// SectRelationManager.cs
// 门派关系管理器
// 创建于: 2026-01-09
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Core;

namespace ZeroEngine.Character.Sect
{
    /// <summary>
    /// 门派关系管理器
    /// 管理门派之间的关系 (友好/敌对/中立)
    /// </summary>
    public class SectRelationManager : MonoSingleton<SectRelationManager>
    {
        [Header("配置")]
        [Tooltip("门派数据库引用")]
        [SerializeField]
        private SectDatabaseSO _database;

        [Tooltip("默认关系")]
        [SerializeField]
        private SectRelationType _defaultRelation = SectRelationType.Neutral;

        // 关系矩阵 (使用字典存储非默认关系)
        private Dictionary<(SectType, SectType), SectRelationType> _relations =
            new Dictionary<(SectType, SectType), SectRelationType>();

        // ===== 事件 =====

        /// <summary>关系变化事件</summary>
        public event Action<SectRelationChangedEventArgs> OnRelationChanged;

        // ===== 初始化 =====

        protected override void Awake()
        {
            base.Awake();
            InitializeRelationsFromDatabase();
        }

        /// <summary>
        /// 从数据库初始化关系
        /// </summary>
        private void InitializeRelationsFromDatabase()
        {
            if (_database == null) return;

            foreach (var sect in _database.GetAllSects())
            {
                if (sect == null) continue;

                // 设置友好关系
                foreach (var friendlySect in sect.friendlySects)
                {
                    SetRelationInternal(sect.sectType, friendlySect, SectRelationType.Friendly, false);
                }

                // 设置敌对关系
                foreach (var hostileSect in sect.hostileSects)
                {
                    SetRelationInternal(sect.sectType, hostileSect, SectRelationType.Hostile, false);
                }
            }
        }

        // ===== 关系查询 =====

        /// <summary>
        /// 获取两个门派之间的关系
        /// </summary>
        public SectRelationType GetRelation(SectType sectA, SectType sectB)
        {
            // 同门派
            if (sectA == sectB) return SectRelationType.Allied;

            // 无门派
            if (sectA == SectType.None || sectB == SectType.None)
                return SectRelationType.Neutral;

            // 查找关系 (双向)
            var key = GetRelationKey(sectA, sectB);
            if (_relations.TryGetValue(key, out var relation))
            {
                return relation;
            }

            return _defaultRelation;
        }

        /// <summary>
        /// 检查两个门派是否敌对
        /// </summary>
        public bool AreHostile(SectType sectA, SectType sectB)
        {
            var relation = GetRelation(sectA, sectB);
            return relation == SectRelationType.Hostile || relation == SectRelationType.Unfriendly;
        }

        /// <summary>
        /// 检查两个门派是否友好
        /// </summary>
        public bool AreFriendly(SectType sectA, SectType sectB)
        {
            var relation = GetRelation(sectA, sectB);
            return relation == SectRelationType.Friendly || relation == SectRelationType.Allied;
        }

        /// <summary>
        /// 检查两个门派是否同盟
        /// </summary>
        public bool AreAllied(SectType sectA, SectType sectB)
        {
            return GetRelation(sectA, sectB) == SectRelationType.Allied;
        }

        // ===== 关系修改 =====

        /// <summary>
        /// 设置两个门派之间的关系
        /// </summary>
        public void SetRelation(SectType sectA, SectType sectB, SectRelationType newRelation)
        {
            SetRelationInternal(sectA, sectB, newRelation, true);
        }

        private void SetRelationInternal(SectType sectA, SectType sectB, SectRelationType newRelation, bool triggerEvent)
        {
            if (sectA == sectB || sectA == SectType.None || sectB == SectType.None)
                return;

            var key = GetRelationKey(sectA, sectB);
            var oldRelation = GetRelation(sectA, sectB);

            if (oldRelation == newRelation) return;

            // 更新关系
            if (newRelation == _defaultRelation)
            {
                _relations.Remove(key);
            }
            else
            {
                _relations[key] = newRelation;
            }

            // 触发事件
            if (triggerEvent)
            {
                var eventArgs = new SectRelationChangedEventArgs
                {
                    SectA = sectA,
                    SectB = sectB,
                    OldRelation = oldRelation,
                    NewRelation = newRelation
                };

                OnRelationChanged?.Invoke(eventArgs);
                EventManager.Trigger(SectEvents.OnSectRelationChanged, eventArgs);

                Debug.Log($"[SectRelation] {sectA} <-> {sectB}: {oldRelation} -> {newRelation}");
            }
        }

        /// <summary>
        /// 改善关系
        /// </summary>
        public void ImproveRelation(SectType sectA, SectType sectB)
        {
            var current = GetRelation(sectA, sectB);
            var improved = ImproveRelationType(current);
            if (improved != current)
            {
                SetRelation(sectA, sectB, improved);
            }
        }

        /// <summary>
        /// 恶化关系
        /// </summary>
        public void WorsenRelation(SectType sectA, SectType sectB)
        {
            var current = GetRelation(sectA, sectB);
            var worsened = WorsenRelationType(current);
            if (worsened != current)
            {
                SetRelation(sectA, sectB, worsened);
            }
        }

        // ===== 批量查询 =====

        /// <summary>
        /// 获取与指定门派敌对的所有门派
        /// </summary>
        public List<SectType> GetHostileSects(SectType sect)
        {
            var result = new List<SectType>();

            if (_database == null) return result;

            foreach (var sectData in _database.GetAllSects())
            {
                if (sectData.sectType != sect && AreHostile(sect, sectData.sectType))
                {
                    result.Add(sectData.sectType);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取与指定门派友好的所有门派
        /// </summary>
        public List<SectType> GetFriendlySects(SectType sect)
        {
            var result = new List<SectType>();

            if (_database == null) return result;

            foreach (var sectData in _database.GetAllSects())
            {
                if (sectData.sectType != sect && AreFriendly(sect, sectData.sectType))
                {
                    result.Add(sectData.sectType);
                }
            }

            return result;
        }

        // ===== 辅助方法 =====

        /// <summary>
        /// 获取关系键 (确保顺序一致)
        /// </summary>
        private (SectType, SectType) GetRelationKey(SectType a, SectType b)
        {
            return (int)a < (int)b ? (a, b) : (b, a);
        }

        private SectRelationType ImproveRelationType(SectRelationType current)
        {
            return current switch
            {
                SectRelationType.Hostile => SectRelationType.Unfriendly,
                SectRelationType.Unfriendly => SectRelationType.Neutral,
                SectRelationType.Neutral => SectRelationType.Friendly,
                SectRelationType.Friendly => SectRelationType.Allied,
                _ => current
            };
        }

        private SectRelationType WorsenRelationType(SectRelationType current)
        {
            return current switch
            {
                SectRelationType.Allied => SectRelationType.Friendly,
                SectRelationType.Friendly => SectRelationType.Neutral,
                SectRelationType.Neutral => SectRelationType.Unfriendly,
                SectRelationType.Unfriendly => SectRelationType.Hostile,
                _ => current
            };
        }

        // ===== 存档 =====

        /// <summary>
        /// 获取存档数据
        /// </summary>
        public SectRelationSaveData GetSaveData()
        {
            var entries = new List<SectRelationEntry>();

            foreach (var kvp in _relations)
            {
                entries.Add(new SectRelationEntry
                {
                    sectA = kvp.Key.Item1,
                    sectB = kvp.Key.Item2,
                    relation = kvp.Value
                });
            }

            return new SectRelationSaveData
            {
                relations = entries.ToArray()
            };
        }

        /// <summary>
        /// 加载存档数据
        /// </summary>
        public void LoadSaveData(SectRelationSaveData saveData)
        {
            if (saveData == null) return;

            _relations.Clear();

            foreach (var entry in saveData.relations)
            {
                var key = GetRelationKey(entry.sectA, entry.sectB);
                _relations[key] = entry.relation;
            }
        }
    }

    /// <summary>
    /// 门派关系存档数据
    /// </summary>
    [Serializable]
    public class SectRelationSaveData
    {
        public SectRelationEntry[] relations;
    }

    /// <summary>
    /// 门派关系条目
    /// </summary>
    [Serializable]
    public class SectRelationEntry
    {
        public SectType sectA;
        public SectType sectB;
        public SectRelationType relation;
    }
}
