// ============================================================================
// JobManager.cs
// 职业管理器 (MonoSingleton + ISaveable)
// 创建于: 2026-01-07
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Core;

namespace ZeroEngine.Character.Job
{
    /// <summary>
    /// 职业管理器
    /// 管理角色职业、职业切换、技能学习
    /// </summary>
    public class JobManager : MonoSingleton<JobManager>
    {
        [Header("配置")]
        [Tooltip("职业配置库")]
        [SerializeField]
        private JobDatabaseSO _database;

        [Tooltip("是否允许换职 (某些系统可能锁定主职业)")]
        [SerializeField]
        private bool _allowJobChange = true;

        [Tooltip("换职是否需要消耗")]
        [SerializeField]
        private bool _jobChangeCostEnabled = false;

        [Tooltip("换职消耗金币")]
        [SerializeField]
        private int _jobChangeGoldCost = 1000;

        [Header("运行时数据")]
        [SerializeField]
        private string _characterId = "player";

        [SerializeField]
        private JobInstance _primaryJob;

        [SerializeField]
        private JobInstance _secondaryJob;

        [SerializeField]
        private List<JobType> _unlockedJobs = new List<JobType>();

        [SerializeField]
        private Dictionary<JobType, JobInstance> _allJobInstances = new Dictionary<JobType, JobInstance>();

        [SerializeField]
        private int _supportSkillSlots = 4;

        [SerializeField]
        private List<string> _equippedSupportSkillIds = new List<string>();

        // ===== 事件 =====

        /// <summary>主职业变更事件</summary>
        public event Action<JobChangedEventArgs> OnPrimaryJobChanged;

        /// <summary>副职业变更事件</summary>
        public event Action<JobChangedEventArgs> OnSecondaryJobChanged;

        /// <summary>职业升级事件</summary>
        public event Action<JobLevelUpEventArgs> OnJobLevelUp;

        /// <summary>技能学习事件</summary>
        public event Action<SkillLearnedEventArgs> OnSkillLearned;

        /// <summary>技能精通事件</summary>
        public event Action<SkillMasteredEventArgs> OnSkillMastered;

        /// <summary>职业解锁事件</summary>
        public event Action<JobUnlockedEventArgs> OnJobUnlocked;

        /// <summary>支援技能变更事件</summary>
        public event Action<SupportSkillChangedEventArgs> OnSupportSkillChanged;

        // ===== 属性 =====

        /// <summary>职业数据库</summary>
        public JobDatabaseSO Database => _database;

        /// <summary>当前主职业</summary>
        public JobInstance PrimaryJob => _primaryJob;

        /// <summary>当前副职业</summary>
        public JobInstance SecondaryJob => _secondaryJob;

        /// <summary>是否有副职业</summary>
        public bool HasSecondaryJob => _secondaryJob != null && _secondaryJob.JobData != null;

        /// <summary>已解锁职业列表</summary>
        public IReadOnlyList<JobType> UnlockedJobs => _unlockedJobs;

        /// <summary>支援技能槽位数</summary>
        public int SupportSkillSlots => _supportSkillSlots;

        /// <summary>已装备的支援技能</summary>
        public IReadOnlyList<string> EquippedSupportSkills => _equippedSupportSkillIds;

        // ===== 初始化 =====

        protected override void Awake()
        {
            base.Awake();

            // 初始化字典
            if (_allJobInstances == null)
                _allJobInstances = new Dictionary<JobType, JobInstance>();
        }

        /// <summary>
        /// 初始化职业系统
        /// </summary>
        /// <param name="characterId">角色 ID</param>
        /// <param name="primaryJobType">初始主职业</param>
        public void Initialize(string characterId, JobType primaryJobType)
        {
            _characterId = characterId;

            // 解锁初始职业
            if (_database != null)
            {
                foreach (var jobData in _database.AllJobs)
                {
                    if (jobData.IsUnlockedByDefault)
                    {
                        UnlockJobInternal(jobData.JobType, silent: true);
                    }
                }
            }

            // 设置主职业
            SetPrimaryJob(primaryJobType);
        }

