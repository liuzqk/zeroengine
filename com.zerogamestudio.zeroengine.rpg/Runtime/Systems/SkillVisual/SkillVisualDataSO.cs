// ============================================================================
// ZeroEngine v2.7.0 - Skill Visual System
// 技能表现数据 - ScriptableObject 配置资产
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

#if ZEROENGINE_DOTWEEN
using DG.Tweening;
#endif

#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace ZeroEngine.RPG.SkillVisual
{
    /// <summary>
    /// 技能表现数据 (ScriptableObject)
    /// 内联多态事件列表，无碎片文件，支持时间轴排序
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillVisual", menuName = "ZeroEngine/RPG/Skill Visual Data")]
    public class SkillVisualDataSO : ScriptableObject
    {
#if ODIN_INSPECTOR
        [Title("技能表现配置")]
        [InfoBox("将多个视觉事件组合成一个技能表现序列。事件按 Delay 时间执行。")]
#endif
        [Tooltip("技能显示名称")]
        public string DisplayName;

        [Tooltip("技能描述")]
        [TextArea(2, 4)]
        public string Description;

#if ODIN_INSPECTOR
        [Title("Visual Events Sequence")]
        [ListDrawerSettings(CustomAddFunction = "AddEvent", ShowItemCount = true)]
#endif
        [SerializeReference] // 关键：允许列表中存储不同子类的实例
        public List<VisualEvent> Events = new List<VisualEvent>();

        [Tooltip("序列总时长 (0 = 自动计算)")]
        public float TotalDuration;

        // ========================================
        // 运行时 API
        // ========================================

#if ZEROENGINE_DOTWEEN
        /// <summary>
        /// 创建 DOTween 序列 (需要手动 Play)
        /// </summary>
        public Sequence CreateSequence(VisualContext context)
        {
            var sequence = DOTween.Sequence();

            foreach (var visualEvent in Events)
            {
                if (visualEvent != null && visualEvent.Enabled)
                {
                    visualEvent.AddToSequence(sequence, context);
                }
            }

            return sequence;
        }

        /// <summary>
        /// 创建并播放 DOTween 序列
        /// </summary>
        public Sequence Play(VisualContext context)
        {
            var sequence = CreateSequence(context);
            sequence.Play();
            return sequence;
        }

        /// <summary>
        /// 播放并在完成时回调
        /// </summary>
        public Sequence Play(VisualContext context, Action onComplete)
        {
            var sequence = CreateSequence(context);
            sequence.OnComplete(() => onComplete?.Invoke());
            sequence.Play();
            return sequence;
        }
#endif

        /// <summary>
        /// 立即执行所有事件 (不使用 DOTween)
        /// </summary>
        public void ExecuteImmediate(VisualContext context)
        {
            foreach (var visualEvent in Events)
            {
                if (visualEvent != null && visualEvent.Enabled)
                {
                    visualEvent.Execute(context);
                }
            }
        }

        /// <summary>
        /// 获取序列总时长
        /// </summary>
        public float GetDuration()
        {
            if (TotalDuration > 0) return TotalDuration;

            float maxDelay = 0f;
            foreach (var e in Events)
            {
                if (e != null && e.Delay > maxDelay)
                    maxDelay = e.Delay;
            }
            return maxDelay;
        }

        // ========================================
        // Editor Tools
        // ========================================

#if UNITY_EDITOR && ODIN_INSPECTOR
        private void AddEvent()
        {
            var eventTypes = GetEventTypes();
            var picker = new GenericSelector<Type>("Add Visual Event", false, eventTypes);

            picker.SelectionConfirmed += selection =>
            {
                var selectedType = selection.FirstOrDefault();
                if (selectedType != null)
                {
                    var newEvent = (VisualEvent)Activator.CreateInstance(selectedType);
                    Events.Add(newEvent);
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            };
            picker.ShowInPopup();
        }

        private IEnumerable<Type> GetEventTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(type => type.IsSubclassOf(typeof(VisualEvent)) && !type.IsAbstract)
                .OrderBy(type => type.Name);
        }
#endif

#if ODIN_INSPECTOR
        [Button("Sort Events By Delay", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
#endif
        private void SortEventsByDelay()
        {
            Events = Events.Where(e => e != null).OrderBy(e => e.Delay).ToList();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

#if ODIN_INSPECTOR
        [Button("Calculate Duration", ButtonSizes.Medium)]
#endif
        private void CalculateDuration()
        {
            TotalDuration = GetDuration();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
