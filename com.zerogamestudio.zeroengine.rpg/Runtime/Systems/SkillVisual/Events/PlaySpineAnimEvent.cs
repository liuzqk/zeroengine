// ============================================================================
// ZeroEngine v2.7.0 - Skill Visual System
// 播放 Spine 动画事件 (需要 SPINE_UNITY 定义)
// ============================================================================

#if SPINE_UNITY

using System;
using UnityEngine;
using Spine.Unity;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

#if ZEROENGINE_DOTWEEN
using DG.Tweening;
#endif

namespace ZeroEngine.RPG.SkillVisual
{
    /// <summary>
    /// 播放 Spine 动画事件 - 触发 Spine 骨骼动画
    /// 需要项目定义 SPINE_UNITY 宏
    /// </summary>
    [Serializable]
#if ODIN_INSPECTOR
    [LabelText("Play Spine Animation")]
#endif
    public class PlaySpineAnimEvent : VisualEvent
    {
#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("要播放的动画名称")]
        public string AnimationName;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("Spine 动画的轨道")]
        public int TrackIndex;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("是否循环播放")]
        public bool Loop;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("混合时间")]
        public float MixDuration = 0.2f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("在谁身上播放")]
        public SpawnTarget PlayOn = SpawnTarget.Caster;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("此动画关联的 Spine 事件映射 (可选)")]
        public SpineEventMappingSO EventMapping;

#if ZEROENGINE_DOTWEEN
        public override void AddToSequence(Sequence sequence, VisualContext context)
        {
            if (!Enabled) return;

            sequence.InsertCallback(Delay, () => PlaySpineAnim(context));
        }
#endif

        public override void Execute(VisualContext context)
        {
            if (!Enabled) return;
            PlaySpineAnim(context);
        }

        private void PlaySpineAnim(VisualContext context)
        {
            if (string.IsNullOrEmpty(AnimationName)) return;

            GameObject target = GetSpawnTarget(context, PlayOn);
            if (target == null) return;

            // 设置事件映射
            var bridge = target.GetComponent<SpineEventBridge>();
            if (bridge != null && EventMapping != null)
            {
                bridge.SetEventMapping(EventMapping);
            }

            // 播放动画
            var skeletonAnimation = target.GetComponent<SkeletonAnimation>();
            if (skeletonAnimation != null)
            {
                var trackEntry = skeletonAnimation.AnimationState.SetAnimation(TrackIndex, AnimationName, Loop);
                if (trackEntry != null)
                {
                    trackEntry.MixDuration = MixDuration;
                }
            }
            else
            {
                // 尝试 SkeletonGraphic (UI)
                var skeletonGraphic = target.GetComponent<SkeletonGraphic>();
                if (skeletonGraphic != null)
                {
                    var trackEntry = skeletonGraphic.AnimationState.SetAnimation(TrackIndex, AnimationName, Loop);
                    if (trackEntry != null)
                    {
                        trackEntry.MixDuration = MixDuration;
                    }
                }
                else
                {
                    Debug.LogWarning($"[PlaySpineAnimEvent] SkeletonAnimation/SkeletonGraphic not found on {target.name}");
                }
            }
        }
    }
}

#endif // SPINE_UNITY