        // ===== 职业管理 =====

        /// <summary>
        /// 设置主职业
        /// </summary>
        public bool SetPrimaryJob(JobType jobType)
        {
            if (!_allowJobChange && _primaryJob != null)
            {
                Debug.LogWarning("[JobManager] 主职业已锁定，不可更换");
                return false;
            }

            var jobData = GetJobData(jobType);
            if (jobData == null)
            {
                Debug.LogError($"[JobManager] 职业数据未找到: {jobType}");
                return false;
            }

            // 检查是否解锁
            if (!IsJobUnlocked(jobType))
            {
                Debug.LogWarning($"[JobManager] 职业未解锁: {jobType}");
                return false;
            }

            var oldJob = _primaryJob;

            // 获取或创建职业实例
            _primaryJob = GetOrCreateJobInstance(jobType);

            // 绑定事件
            BindJobEvents(_primaryJob);

            OnPrimaryJobChanged?.Invoke(new JobChangedEventArgs
            {
                CharacterId = _characterId,
                SlotType = JobSlotType.Primary,
                OldJob = oldJob,
                NewJob = _primaryJob
            });

            return true;
        }

        /// <summary>
        /// 设置副职业
        /// </summary>
        public bool SetSecondaryJob(JobType jobType)
        {
            // 允许设置为 None (清除副职业)
            if (jobType == JobType.None)
            {
                var previousJob = _secondaryJob;
                _secondaryJob = null;

                OnSecondaryJobChanged?.Invoke(new JobChangedEventArgs
                {
                    CharacterId = _characterId,
                    SlotType = JobSlotType.Secondary,
                    OldJob = previousJob,
                    NewJob = null
                });
                return true;
            }

            var jobData = GetJobData(jobType);
            if (jobData == null)
            {
                Debug.LogError($"[JobManager] 职业数据未找到: {jobType}");
                return false;
            }

            // 检查是否解锁
            if (!IsJobUnlocked(jobType))
            {
                Debug.LogWarning($"[JobManager] 职业未解锁: {jobType}");
                return false;
            }

            // 不能与主职业相同
            if (_primaryJob != null && _primaryJob.JobType == jobType)
            {
                Debug.LogWarning($"[JobManager] 副职业不能与主职业相同: {jobType}");
                return false;
            }

            // 检查换职消耗
            if (_jobChangeCostEnabled && _jobChangeGoldCost > 0)
            {
                // TODO: 对接金币系统
                // if (!CurrencyManager.Instance?.TrySpend("gold", _jobChangeGoldCost) ?? false)
                // {
                //     Debug.LogWarning("[JobManager] 金币不足，无法换职");
                //     return false;
                // }
            }

            var oldJob = _secondaryJob;
            _secondaryJob = GetOrCreateJobInstance(jobType);
            BindJobEvents(_secondaryJob);

            OnSecondaryJobChanged?.Invoke(new JobChangedEventArgs
            {
                CharacterId = _characterId,
                SlotType = JobSlotType.Secondary,
                OldJob = oldJob,
                NewJob = _secondaryJob
            });

            return true;
        }

        /// <summary>
        /// 获取或创建职业实例
        /// </summary>
        private JobInstance GetOrCreateJobInstance(JobType jobType)
        {
            if (_allJobInstances.TryGetValue(jobType, out var existing))
                return existing;

            var jobData = GetJobData(jobType);
            if (jobData == null) return null;

            var instance = new JobInstance(jobData);
            _allJobInstances[jobType] = instance;
            return instance;
        }

