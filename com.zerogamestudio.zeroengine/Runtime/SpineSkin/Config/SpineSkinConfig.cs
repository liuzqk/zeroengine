using System;
using System.Collections.Generic;
using UnityEngine;
#if SPINE_UNITY
using Spine.Unity;
#endif

namespace ZeroEngine.SpineSkin
{
    /// <summary>
    /// 皮肤槽位配置
    /// </summary>
    [Serializable]
    public class SkinSlotConfig
    {
        [Tooltip("槽位唯一ID，用于代码引用")]
        public string SlotId;
        
        [Tooltip("显示名称，用于UI")]
        public string DisplayName;
        
        [Tooltip("分类图标")]
        public Sprite Icon;
        
        [Tooltip("是否必须装备（无法卸下）")]
        public bool IsRequired;
        
        [Tooltip("排序顺序")]
        public int SortOrder;
        
        [Tooltip("默认皮肤名称")]
        public string DefaultSkin;
    }
    
    /// <summary>
    /// Spine换装系统主配置
    /// </summary>
    [CreateAssetMenu(menuName = "ZeroEngine/SpineSkin/Config", fileName = "SpineSkinConfig")]
    public class SpineSkinConfig : ScriptableObject
    {
        [Header("Spine数据")]
#if SPINE_UNITY
        [Tooltip("Spine骨骼数据资源")]
        public SkeletonDataAsset SkeletonDataAsset;
        
        [Tooltip("预览材质")]
        public Material PreviewMaterial;
#endif
        
        [Header("命名规范")]
        [Tooltip("皮肤命名模式，支持占位符: {gender}, {slot}, {name}\n例如: {gender}/{slot}/{name}")]
        public string SkinNamePattern = "{gender}/{slot}/{name}";
        
        [Tooltip("性别名称列表")]
        public List<string> GenderNames = new() { "Female", "Male" };
        
        [Tooltip("默认性别索引")]
        public int DefaultGenderIndex = 0;
        
        [Header("角色限制")]
        [Tooltip("最大角色数量，0表示无限制")]
        [Min(0)]
        public int MaxCharacterCount = 10;
        
        [Header("皮肤槽位")]
        [Tooltip("可配置的皮肤槽位列表")]
        public List<SkinSlotConfig> SkinSlots = new();
        
        [Header("UI设置")]
        [Tooltip("UI动画时长")]
        [Min(0)]
        public float AnimationDuration = 0.3f;
        
        [Tooltip("按钮出现延迟间隔")]
        [Min(0)]
        public float ButtonAppearDelay = 0.03f;
        
        [Tooltip("空槽位图标")]
        public Sprite EmptySlotIcon;
        
        /// <summary>
        /// 根据命名模式解析皮肤名称
        /// </summary>
        public bool TryParseSkinName(string fullSkinName, out string gender, out string slot, out string name)
        {
            gender = null;
            slot = null;
            name = null;
            
            if (string.IsNullOrEmpty(fullSkinName)) return false;
            
            // 简单实现：假设分隔符是 /
            var parts = fullSkinName.Split('/');
            var patternParts = SkinNamePattern.Split('/');
            
            if (parts.Length != patternParts.Length) return false;
            
            for (int i = 0; i < patternParts.Length; i++)
            {
                switch (patternParts[i])
                {
                    case "{gender}":
                        gender = parts[i];
                        break;
                    case "{slot}":
                        slot = parts[i];
                        break;
                    case "{name}":
                        name = parts[i];
                        break;
                }
            }
            
            return !string.IsNullOrEmpty(slot);
        }
        
        /// <summary>
        /// 根据命名模式构建皮肤名称
        /// </summary>
        public string BuildSkinName(string gender, string slot, string name)
        {
            return SkinNamePattern
                .Replace("{gender}", gender ?? "")
                .Replace("{slot}", slot ?? "")
                .Replace("{name}", name ?? "");
        }
        
        /// <summary>
        /// 获取槽位配置
        /// </summary>
        public SkinSlotConfig GetSlotConfig(string slotId)
        {
            return SkinSlots.Find(s => s.SlotId == slotId);
        }
        
        /// <summary>
        /// 获取默认性别名称
        /// </summary>
        public string DefaultGender => 
            GenderNames != null && GenderNames.Count > DefaultGenderIndex 
                ? GenderNames[DefaultGenderIndex] 
                : "Female";
    }
}
