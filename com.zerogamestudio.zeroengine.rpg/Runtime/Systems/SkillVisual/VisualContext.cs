// ============================================================================
// ZeroEngine v2.7.0 - Skill Visual System
// 视觉上下文 - 传递施法者/目标等运行时信息
// ============================================================================

using UnityEngine;

namespace ZeroEngine.RPG.SkillVisual
{
    /// <summary>
    /// 视觉事件执行上下文
    /// 包含施法者、目标等运行时信息
    /// </summary>
    public class VisualContext
    {
        /// <summary>施法者 GameObject</summary>
        public GameObject Caster { get; set; }

        /// <summary>目标 GameObject</summary>
        public GameObject Target { get; set; }

        /// <summary>目标位置 (用于无目标技能)</summary>
        public Vector3? TargetPosition { get; set; }

        /// <summary>技能数据引用 (可选)</summary>
        public object SkillData { get; set; }

        /// <summary>自定义数据</summary>
        public object CustomData { get; set; }

        // ========================================
        // 工厂方法
        // ========================================

        /// <summary>
        /// 创建施法者→目标的上下文
        /// </summary>
        public static VisualContext Create(GameObject caster, GameObject target)
        {
            return new VisualContext
            {
                Caster = caster,
                Target = target
            };
        }

        /// <summary>
        /// 创建施法者→位置的上下文
        /// </summary>
        public static VisualContext Create(GameObject caster, Vector3 targetPosition)
        {
            return new VisualContext
            {
                Caster = caster,
                TargetPosition = targetPosition
            };
        }

        /// <summary>
        /// 创建仅施法者的上下文 (自身 Buff 等)
        /// </summary>
        public static VisualContext CreateSelf(GameObject caster)
        {
            return new VisualContext
            {
                Caster = caster,
                Target = caster
            };
        }

        // ========================================
        // 辅助方法
        // ========================================

        /// <summary>
        /// 获取目标位置 (优先使用 Target.position，否则使用 TargetPosition)
        /// </summary>
        public Vector3 GetTargetPosition()
        {
            if (Target != null)
                return Target.transform.position;
            if (TargetPosition.HasValue)
                return TargetPosition.Value;
            if (Caster != null)
                return Caster.transform.position;
            return Vector3.zero;
        }

        /// <summary>
        /// 获取施法者位置
        /// </summary>
        public Vector3 GetCasterPosition()
        {
            return Caster != null ? Caster.transform.position : Vector3.zero;
        }

        /// <summary>
        /// 获取施法者到目标的方向
        /// </summary>
        public Vector3 GetDirection()
        {
            return (GetTargetPosition() - GetCasterPosition()).normalized;
        }

        /// <summary>
        /// 获取施法者到目标的距离
        /// </summary>
        public float GetDistance()
        {
            return Vector3.Distance(GetCasterPosition(), GetTargetPosition());
        }
    }
}