        /// <summary>
        /// 绑定职业事件
        /// </summary>
        private void BindJobEvents(JobInstance job)
        {
            if (job == null) return;

            job.OnLevelUp += (oldLevel, newLevel) =>
            {
                OnJobLevelUp?.Invoke(new JobLevelUpEventArgs
                {
                    CharacterId = _characterId,
                    JobInstance = job,
                    OldLevel = oldLevel,
                    NewLevel = newLevel
                });
            };

            job.OnSkillLearned += skill =>
            {
                OnSkillLearned?.Invoke(new SkillLearnedEventArgs
                {
                    CharacterId = _characterId,
                    JobInstance = job,
                    Skill = skill
                });
            };

            job.OnSkillMastered += skill =>
            {
                OnSkillMastered?.Invoke(new SkillMasteredEventArgs
                {
                    CharacterId = _characterId,
                    JobInstance = job,
                    Skill = skill
                });
            };
        }

        // ===== 职业解锁 =====

        /// <summary>
        /// 检查职业是否已解锁
        /// </summary>
        public bool IsJobUnlocked(JobType jobType)
        {
            return _unlockedJobs.Contains(jobType);
        }

        /// <summary>
        /// 解锁职业
        /// </summary>
        public bool UnlockJob(JobType jobType)
        {
            return UnlockJobInternal(jobType, silent: false);
        }

        private bool UnlockJobInternal(JobType jobType, bool silent)
        {
            if (_unlockedJobs.Contains(jobType))
                return false;

            var jobData = GetJobData(jobType);
            if (jobData == null)
                return false;

            // 检查解锁条件
            if (!silent && !jobData.CheckUnlockConditions())
            {
                Debug.LogWarning($"[JobManager] 职业解锁条件不满足: {jobType}");
                return false;
            }

            _unlockedJobs.Add(jobType);

            if (!silent)
            {
                OnJobUnlocked?.Invoke(new JobUnlockedEventArgs
                {
                    CharacterId = _characterId,
                    JobType = jobType,
                    JobData = jobData
                });
            }

            return true;
        }

        // ===== JP 系统 =====

        /// <summary>
        /// 为当前职业添加 JP
        /// </summary>
        /// <param name="amount">JP 数量</param>
        /// <param name="applyToBoth">是否同时给主副职业</param>
        public void AddJP(int amount, bool applyToBoth = false)
        {
            _primaryJob?.AddJP(amount);

            if (applyToBoth && _secondaryJob != null)
            {
                _secondaryJob.AddJP(amount);
            }
        }

        /// <summary>
        /// 为指定职业添加 JP
        /// </summary>
        public void AddJPToJob(JobType jobType, int amount)
        {
            if (_allJobInstances.TryGetValue(jobType, out var job))
            {
                job.AddJP(amount);
            }
        }

        // ===== 技能系统 =====

        /// <summary>
        /// 学习技能
        /// </summary>
        public bool LearnSkill(JobType jobType, string skillId)
        {
            if (!_allJobInstances.TryGetValue(jobType, out var job))
                return false;

            var skill = GetSkillById(jobType, skillId);
            if (skill == null)
                return false;

            return job.LearnSkill(skill);
        }

