// ============================================================================
// ZeroEngine v2.7.0 - Skill Visual System
// 播放 Timeline 事件 (需要 com.unity.timeline 包)
// ============================================================================

#if UNITY_TIMELINE

using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

#if ZEROENGINE_DOTWEEN
using DG.Tweening;
#endif

namespace ZeroEngine.RPG.SkillVisual
{
    /// <summary>
    /// 播放 Timeline 事件 - 在施法者上播放 Timeline 资源
    /// </summary>
    [Serializable]
#if ODIN_INSPECTOR
    [LabelText("Play Timeline")]
#endif
    public class PlayTimelineEvent : VisualEvent
    {
#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("要播放的 Timeline 资源")]
        public TimelineAsset TimelineAsset;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("是否等待 Timeline 播放完毕才继续序列")]
        public bool WaitForCompletion;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("播放速度")]
        [Range(0.1f, 3f)]
        public float Speed = 1f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("在谁身上播放")]
        public SpawnTarget PlayOn = SpawnTarget.Caster;

#if ZEROENGINE_DOTWEEN
        public override void AddToSequence(Sequence sequence, VisualContext context)
        {
            if (!Enabled) return;

            sequence.InsertCallback(Delay, () => PlayTimeline(context));

            // 如果需要等待 Timeline 播放完毕
            if (WaitForCompletion && TimelineAsset != null)
            {
                float duration = (float)TimelineAsset.duration / Speed;
                sequence.InsertCallback(Delay + duration, () => { }); // 空回调占位
            }
        }
#endif

        public override void Execute(VisualContext context)
        {
            if (!Enabled) return;
            PlayTimeline(context);
        }

        private void PlayTimeline(VisualContext context)
        {
            if (TimelineAsset == null) return;

            GameObject target = GetSpawnTarget(context, PlayOn);
            if (target == null) return;

            // 获取或添加 PlayableDirector
            var director = target.GetComponent<PlayableDirector>();
            if (director == null)
            {
                director = target.AddComponent<PlayableDirector>();
            }

            // 配置 Director
            director.playableAsset = TimelineAsset;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
            director.playOnAwake = false;

            // 设置速度
            // 注意：PlayableDirector 没有直接的 speed 属性
            // 需要通过 PlayableGraph 设置
            if (Speed != 1f)
            {
                director.played += _ =>
                {
                    if (director.playableGraph.IsValid())
                    {
                        director.playableGraph.GetRootPlayable(0).SetSpeed(Speed);
                    }
                };
            }

            // 播放
            director.Play();
        }
    }
}

#endif // UNITY_TIMELINE
