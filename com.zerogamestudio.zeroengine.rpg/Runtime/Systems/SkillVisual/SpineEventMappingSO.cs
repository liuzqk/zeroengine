// ============================================================================
// ZeroEngine v2.7.0 - Skill Visual System
// Spine 事件映射配置 (需要 SPINE_UNITY 定义)
// ============================================================================

#if SPINE_UNITY

using System;
using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace ZeroEngine.RPG.SkillVisual
{
    /// <summary>
    /// Spine 事件映射条目
    /// </summary>
    [Serializable]
    public class SpineEventMappingEntry
    {
#if ODIN_INSPECTOR
        [HorizontalGroup("Entry", 0.3f)]
#endif
        [Tooltip("Spine 动画事件名称")]
        public string EventName;

#if ODIN_INSPECTOR
        [HorizontalGroup("Entry", 0.7f)]
        [ListDrawerSettings(ShowFoldout = false)]
#endif
        [SerializeReference]
        [Tooltip("触发的视觉事件列表")]
        public List<VisualEvent> Events = new List<VisualEvent>();
    }

    /// <summary>
    /// Spine 事件映射配置 (ScriptableObject)
    /// 将 Spine 动画事件名映射到 VisualEvent 列表
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpineEventMapping", menuName = "ZeroEngine/RPG/Spine Event Mapping")]
    public class SpineEventMappingSO : ScriptableObject
    {
#if ODIN_INSPECTOR
        [Title("Spine Event Mapping")]
        [InfoBox("将 Spine 动画中的事件名映射到 VisualEvent 列表。当 Spine 播放动画时，触发对应的视觉事件。")]
        [ListDrawerSettings(ShowItemCount = true)]
#endif
        [Tooltip("事件映射条目列表")]
        public List<SpineEventMappingEntry> Entries = new List<SpineEventMappingEntry>();

        // ========================================
        // 运行时查询
        // ========================================

        private Dictionary<string, List<VisualEvent>> _lookup;

        /// <summary>
        /// 获取事件对应的视觉事件列表
        /// </summary>
        public List<VisualEvent> GetEvents(string eventName)
        {
            BuildLookupIfNeeded();

            if (_lookup.TryGetValue(eventName, out var events))
            {
                return events;
            }
            return null;
        }

        /// <summary>
        /// 检查是否包含指定事件
        /// </summary>
        public bool HasEvent(string eventName)
        {
            BuildLookupIfNeeded();
            return _lookup.ContainsKey(eventName);
        }

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<string, List<VisualEvent>>();
            foreach (var entry in Entries)
            {
                if (!string.IsNullOrEmpty(entry.EventName))
                {
                    _lookup[entry.EventName] = entry.Events;
                }
            }
        }

        private void OnValidate()
        {
            // Inspector 修改时清除缓存
            _lookup = null;
        }
    }
}

#endif // SPINE_UNITY