        /// <summary>
        /// 获取可用技能列表 (主职业 + 副职业 + 精通技能)
        /// </summary>
        public List<JobSkillSO> GetAvailableSkills()
        {
            var result = new List<JobSkillSO>();

            // 主职业已学技能
            if (_primaryJob != null)
            {
                result.AddRange(_primaryJob.GetLearnedSkills());
            }

            // 副职业已学技能
            if (_secondaryJob != null)
            {
                foreach (var skill in _secondaryJob.GetLearnedSkills())
                {
                    if (!skill.IsJobExclusive)
                    {
                        result.Add(skill);
                    }
                }
            }

            // 所有精通技能 (可跨职业使用)
            foreach (var kvp in _allJobInstances)
            {
                foreach (var skillId in kvp.Value.MasteredSkillIds)
                {
                    var skill = GetSkillById(kvp.Key, skillId);
                    if (skill != null && !result.Contains(skill))
                    {
                        result.Add(skill);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取所有精通技能
        /// </summary>
        public List<JobSkillSO> GetAllMasteredSkills()
        {
            var result = new List<JobSkillSO>();

            foreach (var kvp in _allJobInstances)
            {
                foreach (var skillId in kvp.Value.MasteredSkillIds)
                {
                    var skill = GetSkillById(kvp.Key, skillId);
                    if (skill != null)
                    {
                        result.Add(skill);
                    }
                }
            }

            return result;
        }

        // ===== 支援技能 =====

        /// <summary>
        /// 装备支援技能
        /// </summary>
        public bool EquipSupportSkill(string skillId, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _supportSkillSlots)
                return false;

            // 检查是否已精通
            bool isMastered = false;
            foreach (var kvp in _allJobInstances)
            {
                if (kvp.Value.HasMasteredSkill(skillId))
                {
                    isMastered = true;
                    break;
                }
            }

            if (!isMastered)
            {
                Debug.LogWarning($"[JobManager] 技能未精通，无法装备为支援技能: {skillId}");
                return false;
            }

            // 扩展列表
            while (_equippedSupportSkillIds.Count <= slotIndex)
            {
                _equippedSupportSkillIds.Add("");
            }

            var oldSkillId = _equippedSupportSkillIds[slotIndex];
            _equippedSupportSkillIds[slotIndex] = skillId;

            OnSupportSkillChanged?.Invoke(new SupportSkillChangedEventArgs
            {
                CharacterId = _characterId,
                SlotIndex = slotIndex,
                OldSkillId = oldSkillId,
                NewSkillId = skillId
            });

            return true;
        }

        /// <summary>
        /// 卸下支援技能
        /// </summary>
        public void UnequipSupportSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _equippedSupportSkillIds.Count)
                return;

            var oldSkillId = _equippedSupportSkillIds[slotIndex];
            _equippedSupportSkillIds[slotIndex] = "";

            OnSupportSkillChanged?.Invoke(new SupportSkillChangedEventArgs
            {
                CharacterId = _characterId,
                SlotIndex = slotIndex,
                OldSkillId = oldSkillId,
                NewSkillId = ""
            });
        }

        // ===== 属性计算 =====

        /// <summary>
        /// 获取当前职业组合的总属性加成
        /// </summary>
        public JobStatBonus GetTotalStatBonus()
        {
            var bonus = new JobStatBonus();

            // 主职业加成
            if (_primaryJob != null)
            {
                var primaryBonus = _primaryJob.GetCurrentStatBonus();
                AddBonus(ref bonus, primaryBonus);
            }

            // 副职业加成 (可能打折)
            if (_secondaryJob != null)
            {
                var secondaryBonus = _secondaryJob.GetCurrentStatBonus();
                // 副职业属性可能只有一半
                // AddBonus(ref bonus, secondaryBonus, 0.5f);
                AddBonus(ref bonus, secondaryBonus);
            }

            return bonus;
        }

        private void AddBonus(ref JobStatBonus target, JobStatBonus source, float multiplier = 1f)
        {
            target.HP += (int)(source.HP * multiplier);
            target.MP += (int)(source.MP * multiplier);
            target.Attack += (int)(source.Attack * multiplier);
            target.Defense += (int)(source.Defense * multiplier);
            target.MagicAttack += (int)(source.MagicAttack * multiplier);
            target.MagicDefense += (int)(source.MagicDefense * multiplier);
            target.Speed += (int)(source.Speed * multiplier);
            target.Luck += (int)(source.Luck * multiplier);
            target.CriticalRate += source.CriticalRate * multiplier;
            target.EvasionRate += source.EvasionRate * multiplier;
            target.AccuracyRate += source.AccuracyRate * multiplier;
        }

        /// <summary>
        /// 获取可用武器类型 (主职业 + 副职业)
        /// </summary>
        public WeaponCategory GetAllowedWeapons()
        {
            var weapons = WeaponCategory.None;

            if (_primaryJob?.JobData != null)
                weapons |= _primaryJob.JobData.AllowedWeapons;

            if (_secondaryJob?.JobData != null)
                weapons |= _secondaryJob.JobData.AllowedWeapons;

            return weapons;
        }

        // ===== 辅助方法 =====

        /// <summary>
        /// 获取职业数据
        /// </summary>
        public JobDataSO GetJobData(JobType jobType)
        {
            return _database?.GetJobData(jobType);
        }

        /// <summary>
        /// 根据 ID 获取技能
        /// </summary>
        public JobSkillSO GetSkillById(JobType jobType, string skillId)
        {
            var jobData = GetJobData(jobType);
            if (jobData == null) return null;

            foreach (var skill in jobData.Skills)
            {
                if (skill != null && skill.SkillId == skillId)
                    return skill;
            }
            return null;
        }

        /// <summary>
        /// 获取指定职业的等级
        /// </summary>
        public int GetJobLevel(JobType jobType)
        {
            if (_allJobInstances.TryGetValue(jobType, out var job))
                return job.CurrentLevel;
            return 0;
        }

        /// <summary>
        /// 获取指定职业实例
        /// </summary>
        public JobInstance GetJobInstance(JobType jobType)
        {
            _allJobInstances.TryGetValue(jobType, out var job);
            return job;
        }

        // ===== 存档 =====

        private const string SAVE_KEY = "JobManager";

        /// <summary>
        /// 导出存档数据
        /// </summary>
        public JobManagerSaveData ExportSaveData()
        {
            var data = new JobManagerSaveData
            {
                CharacterId = _characterId,
                PrimaryJobType = _primaryJob?.JobType ?? JobType.None,
                SecondaryJobType = _secondaryJob?.JobType ?? JobType.None,
                UnlockedJobs = new List<JobType>(_unlockedJobs),
                JobInstances = new List<JobInstanceSaveData>(),
                EquippedSupportSkillIds = new List<string>(_equippedSupportSkillIds)
            };

            foreach (var kvp in _allJobInstances)
            {
                data.JobInstances.Add(kvp.Value.ExportSaveData());
            }

            return data;
        }

        /// <summary>
        /// 导入存档数据
        /// </summary>
        public void ImportSaveData(JobManagerSaveData data)
        {
            if (data == null) return;

            _characterId = data.CharacterId;
            _unlockedJobs = data.UnlockedJobs ?? new List<JobType>();
            _equippedSupportSkillIds = data.EquippedSupportSkillIds ?? new List<string>();

            // 恢复职业实例
            _allJobInstances.Clear();
            foreach (var instanceData in data.JobInstances)
            {
                if (string.IsNullOrEmpty(instanceData.JobDataName)) continue;

                var jobData = _database?.GetJobDataByName(instanceData.JobDataName);
                if (jobData == null) continue;

                var instance = new JobInstance(jobData);
                instance.ImportSaveData(instanceData);
                _allJobInstances[jobData.JobType] = instance;
            }

            // 恢复主副职业
            if (data.PrimaryJobType != JobType.None)
            {
                _primaryJob = GetOrCreateJobInstance(data.PrimaryJobType);
            }

            if (data.SecondaryJobType != JobType.None)
            {
                _secondaryJob = GetOrCreateJobInstance(data.SecondaryJobType);
            }
        }
    }

    /// <summary>
    /// 职业管理器存档数据
    /// </summary>
    [Serializable]
    public class JobManagerSaveData
    {
        public string CharacterId;
        public JobType PrimaryJobType;
        public JobType SecondaryJobType;
        public List<JobType> UnlockedJobs;
        public List<JobInstanceSaveData> JobInstances;
        public List<string> EquippedSupportSkillIds;
    }
}
