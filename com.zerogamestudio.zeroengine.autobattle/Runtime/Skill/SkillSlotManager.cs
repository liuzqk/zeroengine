using System;
using System.Collections.Generic;
using ZeroEngine.AutoBattle.Grid;
using ZeroEngine.AutoBattle.Battle;

namespace ZeroEngine.AutoBattle.Skill
{
    /// <summary>
    /// 技能槽位管理器
    /// </summary>
    public class SkillSlotManager
    {
        /// <summary>
        /// 技能槽位数量
        /// </summary>
        public int SlotCount { get; }

        /// <summary>
        /// 已装备的技能
        /// </summary>
        private readonly SkillData[] _equippedSkills;

        /// <summary>
        /// 技能冷却时间
        /// </summary>
        private readonly Dictionary<string, float> _cooldowns = new();

        public SkillSlotManager(int slotCount)
        {
            SlotCount = slotCount;
            _equippedSkills = new SkillData[slotCount];
        }

        /// <summary>
        /// 装备技能到指定槽位
        /// </summary>
        public bool EquipSkill(SkillData skill, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
                return false;

            _equippedSkills[slotIndex] = skill;
            return true;
        }

        /// <summary>
        /// 卸载指定槽位的技能
        /// </summary>
        public SkillData UnequipSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
                return null;

            var skill = _equippedSkills[slotIndex];
            _equippedSkills[slotIndex] = null;
            return skill;
        }

        /// <summary>
        /// 获取指定槽位的技能
        /// </summary>
        public SkillData GetSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
                return null;
            return _equippedSkills[slotIndex];
        }

        /// <summary>
        /// 获取所有已装备的技能
        /// </summary>
        public IEnumerable<SkillData> GetEquippedSkills()
        {
            foreach (var skill in _equippedSkills)
            {
                if (skill != null)
                    yield return skill;
            }
        }

        /// <summary>
        /// 更新所有技能冷却
        /// </summary>
        public void UpdateCooldowns(float deltaTime)
        {
            var keysToRemove = new List<string>();
            var keysToUpdate = new List<string>(_cooldowns.Keys);

            foreach (var key in keysToUpdate)
            {
                float newValue = _cooldowns[key] - deltaTime;
                if (newValue <= 0)
                {
                    keysToRemove.Add(key);
                }
                else
                {
                    _cooldowns[key] = newValue;
                }
            }

            foreach (var key in keysToRemove)
            {
                _cooldowns.Remove(key);
            }
        }

        /// <summary>
        /// 开始技能冷却
        /// </summary>
        public void StartCooldown(SkillData skill)
        {
            _cooldowns[skill.SkillId] = skill.Cooldown;
        }

        /// <summary>
        /// 检查技能是否可用
        /// </summary>
        public bool IsSkillReady(SkillData skill)
        {
            return !_cooldowns.ContainsKey(skill.SkillId);
        }

        /// <summary>
        /// 获取技能剩余冷却时间
        /// </summary>
        public float GetRemainingCooldown(SkillData skill)
        {
            return _cooldowns.TryGetValue(skill.SkillId, out float cd) ? cd : 0f;
        }

        /// <summary>
        /// 获取第一个可用的技能
        /// </summary>
        public SkillData GetAvailableSkill(IBattleUnit owner, IBattleUnit target)
        {
            foreach (var skill in _equippedSkills)
            {
                if (skill != null && IsSkillReady(skill) && skill.CanUse(owner, target))
                {
                    return skill;
                }
            }
            return null;
        }

        /// <summary>
        /// 重置所有冷却
        /// </summary>
        public void ResetAllCooldowns()
        {
            _cooldowns.Clear();
        }
    }
}
