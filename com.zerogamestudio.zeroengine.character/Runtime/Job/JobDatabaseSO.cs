// ============================================================================
// JobDatabaseSO.cs
// 职业数据库 (ScriptableObject)
// 创建于: 2026-01-07
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Character.Job
{
    /// <summary>
    /// 职业数据库
    /// 集中管理所有职业配置
    /// </summary>
    [CreateAssetMenu(fileName = "JobDatabase", menuName = "ZeroEngine/Job/Job Database")]
    public class JobDatabaseSO : ScriptableObject
    {
        [Header("基础职业")]
        [Tooltip("基础职业列表 (初始可选)")]
        public List<JobDataSO> BasicJobs = new List<JobDataSO>();

        [Header("高级职业")]
        [Tooltip("高级职业列表 (需解锁)")]
        public List<JobDataSO> AdvancedJobs = new List<JobDataSO>();

        [Header("隐藏职业")]
        [Tooltip("隐藏职业列表 (特殊条件)")]
        public List<JobDataSO> SecretJobs = new List<JobDataSO>();

        [Header("自定义职业")]
        [Tooltip("自定义/Mod 职业")]
        public List<JobDataSO> CustomJobs = new List<JobDataSO>();

        // 运行时缓存
        private Dictionary<JobType, JobDataSO> _jobLookup;
        private Dictionary<string, JobDataSO> _jobNameLookup;

        /// <summary>
        /// 所有职业
        /// </summary>
        public IEnumerable<JobDataSO> AllJobs
        {
            get
            {
                foreach (var job in BasicJobs)
                    if (job != null) yield return job;
                foreach (var job in AdvancedJobs)
                    if (job != null) yield return job;
                foreach (var job in SecretJobs)
                    if (job != null) yield return job;
                foreach (var job in CustomJobs)
                    if (job != null) yield return job;
            }
        }

        /// <summary>
        /// 职业总数
        /// </summary>
        public int TotalJobCount => BasicJobs.Count + AdvancedJobs.Count + SecretJobs.Count + CustomJobs.Count;

        /// <summary>
        /// 根据职业类型获取数据
        /// </summary>
        public JobDataSO GetJobData(JobType jobType)
        {
            EnsureLookupBuilt();
            _jobLookup.TryGetValue(jobType, out var jobData);
            return jobData;
        }

        /// <summary>
        /// 根据名称获取职业数据
        /// </summary>
        public JobDataSO GetJobDataByName(string jobName)
        {
            EnsureLookupBuilt();
            _jobNameLookup.TryGetValue(jobName, out var jobData);
            return jobData;
        }

        /// <summary>
        /// 获取指定分类的职业
        /// </summary>
        public List<JobDataSO> GetJobsByCategory(JobCategory category)
        {
            switch (category)
            {
                case JobCategory.Basic:
                    return BasicJobs;
                case JobCategory.Advanced:
                    return AdvancedJobs;
                case JobCategory.Secret:
                    return SecretJobs;
                case JobCategory.Custom:
                    return CustomJobs;
                default:
                    return new List<JobDataSO>();
            }
        }

        /// <summary>
        /// 获取所有初始可用职业
        /// </summary>
        public List<JobDataSO> GetDefaultUnlockedJobs()
        {
            var result = new List<JobDataSO>();
            foreach (var job in AllJobs)
            {
                if (job != null && job.IsUnlockedByDefault)
                {
                    result.Add(job);
                }
            }
            return result;
        }

        /// <summary>
        /// 检查职业是否存在
        /// </summary>
        public bool HasJob(JobType jobType)
        {
            EnsureLookupBuilt();
            return _jobLookup.ContainsKey(jobType);
        }

        /// <summary>
        /// 构建查找表
        /// </summary>
        private void EnsureLookupBuilt()
        {
            if (_jobLookup != null && _jobNameLookup != null)
                return;

            _jobLookup = new Dictionary<JobType, JobDataSO>();
            _jobNameLookup = new Dictionary<string, JobDataSO>();

            foreach (var job in AllJobs)
            {
                if (job == null) continue;

                if (!_jobLookup.ContainsKey(job.JobType))
                {
                    _jobLookup[job.JobType] = job;
                }
                else
                {
                    Debug.LogWarning($"[JobDatabase] 重复的职业类型: {job.JobType}");
                }

                if (!_jobNameLookup.ContainsKey(job.name))
                {
                    _jobNameLookup[job.name] = job;
                }
            }
        }

        /// <summary>
        /// 清除缓存 (编辑器用)
        /// </summary>
        public void ClearCache()
        {
            _jobLookup = null;
            _jobNameLookup = null;
        }

        private void OnValidate()
        {
            ClearCache();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器: 添加职业到对应分类
        /// </summary>
        public void AddJob(JobDataSO jobData)
        {
            if (jobData == null) return;

            switch (jobData.Category)
            {
                case JobCategory.Basic:
                    if (!BasicJobs.Contains(jobData))
                        BasicJobs.Add(jobData);
                    break;
                case JobCategory.Advanced:
                    if (!AdvancedJobs.Contains(jobData))
                        AdvancedJobs.Add(jobData);
                    break;
                case JobCategory.Secret:
                    if (!SecretJobs.Contains(jobData))
                        SecretJobs.Add(jobData);
                    break;
                case JobCategory.Custom:
                    if (!CustomJobs.Contains(jobData))
                        CustomJobs.Add(jobData);
                    break;
            }

            ClearCache();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 编辑器: 创建默认职业模板
        /// </summary>
        [ContextMenu("Create Default Jobs")]
        public void CreateDefaultJobs()
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            string folder = System.IO.Path.GetDirectoryName(path);

            // 创建 8 个基础职业
            var defaultJobs = new[]
            {
                (JobType.Warrior, "Warrior", "剑士", WeaponCategory.Sword | WeaponCategory.Axe | WeaponCategory.Shield),
                (JobType.Thief, "Thief", "盗贼", WeaponCategory.Dagger | WeaponCategory.Thrown),
                (JobType.Hunter, "Hunter", "猎人", WeaponCategory.Bow | WeaponCategory.Axe),
                (JobType.Monk, "Monk", "武僧", WeaponCategory.Fist | WeaponCategory.Staff),
                (JobType.Scholar, "Scholar", "学者", WeaponCategory.Staff | WeaponCategory.Book),
                (JobType.Cleric, "Cleric", "神官", WeaponCategory.Staff | WeaponCategory.Shield),
                (JobType.Dancer, "Dancer", "舞者", WeaponCategory.Dagger | WeaponCategory.Instrument),
                (JobType.Merchant, "Merchant", "商人", WeaponCategory.Spear | WeaponCategory.Bow),
            };

            foreach (var (jobType, name, displayName, weapons) in defaultJobs)
            {
                string assetPath = $"{folder}/Jobs/{name}.asset";
                System.IO.Directory.CreateDirectory($"{folder}/Jobs");

                var jobData = ScriptableObject.CreateInstance<JobDataSO>();
                jobData.JobType = jobType;
                jobData.Category = JobCategory.Basic;
                jobData.DisplayName = displayName;
                jobData.Description = $"{displayName}职业";
                jobData.AllowedWeapons = weapons;
                jobData.IsUnlockedByDefault = true;

                UnityEditor.AssetDatabase.CreateAsset(jobData, assetPath);
                BasicJobs.Add(jobData);
            }

            ClearCache();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("[JobDatabase] 创建了 8 个默认职业");
        }
#endif
    }
}
