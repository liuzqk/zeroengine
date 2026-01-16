// ============================================================================
// SectDatabaseSO.cs
// 门派数据库 (ScriptableObject)
// 创建于: 2026-01-09
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Character.Sect
{
    /// <summary>
    /// 门派数据库
    /// 存储所有门派配置的集合
    /// </summary>
    [CreateAssetMenu(fileName = "SectDatabase", menuName = "ZeroEngine/Character/Sect/Sect Database")]
    public class SectDatabaseSO : ScriptableObject
    {
        [Header("门派列表")]
        [Tooltip("所有门派配置")]
        [SerializeField]
        private List<SectDataSO> _sects = new List<SectDataSO>();

        // 运行时缓存
        private Dictionary<SectType, SectDataSO> _sectLookup;

        /// <summary>
        /// 所有门派数量
        /// </summary>
        public int Count => _sects.Count;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _sectLookup = new Dictionary<SectType, SectDataSO>();
            foreach (var sect in _sects)
            {
                if (sect != null && !_sectLookup.ContainsKey(sect.sectType))
                {
                    _sectLookup[sect.sectType] = sect;
                }
            }
        }

        /// <summary>
        /// 获取门派数据
        /// </summary>
        public SectDataSO GetSectData(SectType sectType)
        {
            if (_sectLookup == null) BuildLookup();

            _sectLookup.TryGetValue(sectType, out var data);
            return data;
        }

        /// <summary>
        /// 获取所有门派
        /// </summary>
        public IReadOnlyList<SectDataSO> GetAllSects()
        {
            return _sects;
        }

        /// <summary>
        /// 按分类获取门派
        /// </summary>
        public List<SectDataSO> GetSectsByCategory(SectCategory category)
        {
            return _sects.FindAll(s => s != null && s.category == category);
        }

        /// <summary>
        /// 获取已解锁的门派 (不需要特殊条件)
        /// </summary>
        public List<SectDataSO> GetUnlockedSects()
        {
            return _sects.FindAll(s => s != null && !s.requiresUnlock);
        }

        /// <summary>
        /// 获取隐藏门派
        /// </summary>
        public List<SectDataSO> GetHiddenSects()
        {
            return _sects.FindAll(s => s != null && s.category == SectCategory.Hidden);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器: 添加门派
        /// </summary>
        public void AddSect(SectDataSO sect)
        {
            if (sect != null && !_sects.Contains(sect))
            {
                _sects.Add(sect);
                BuildLookup();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// 编辑器: 移除门派
        /// </summary>
        public void RemoveSect(SectDataSO sect)
        {
            if (_sects.Remove(sect))
            {
                BuildLookup();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// 编辑器: 验证数据
        /// </summary>
        [ContextMenu("Validate Database")]
        private void ValidateDatabase()
        {
            var duplicates = new HashSet<SectType>();
            var seen = new HashSet<SectType>();

            foreach (var sect in _sects)
            {
                if (sect == null) continue;

                if (seen.Contains(sect.sectType))
                {
                    duplicates.Add(sect.sectType);
                }
                seen.Add(sect.sectType);
            }

            if (duplicates.Count > 0)
            {
                Debug.LogWarning($"[SectDatabase] 发现重复门派类型: {string.Join(", ", duplicates)}");
            }
            else
            {
                Debug.Log($"[SectDatabase] 验证通过, 共 {_sects.Count} 个门派");
            }
        }
#endif
    }
}
