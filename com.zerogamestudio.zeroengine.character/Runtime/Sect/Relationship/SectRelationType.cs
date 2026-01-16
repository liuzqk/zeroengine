// ============================================================================
// SectRelationType.cs
// 门派关系类型
// 创建于: 2026-01-09
// ============================================================================

namespace ZeroEngine.Character.Sect
{
    /// <summary>
    /// 门派关系类型
    /// </summary>
    public enum SectRelationType
    {
        /// <summary>未知/无关系</summary>
        Unknown = 0,

        /// <summary>敌对 (见面就打)</summary>
        Hostile = -2,

        /// <summary>不友好 (有冲突)</summary>
        Unfriendly = -1,

        /// <summary>中立</summary>
        Neutral = 0,

        /// <summary>友好</summary>
        Friendly = 1,

        /// <summary>同盟</summary>
        Allied = 2
    }
}
