// ============================================================================
// ZeroEngine v2.7.0 - Skill Visual System
// Spine 事件桥接器 (需要 SPINE_UNITY 定义)
// ============================================================================

#if SPINE_UNITY

using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

#if ZEROENGINE_DOTWEEN
using DG.Tweening;
#endif

namespace ZeroEngine.RPG.SkillVisual
{
    /// <summary>
    /// Spine 事件桥接器 - 将 Spine 动画事件转换为 VisualEvent 调用
    /// </summary>
    [RequireComponent(typeof(SkeletonAnimation))]
    public class SpineEventBridge : MonoBehaviour
    {
        private SkeletonAnimation _skeletonAnimation;
        private SpineEventMappingSO _currentMapping;
        private Dictionary<string, List<VisualEvent>> _eventLookup;
        private VisualContext _currentContext;

        private void Awake()
        {
            _skeletonAnimation = GetComponent<SkeletonAnimation>();
            _eventLookup = new Dictionary<string, List<VisualEvent>>();
        }

        private void OnEnable()
        {
            if (_skeletonAnimation != null)
            {
                _skeletonAnimation.AnimationState.Event += HandleAnimationEvent;
            }
        }

        private void OnDisable()
        {
            if (_skeletonAnimation != null)
            {
                _skeletonAnimation.AnimationState.Event -= HandleAnimationEvent;
            }
        }

        /// <summary>
        /// 设置当前事件映射
        /// </summary>
        public void SetEventMapping(SpineEventMappingSO mapping)
        {
            _currentMapping = mapping;
            _eventLookup.Clear();

            if (_currentMapping == null) return;

            foreach (var entry in _currentMapping.Entries)
            {
                if (!string.IsNullOrEmpty(entry.EventName))
                {
                    _eventLookup[entry.EventName] = entry.Events;
                }
            }
        }

        /// <summary>
        /// 设置当前视觉上下文 (用于事件触发)
        /// </summary>
        public void SetContext(VisualContext context)
        {
            _currentContext = context;
        }

        private void HandleAnimationEvent(Spine.TrackEntry trackEntry, Spine.Event e)
        {
            if (!_eventLookup.TryGetValue(e.Data.Name, out var visualEvents))
                return;

            // 创建上下文
            var context = _currentContext ?? new VisualContext
            {
                Caster = gameObject,
                Target = null
            };

            foreach (var visualEvent in visualEvents)
            {
                if (visualEvent == null || !visualEvent.Enabled) continue;

#if ZEROENGINE_DOTWEEN
                // 从 Spine 事件触发的视觉事件是立即执行的
                // 忽略 Delay 字段
                var sequence = DOTween.Sequence();
                visualEvent.AddToSequence(sequence, context);
                sequence.Play();
#else
                visualEvent.Execute(context);
#endif
            }
        }
    }
}

#endif // SPINE_UNITY
