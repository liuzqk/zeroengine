// ============================================================================
// RealmDatabaseSO.cs
// 境界数据库 (ScriptableObject)
// 创建于: 2026-01-09
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Character.Realm
{
    /// <summary>
    /// 境界数据库
    /// 存储所有境界配置的集合
    /// </summary>
    [CreateAssetMenu(fileName = "RealmDatabase", menuName = "ZeroEngine/Character/Realm/Realm Database")]
    public class RealmDatabaseSO : ScriptableObject
    {
        [Header("境界列表")]
        [Tooltip("所有境界配置")]
        [SerializeField]
        private List<RealmDataSO> _realms = new List<RealmDataSO>();

        // 运行时缓存
        private Dictionary<RealmType, RealmDataSO> _realmLookup;

        /// <summary>
        /// 境界数量
        /// </summary>
        public int Count => _realms.Count;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _realmLookup = new Dictionary<RealmType, RealmDataSO>();
            foreach (var realm in _realms)
            {
                if (realm != null && !_realmLookup.ContainsKey(realm.realmType))
                {
                    _realmLookup[realm.realmType] = realm;
                }
            }
        }

        /// <summary>
        /// 获取境界数据
        /// </summary>
        public RealmDataSO GetRealmData(RealmType realmType)
        {
            if (_realmLookup == null) BuildLookup();

            _realmLookup.TryGetValue(realmType, out var data);
            return data;
        }

        /// <summary>
        /// 获取所有境界
        /// </summary>
        public IReadOnlyList<RealmDataSO> GetAllRealms()
        {
            return _realms;
        }

        /// <summary>
        /// 按分类获取境界
        /// </summary>
        public List<RealmDataSO> GetRealmsByCategory(RealmCategory category)
        {
            return _realms.FindAll(r => r != null && RealmHelper.GetCategory(r.realmType) == category);
        }

        /// <summary>
        /// 获取凡人境界列表
        /// </summary>
        public List<RealmDataSO> GetMortalRealms()
        {
            return GetRealmsByCategory(RealmCategory.Mortal);
        }

        /// <summary>
        /// 获取仙人境界列表
        /// </summary>
        public List<RealmDataSO> GetImmortalRealms()
        {
            return GetRealmsByCategory(RealmCategory.Immortal);
        }

        /// <summary>
        /// 获取下一个境界数据
        /// </summary>
        public RealmDataSO GetNextRealmData(RealmType currentRealm)
        {
            var nextRealm = RealmHelper.GetNextRealm(currentRealm);
            return GetRealmData(nextRealm);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器: 添加境界
        /// </summary>
        public void AddRealm(RealmDataSO realm)
        {
            if (realm != null && !_realms.Contains(realm))
            {
                _realms.Add(realm);
                BuildLookup();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// 编辑器: 移除境界
        /// </summary>
        public void RemoveRealm(RealmDataSO realm)
        {
            if (_realms.Remove(realm))
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
            var duplicates = new HashSet<RealmType>();
            var seen = new HashSet<RealmType>();

            foreach (var realm in _realms)
            {
                if (realm == null) continue;

                if (seen.Contains(realm.realmType))
                {
                    duplicates.Add(realm.realmType);
                }
                seen.Add(realm.realmType);
            }

            if (duplicates.Count > 0)
            {
                Debug.LogWarning($"[RealmDatabase] 发现重复境界类型: {string.Join(", ", duplicates)}");
            }
            else
            {
                Debug.Log($"[RealmDatabase] 验证通过, 共 {_realms.Count} 个境界");
            }
        }

        /// <summary>
        /// 编辑器: 创建默认境界配置
        /// </summary>
        [ContextMenu("Create Default Realms")]
        private void CreateDefaultRealms()
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            string folder = System.IO.Path.GetDirectoryName(path);

            var defaultRealms = new[]
            {
                (RealmType.Mortal_Beginner, "初入江湖", 0, 1f),
                (RealmType.Mortal_Intermediate, "小有所成", 1000, 1.2f),
                (RealmType.Mortal_Advanced, "一流高手", 5000, 1.5f),
                (RealmType.Mortal_Peak, "绝顶高手", 20000, 2f),
                (RealmType.Immortal_Entry, "踏入仙途", 50000, 2.5f),
                (RealmType.Immortal_Foundation, "筑基", 100000, 3f),
                (RealmType.Immortal_Core, "结丹", 200000, 4f),
                (RealmType.Immortal_Nascent, "元婴", 500000, 5f)
            };

            foreach (var (type, name, cultivation, multiplier) in defaultRealms)
            {
                if (_realmLookup != null && _realmLookup.ContainsKey(type))
                    continue;

                var realmData = ScriptableObject.CreateInstance<RealmDataSO>();
                realmData.realmType = type;
                realmData.realmName = name;
                realmData.description = RealmHelper.GetRealmDescription(type);
                realmData.cultivationRequired = cultivation;
                realmData.statMultiplier = new RealmStatMultiplier
                {
                    healthMultiplier = multiplier,
                    energyMultiplier = multiplier,
                    attackMultiplier = multiplier,
                    defenseMultiplier = multiplier,
                    speedMultiplier = 1f + (multiplier - 1f) * 0.5f
                };

                string assetPath = $"{folder}/Realm_{type}.asset";
                UnityEditor.AssetDatabase.CreateAsset(realmData, assetPath);

                _realms.Add(realmData);
            }

            BuildLookup();
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.EditorUtility.SetDirty(this);

            Debug.Log($"[RealmDatabase] 创建默认境界配置完成");
        }
#endif
    }
}
