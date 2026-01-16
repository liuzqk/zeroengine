// ============================================================================
// JobInstance.cs
// 运行时职业实例
// 创建于: 2026-01-07
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Character.Job
{
    /// <summary>
    /// 运行时职业实例
    /// 管理职业等级、经验、已学技能
    /// </summary>
    [Serializable]
    public class JobInstance
    {
        // ===== 基础数据 =====

        [SerializeField]
        private JobDataSO _jobData;

        [SerializeField]
        private int _currentLevel = 1;

        [SerializeField]
        private int _currentJP;

        [SerializeField]
        private int _totalJPEarned;

        // ===== 技能数据 =====

        [SerializeField]
        private List<string> _learnedSkillIds = new List<string>();

        [SerializeField]
        private List<string> _masteredSkillIds = new List<string>();

        [SerializeField]
        private List<string> _unlockedPassiveIds = new List<string>();

        // ===== 事件 =====

        /// <summary>职业升级事件</summary>
        public event Action<int, int> OnLevelUp;

        /// <summary>获得 JP 事件</summary>
        public event Action<int> OnJPGained;

        /// <summary>技能学习事件</summary>
        public event Action<JobSkillSO> OnSkillLearned;

        /// <summary>技能精通事件</summary>
        public event Action<JobSkillSO> OnSkillMastered;

        /// <summary>被动解锁事件</summary>
        public event Action<JobPassiveSO> OnPassiveUnlocked;

        // ===== 属性 =====

        /// <summary>职业配置数据</summary>
        public JobDataSO JobData => _jobData;

        /// <summary>职业类型</summary>
        public JobType JobType => _jobData != null ? _jobData.JobType : JobType.None;

        /// <summary>当前等级</summary>
        public int CurrentLevel => _currentLevel;

        /// <summary>当前 JP</summary>
        public int CurrentJP => _currentJP;

        /// <summary>累计获得 JP</summary>
        public int TotalJPEarned => _totalJPEarned;

        /// <summary>是否满级</summary>
        public bool IsMaxLevel => _jobData != null && _currentLevel >= _jobData.MaxJobLevel;

        /// <summary>升级所需 JP</summary>
        public int JPToNextLevel => _jobData?.GetJPForNextLevel(_currentLevel) ?? 0;

        /// <summary>升级进度 (0-1)</summary>
        public float LevelProgress
        {
            get
            {
                int required = JPToNextLevel;
                if (required <= 0) return 1f;
                return Mathf.Clamp01((float)_currentJP / required);
            }
        }

        /// <summary>已学技能 ID 集合</summary>
        public IReadOnlyList<string> LearnedSkillIds => _learnedSkillIds;

        /// <summary>已精通技能 ID 集合</summary>
        public IReadOnlyList<string> MasteredSkillIds => _masteredSkillIds;

        /// <summary>已解锁被动 ID 集合</summary>
        public IReadOnlyList<string> UnlockedPassiveIds => _unlockedPassiveIds;

        // ===== 构造函数 =====

        /// <summary>
        /// 创建职业实例
        /// </summary>
        public JobInstance(JobDataSO jobData, int initialLevel = 1)
        {
            _jobData = jobData;
            _currentLevel = Mathf.Clamp(initialLevel, 1, jobData?.MaxJobLevel ?? 99);
            _currentJP = 0;
            _totalJPEarned = 0;
        }

        /// <summary>
        /// 无参构造 (序列化用)
        /// </summary>
        public JobInstance() { }

        // ===== JP 与升级 =====

        /// <summary>
        /// 获得 JP
        /// </summary>
        /// <param name="amount">JP 数量</param>
        /// <returns>实际获得量</returns>
        public int AddJP(int amount)
        {
            if (amount <= 0 || _jobData == null)
                return 0;

            _currentJP += amount;
            _totalJPEarned += amount;
            OnJPGained?.Invoke(amount);

            // 检查升级
            CheckLevelUp();

            return amount;
        }

        /// <summary>
        /// 消耗 JP (学习技能)
        /// </summary>
        public bool SpendJP(int amount)
        {
            if (amount <= 0 || _currentJP < amount)
                return false;

            _currentJP -= amount;
            return true;
        }

        /// <summary>
        /// 检查并执行升级
        /// </summary>
        private void CheckLevelUp()
        {
            if (_jobData == null || IsMaxLevel)
                return;

            int requiredJP = JPToNextLevel;
            int levelUps = 0;
            int oldLevel = _currentLevel;

            while (_currentJP >= requiredJP && _currentLevel < _jobData.MaxJobLevel)
            {
                _currentJP -= requiredJP;
                _currentLevel++;
                levelUps++;
                requiredJP = _jobData.GetJPForNextLevel(_currentLevel);

                // 检查被动解锁
                CheckPassiveUnlocks();
            }

            if (levelUps > 0)
            {
                OnLevelUp?.Invoke(oldLevel, _currentLevel);
            }
        }

        /// <summary>
        /// 强制设置等级 (调试/GM 命令)
        /// </summary>
        public void SetLevel(int level)
        {
            if (_jobData == null) return;

            int oldLevel = _currentLevel;
            _currentLevel = Mathf.Clamp(level, 1, _jobData.MaxJobLevel);

            if (_currentLevel != oldLevel)
            {
                CheckPassiveUnlocks();
                OnLevelUp?.Invoke(oldLevel, _currentLevel);
            }
        }

        // ===== 技能系统 =====

        /// <summary>
        /// 检查技能是否可学习
        /// </summary>
        public SkillLearnStatus GetSkillStatus(JobSkillSO skill)
        {
            if (skill == null)
                return SkillLearnStatus.Locked;

            // 已精通
            if (_masteredSkillIds.Contains(skill.SkillId))
                return SkillLearnStatus.Mastered;

            // 已学习
            if (_learnedSkillIds.Contains(skill.SkillId))
                return SkillLearnStatus.Learned;

            // 检查学习条件
            var learnedSet = new HashSet<string>(_learnedSkillIds);
            if (skill.CanLearn(_currentLevel, _currentJP, learnedSet))
                return SkillLearnStatus.Available;

            return SkillLearnStatus.Locked;
        }

        /// <summary>
        /// 学习技能
        /// </summary>
        /// <returns>是否成功</returns>
        public bool LearnSkill(JobSkillSO skill)
        {
            if (skill == null || _jobData == null)
                return false;

            // 检查是否已学
            if (_learnedSkillIds.Contains(skill.SkillId))
                return false;

            // 检查学习条件
            var learnedSet = new HashSet<string>(_learnedSkillIds);
            if (!skill.CanLearn(_currentLevel, _currentJP, learnedSet))
                return false;

            // 消耗 JP
            if (!SpendJP(skill.JPCost))
                return false;

            _learnedSkillIds.Add(skill.SkillId);
            OnSkillLearned?.Invoke(skill);

            return true;
        }

        /// <summary>
        /// 精通技能 (永久保留)
        /// </summary>
        /// <param name="skill">技能</param>
        /// <param name="spCost">SP 消耗 (从外部传入)</param>
        public bool MasterSkill(JobSkillSO skill, ref int availableSP)
        {
            if (skill == null || !skill.CanMaster)
                return false;

            // 必须先学习
            if (!_learnedSkillIds.Contains(skill.SkillId))
                return false;

            // 已精通
            if (_masteredSkillIds.Contains(skill.SkillId))
                return false;

            // 检查 SP
            if (availableSP < skill.MasterySPCost)
                return false;

            availableSP -= skill.MasterySPCost;
            _masteredSkillIds.Add(skill.SkillId);
            OnSkillMastered?.Invoke(skill);

            return true;
        }

        /// <summary>
        /// 检查技能是否已学习
        /// </summary>
        public bool HasLearnedSkill(string skillId)
        {
            return _learnedSkillIds.Contains(skillId) || _masteredSkillIds.Contains(skillId);
        }

        /// <summary>
        /// 检查技能是否已精通
        /// </summary>
        public bool HasMasteredSkill(string skillId)
        {
            return _masteredSkillIds.Contains(skillId);
        }

        /// <summary>
        /// 获取所有已学技能
        /// </summary>
        public List<JobSkillSO> GetLearnedSkills()
        {
            var result = new List<JobSkillSO>();
            if (_jobData == null) return result;

            foreach (var skill in _jobData.Skills)
            {
                if (skill != null && _learnedSkillIds.Contains(skill.SkillId))
                {
                    result.Add(skill);
                }
            }
            return result;
        }

        // ===== 被动系统 =====

        /// <summary>
        /// 检查并解锁被动
        /// </summary>
        private void CheckPassiveUnlocks()
        {
            if (_jobData == null) return;

            foreach (var passive in _jobData.ExclusivePassives)
            {
                if (passive == null) continue;
                if (_unlockedPassiveIds.Contains(passive.PassiveId)) continue;

                if (_currentLevel >= passive.RequiredJobLevel)
                {
                    _unlockedPassiveIds.Add(passive.PassiveId);
                    OnPassiveUnlocked?.Invoke(passive);
                }
            }
        }

        /// <summary>
        /// 检查被动是否已解锁
        /// </summary>
        public bool HasUnlockedPassive(string passiveId)
        {
            return _unlockedPassiveIds.Contains(passiveId);
        }

        /// <summary>
        /// 获取已解锁的被动列表
        /// </summary>
        public List<JobPassiveSO> GetUnlockedPassives()
        {
            var result = new List<JobPassiveSO>();
            if (_jobData == null) return result;

            foreach (var passive in _jobData.ExclusivePassives)
            {
                if (passive != null && _unlockedPassiveIds.Contains(passive.PassiveId))
                {
                    result.Add(passive);
                }
            }
            return result;
        }

        // ===== 属性计算 =====

        /// <summary>
        /// 获取当前等级的属性加成
        /// </summary>
        public JobStatBonus GetCurrentStatBonus()
        {
            return _jobData?.GetStatBonusAtLevel(_currentLevel) ?? new JobStatBonus();
        }

        // ===== 序列化 =====

        /// <summary>
        /// 导出存档数据
        /// </summary>
        public JobInstanceSaveData ExportSaveData()
        {
            return new JobInstanceSaveData
            {
                JobDataName = _jobData != null ? _jobData.name : "",
                CurrentLevel = _currentLevel,
                CurrentJP = _currentJP,
                TotalJPEarned = _totalJPEarned,
                LearnedSkillIds = new List<string>(_learnedSkillIds),
                MasteredSkillIds = new List<string>(_masteredSkillIds),
                UnlockedPassiveIds = new List<string>(_unlockedPassiveIds)
            };
        }

        /// <summary>
        /// 导入存档数据
        /// </summary>
        public void ImportSaveData(JobInstanceSaveData data)
        {
            if (data == null) return;

            _currentLevel = data.CurrentLevel;
            _currentJP = data.CurrentJP;
            _totalJPEarned = data.TotalJPEarned;
            _learnedSkillIds = data.LearnedSkillIds ?? new List<string>();
            _masteredSkillIds = data.MasteredSkillIds ?? new List<string>();
            _unlockedPassiveIds = data.UnlockedPassiveIds ?? new List<string>();
        }
    }

    /// <summary>
    /// 职业实例存档数据
    /// </summary>
    [Serializable]
    public class JobInstanceSaveData
    {
        public string JobDataName;
        public int CurrentLevel;
        public int CurrentJP;
        public int TotalJPEarned;
        public List<string> LearnedSkillIds;
        public List<string> MasteredSkillIds;
        public List<string> UnlockedPassiveIds;
    }
}
