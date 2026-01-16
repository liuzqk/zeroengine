// ============================================================================
// ZeroEngine v2.7.0 - Skill Visual System
// 播放 Animator 动画事件
// ============================================================================

using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

#if ZEROENGINE_DOTWEEN
using DG.Tweening;
#endif

namespace ZeroEngine.RPG.SkillVisual
{
    /// <summary>
    /// 播放 Animator 动画事件 - 触发 Animator 状态或参数
    /// </summary>
    [Serializable]
#if ODIN_INSPECTOR
    [LabelText("Play Animation")]
#endif
    public class PlayAnimationEvent : VisualEvent
    {
#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("动画触发方式")]
        public AnimationTriggerType TriggerType = AnimationTriggerType.Trigger;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("参数/状态名称")]
        public string ParameterName;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings"), ShowIf("TriggerType", AnimationTriggerType.SetBool)]
#endif
        [Tooltip("Bool 值")]
        public bool BoolValue = true;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings"), ShowIf("TriggerType", AnimationTriggerType.SetFloat)]
#endif
        [Tooltip("Float 值")]
        public float FloatValue;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings"), ShowIf("TriggerType", AnimationTriggerType.SetInteger)]
#endif
        [Tooltip("Integer 值")]
        public int IntValue;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings"), ShowIf("TriggerType", AnimationTriggerType.CrossFade)]
#endif
        [Tooltip("过渡时间")]
        public float TransitionDuration = 0.1f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("在谁身上播放")]
        public SpawnTarget PlayOn = SpawnTarget.Caster;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("Animator 层级")]
        public int Layer = 0;

#if ZEROENGINE_DOTWEEN
        public override void AddToSequence(Sequence sequence, VisualContext context)
        {
            if (!Enabled) return;

            sequence.InsertCallback(Delay, () => PlayAnimation(context));
        }
#endif

        public override void Execute(VisualContext context)
        {
            if (!Enabled) return;
            PlayAnimation(context);
        }

        private void PlayAnimation(VisualContext context)
        {
            if (string.IsNullOrEmpty(ParameterName)) return;

            GameObject target = GetSpawnTarget(context, PlayOn);
            if (target == null) return;

            var animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"[PlayAnimationEvent] Animator not found on {target.name}");
                return;
            }

            switch (TriggerType)
            {
                case AnimationTriggerType.Trigger:
                    animator.SetTrigger(ParameterName);
                    break;

                case AnimationTriggerType.SetBool:
                    animator.SetBool(ParameterName, BoolValue);
                    break;

                case AnimationTriggerType.SetFloat:
                    animator.SetFloat(ParameterName, FloatValue);
                    break;

                case AnimationTriggerType.SetInteger:
                    animator.SetInteger(ParameterName, IntValue);
                    break;

                case AnimationTriggerType.CrossFade:
                    animator.CrossFade(ParameterName, TransitionDuration, Layer);
                    break;

                case AnimationTriggerType.Play:
                    animator.Play(ParameterName, Layer);
                    break;
            }
        }
    }

    /// <summary>
    /// 动画触发类型
    /// </summary>
    public enum AnimationTriggerType
    {
        /// <summary>触发 Trigger 参数</summary>
        Trigger,
        /// <summary>设置 Bool 参数</summary>
        SetBool,
        /// <summary>设置 Float 参数</summary>
        SetFloat,
        /// <summary>设置 Integer 参数</summary>
        SetInteger,
        /// <summary>CrossFade 过渡到状态</summary>
        CrossFade,
        /// <summary>直接播放状态</summary>
        Play
    }
}
