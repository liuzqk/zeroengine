// ============================================================================
// MartialArtDatabaseSO.cs
// 武学数据库 (ScriptableObject)
// 创建于: 2026-01-09
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Character.MartialArts
{
    /// <summary>
    /// 武学数据库
    /// 存储所有武学配置的集合
    /// </summary>
    [CreateAssetMenu(fileName = "MartialArtDatabase", menuName = "ZeroEngine/Character/MartialArts/Martial Art Database")]
    public class MartialArtDatabaseSO : ScriptableObject
    {
        [Header("武学列表")]
        [Tooltip("所有武学配置")]
        [SerializeField]
        private List<MartialArtDataSO> _martialArts = new List<MartialArtDataSO>();

        // 运行时缓存
        private Dictionary<string, MartialArtDataSO> _artLookup;

        /// <summary>
        /// 武学数量
        /// </summary>
        public int Count => _martialArts.Count;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _artLookup = new Dictionary<string, MartialArtDataSO>();
            foreach (var art in _martialArts)
            {
                if (art != null && !string.IsNullOrEmpty(art.artId) && !_artLookup.ContainsKey(art.artId))
                {
                    _artLookup[art.artId] = art;
                }
            }
        }

        /// <summary>
        /// 获取武学数据
        /// </summary>
        public MartialArtDataSO GetMartialArt(string artId)
        {
            if (_artLookup == null) BuildLookup();

            _artLookup.TryGetValue(artId, out var data);
            return data;
        }

        /// <summary>
        /// 获取所有武学
        /// </summary>
        public IReadOnlyList<MartialArtDataSO> GetAllMartialArts()
        {
            return _martialArts;
        }

        /// <summary>
        /// 按类型获取武学
        /// </summary>
        public List<MartialArtDataSO> GetMartialArtsByType(MartialArtType type)
        {
            return _martialArts.FindAll(a => a != null && a.artType == type);
        }

        /// <summary>
        /// 按品级获取武学
        /// </summary>
        public List<MartialArtDataSO> GetMartialArtsByGrade(MartialArtGrade grade)
        {
            return _martialArts.FindAll(a => a != null && a.grade == grade);
        }

        /// <summary>
        /// 按属性获取武学
        /// </summary>
        public List<MartialArtDataSO> GetMartialArtsByElement(ElementType element)
        {
            return _martialArts.FindAll(a => a != null && (a.element & element) != 0);
        }

        /// <summary>
        /// 获取内功列表
        /// </summary>
        public List<MartialArtDataSO> GetInnerArts()
        {
            return GetMartialArtsByType(MartialArtType.InnerArt);
        }

        /// <summary>
        /// 获取轻功列表
        /// </summary>
        public List<MartialArtDataSO> GetLightnessArts()
        {
            return GetMartialArtsByType(MartialArtType.Lightness);
        }

        /// <summary>
        /// 获取绝学列表
        /// </summary>
        public List<MartialArtDataSO> GetUltimateArts()
        {
            return GetMartialArtsByType(MartialArtType.Ultimate);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器: 添加武学
        /// </summary>
        public void AddMartialArt(MartialArtDataSO art)
        {
            if (art != null && !_martialArts.Contains(art))
            {
                _martialArts.Add(art);
                BuildLookup();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// 编辑器: 移除武学
        /// </summary>
        public void RemoveMartialArt(MartialArtDataSO art)
        {
            if (_martialArts.Remove(art))
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
            var duplicates = new HashSet<string>();
            var seen = new HashSet<string>();
            int nullCount = 0;
            int emptyIdCount = 0;

            foreach (var art in _martialArts)
            {
                if (art == null)
                {
                    nullCount++;
                    continue;
                }

                if (string.IsNullOrEmpty(art.artId))
                {
                    emptyIdCount++;
                    continue;
                }

                if (seen.Contains(art.artId))
                {
                    duplicates.Add(art.artId);
                }
                seen.Add(art.artId);
            }

            if (nullCount > 0)
                Debug.LogWarning($"[MartialArtDatabase] 发现 {nullCount} 个空引用");

            if (emptyIdCount > 0)
                Debug.LogWarning($"[MartialArtDatabase] 发现 {emptyIdCount} 个空 ID");

            if (duplicates.Count > 0)
                Debug.LogWarning($"[MartialArtDatabase] 发现重复 ID: {string.Join(", ", duplicates)}");

            if (nullCount == 0 && emptyIdCount == 0 && duplicates.Count == 0)
                Debug.Log($"[MartialArtDatabase] 验证通过, 共 {_martialArts.Count} 个武学");
        }
#endif
    }
}
