// ============================================================================
// ZeroEngine v2.7.0 - Skill Visual System
// 移动事件 - 施法者移动到目标
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
    /// 移动类型
    /// </summary>
    public enum MoveType
    {
        /// <summary>移动到目标</summary>
        ToTarget,
        /// <summary>移动到位置</summary>
        ToPosition,
        /// <summary>返回原位</summary>
        ReturnToOrigin,
        /// <summary>相对移动</summary>
        RelativeMove,
        /// <summary>冲刺到目标前方</summary>
        DashToTarget
    }

    /// <summary>
    /// 移动事件 - 控制施法者的位移
    /// </summary>
    [Serializable]
#if ODIN_INSPECTOR
    [LabelText("Move")]
#endif
    public class MoveEvent : VisualEvent
    {
#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("移动类型")]
        public MoveType MoveType = MoveType.ToTarget;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("移动时长")]
        public float Duration = 0.3f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings"), ShowIf("MoveType", MoveType.ToPosition)]
#endif
        [Tooltip("目标位置")]
        public Vector3 TargetPosition;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings"), ShowIf("MoveType", MoveType.RelativeMove)]
#endif
        [Tooltip("相对偏移")]
        public Vector3 RelativeOffset;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings"), ShowIf("MoveType", MoveType.DashToTarget)]
#endif
        [Tooltip("距离目标的停止距离")]
        public float StopDistance = 1f;

#if ZEROENGINE_DOTWEEN
#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("缓动类型")]
        public Ease EaseType = Ease.OutQuad;
#endif

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("是否等待移动完成")]
        public bool WaitForCompletion = true;

        // 运行时存储原始位置
        private static Vector3 _savedOriginPosition;

#if ZEROENGINE_DOTWEEN
        public override void AddToSequence(Sequence sequence, VisualContext context)
        {
            if (!Enabled) return;

            switch (MoveType)
            {
                case MoveType.ToTarget:
                    AddMoveToTarget(sequence, context);
                    break;
                case MoveType.ToPosition:
                    AddMoveToPosition(sequence, context);
                    break;
                case MoveType.ReturnToOrigin:
                    AddReturnToOrigin(sequence, context);
                    break;
                case MoveType.RelativeMove:
                    AddRelativeMove(sequence, context);
                    break;
                case MoveType.DashToTarget:
                    AddDashToTarget(sequence, context);
                    break;
            }
        }

        private void AddMoveToTarget(Sequence sequence, VisualContext context)
        {
            if (context.Caster == null || context.Target == null) return;

            var casterTransform = context.Caster.transform;

            // 保存原始位置
            sequence.InsertCallback(Delay, () =>
            {
                _savedOriginPosition = casterTransform.position;
            });

            // 移动到目标
            var moveTween = casterTransform.DOMove(context.Target.transform.position, Duration)
                .SetEase(EaseType);

            if (WaitForCompletion)
            {
                sequence.Insert(Delay, moveTween);
            }
            else
            {
                sequence.InsertCallback(Delay, () => moveTween.Play());
            }
        }

        private void AddMoveToPosition(Sequence sequence, VisualContext context)
        {
            if (context.Caster == null) return;

            var casterTransform = context.Caster.transform;

            sequence.InsertCallback(Delay, () =>
            {
                _savedOriginPosition = casterTransform.position;
            });

            var moveTween = casterTransform.DOMove(TargetPosition, Duration)
                .SetEase(EaseType);

            if (WaitForCompletion)
            {
                sequence.Insert(Delay, moveTween);
            }
            else
            {
                sequence.InsertCallback(Delay, () => moveTween.Play());
            }
        }

        private void AddReturnToOrigin(Sequence sequence, VisualContext context)
        {
            if (context.Caster == null) return;

            var casterTransform = context.Caster.transform;

            var moveTween = casterTransform.DOMove(_savedOriginPosition, Duration)
                .SetEase(EaseType);

            if (WaitForCompletion)
            {
                sequence.Insert(Delay, moveTween);
            }
            else
            {
                sequence.InsertCallback(Delay, () => moveTween.Play());
            }
        }

        private void AddRelativeMove(Sequence sequence, VisualContext context)
        {
            if (context.Caster == null) return;

            var casterTransform = context.Caster.transform;

            sequence.InsertCallback(Delay, () =>
            {
                _savedOriginPosition = casterTransform.position;
            });

            // 相对当前位置移动
            sequence.Insert(Delay,
                casterTransform.DOMove(casterTransform.position + RelativeOffset, Duration)
                    .SetEase(EaseType));
        }

        private void AddDashToTarget(Sequence sequence, VisualContext context)
        {
            if (context.Caster == null || context.Target == null) return;

            var casterTransform = context.Caster.transform;

            sequence.InsertCallback(Delay, () =>
            {
                _savedOriginPosition = casterTransform.position;
            });

            // 计算目标前方位置
            Vector3 targetPos = context.Target.transform.position;
            Vector3 direction = (casterTransform.position - targetPos).normalized;
            Vector3 stopPosition = targetPos + direction * StopDistance;

            var moveTween = casterTransform.DOMove(stopPosition, Duration)
                .SetEase(EaseType);

            if (WaitForCompletion)
            {
                sequence.Insert(Delay, moveTween);
            }
            else
            {
                sequence.InsertCallback(Delay, () => moveTween.Play());
            }
        }
#endif

        public override void Execute(VisualContext context)
        {
            if (!Enabled) return;
            // 非 DOTween 模式暂不支持
            Debug.Log($"[MoveEvent] Execute: {MoveType} (requires DOTween)");
        }
    }
}
