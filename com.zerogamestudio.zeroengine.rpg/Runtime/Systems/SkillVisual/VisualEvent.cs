// ============================================================================
// ZeroEngine v2.7.0 - Skill Visual System
// 视觉事件基类 - 所有事件类型的抽象基类
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
    /// 视觉事件基类 (普通类，非 ScriptableObject)
    /// 子类定义具体的视觉效果 (特效、动画、音效等)
    /// </summary>
    [Serializable]
    public abstract class VisualEvent
    {
#if ODIN_INSPECTOR
        [HorizontalGroup("Header", 0.7f), HideLabel, DisplayAsString(false)]
#endif
        public string DisplayName => GetType().Name.Replace("Event", "");

#if ODIN_INSPECTOR
        [HorizontalGroup("Header", 0.3f), LabelWidth(40), SuffixLabel("s")]
#endif
        [Tooltip("延迟时间 (秒)")]
        public float Delay;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("是否启用此事件")]
        public bool Enabled = true;

        // ========================================
        // 抽象方法
        // ========================================

#if ZEROENGINE_DOTWEEN
        /// <summary>
        /// 将此事件添加到 DOTween 序列中
        /// </summary>
        /// <param name="sequence">DOTween Sequence</param>
        /// <param name="context">执行上下文</param>
        public abstract void AddToSequence(Sequence sequence, VisualContext context);
#endif

        /// <summary>
        /// 立即执行此事件 (不使用 DOTween)
        /// </summary>
        /// <param name="context">执行上下文</param>
        public virtual void Execute(VisualContext context)
        {
            // 默认实现：子类可重写
        }

        // ========================================
        // 辅助方法
        // ========================================

        /// <summary>
        /// 根据 SpawnTarget 获取目标 GameObject
        /// </summary>
        protected GameObject GetSpawnTarget(VisualContext context, SpawnTarget target)
        {
            return target switch
            {
                SpawnTarget.Caster => context.Caster,
                SpawnTarget.Target => context.Target,
                SpawnTarget.TargetPosition => null, // 使用位置
                _ => context.Caster
            };
        }

        /// <summary>
        /// 根据 SpawnTarget 获取位置
        /// </summary>
        protected Vector3 GetSpawnPosition(VisualContext context, SpawnTarget target, Vector3 offset)
        {
            return target switch
            {
                SpawnTarget.Caster => context.GetCasterPosition() + offset,
                SpawnTarget.Target => context.GetTargetPosition() + offset,
                SpawnTarget.TargetPosition => context.TargetPosition.GetValueOrDefault() + offset,
                _ => context.GetCasterPosition() + offset
            };
        }
    }

    /// <summary>
    /// 生成目标类型
    /// </summary>
    public enum SpawnTarget
    {
        /// <summary>施法者位置</summary>
        Caster,
        /// <summary>目标位置</summary>
        Target,
        /// <summary>指定位置</summary>
        TargetPosition
    }
}
