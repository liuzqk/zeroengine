using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.TalentTree
{
    /// <summary>
    /// 天赋节点定义
    /// </summary>
    [CreateAssetMenu(fileName = "NewTalentNode", menuName = "ZeroEngine/TalentTree/Talent Node")]
    public class TalentNodeSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("节点唯一 ID")]
        public string NodeId;

        [Tooltip("显示名称")]
        public string DisplayName;

        [Tooltip("节点描述")]
        [TextArea]
        public string Description;

        [Tooltip("节点图标")]
        public Sprite Icon;

        [Tooltip("节点类型")]
        public TalentNodeType NodeType = TalentNodeType.Normal;

        [Header("等级配置")]
        [Tooltip("最大等级")]
        [Min(1)]
        public int MaxLevel = 1;

        [Tooltip("每级消耗点数")]
        public int PointCostPerLevel = 1;

        [Header("前置条件")]
        [Tooltip("前置节点（需要解锁才能点此节点）")]
        public List<TalentNodeSO> Prerequisites = new List<TalentNodeSO>();

        [Tooltip("前置节点的最低等级要求")]
        public int PrerequisiteMinLevel = 1;

        [Tooltip("需要的角色等级")]
        public int RequiredCharacterLevel = 1;

        [Header("效果")]
        [Tooltip("天赋效果列表")]
        [SerializeReference]
        public List<TalentEffect> Effects = new List<TalentEffect>();

        [Header("编辑器")]
        [Tooltip("节点在编辑器中的位置")]
        public Vector2 EditorPosition;

        /// <summary>
        /// 获取指定等级的总消耗点数
        /// </summary>
        public int GetTotalCost(int level)
        {
            return PointCostPerLevel * level;
        }

        /// <summary>
        /// 获取从当前等级升级到下一级的消耗
        /// </summary>
        public int GetUpgradeCost(int currentLevel)
        {
            if (currentLevel >= MaxLevel) return 0;
            return PointCostPerLevel;
        }

        /// <summary>
        /// 检查前置条件是否满足
        /// </summary>
        public bool CheckPrerequisites(Func<TalentNodeSO, int> getLevelFunc, int characterLevel = 0)
        {
            // 检查角色等级
            if (characterLevel > 0 && characterLevel < RequiredCharacterLevel)
                return false;

            // 检查前置节点
            foreach (var prereq in Prerequisites)
            {
                if (prereq == null) continue;
                int level = getLevelFunc(prereq);
                if (level < PrerequisiteMinLevel)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 应用所有效果
        /// </summary>
        public void ApplyEffects(GameObject owner, int level)
        {
            foreach (var effect in Effects)
            {
                effect?.Apply(owner, level);
            }
        }

        /// <summary>
        /// 移除所有效果
        /// </summary>
        public void RemoveEffects(GameObject owner)
        {
            foreach (var effect in Effects)
            {
                effect?.Remove(owner);
            }
        }

        /// <summary>
        /// 获取效果描述
        /// </summary>
        public string GetEffectsDescription(int level)
        {
            var descriptions = new List<string>();
            foreach (var effect in Effects)
            {
                if (effect != null)
                {
                    descriptions.Add(effect.GetDescription(level));
                }
            }
            return string.Join("\n", descriptions);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(NodeId))
            {
                NodeId = name.ToLowerInvariant().Replace(" ", "_");
            }
            if (string.IsNullOrEmpty(DisplayName))
            {
                DisplayName = name;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("添加属性效果")]
        private void AddStatEffect()
        {
            Effects.Add(new StatModifierEffect());
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("添加多属性效果")]
        private void AddMultiStatEffect()
        {
            Effects.Add(new MultiStatModifierEffect());
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("添加 Buff 效果")]
        private void AddBuffEffect()
        {
            Effects.Add(new BuffEffect());
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("添加技能解锁效果")]
        private void AddAbilityEffect()
        {
            Effects.Add(new UnlockAbilityEffect());
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("添加自定义效果")]
        private void AddCustomEffect()
        {
            Effects.Add(new CustomEffect());
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
