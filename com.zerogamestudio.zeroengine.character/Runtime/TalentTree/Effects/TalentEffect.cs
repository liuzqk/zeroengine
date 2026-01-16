using System;
using UnityEngine;

namespace ZeroEngine.TalentTree
{
    /// <summary>
    /// 天赋效果基类
    /// 使用 [SerializeReference] 支持多态序列化
    /// </summary>
    [Serializable]
    public abstract class TalentEffect
    {
        [Tooltip("效果描述")]
        public string Description;

        /// <summary>
        /// 应用效果
        /// </summary>
        /// <param name="owner">效果持有者</param>
        /// <param name="level">天赋等级</param>
        public abstract void Apply(GameObject owner, int level);

        /// <summary>
        /// 移除效果
        /// </summary>
        /// <param name="owner">效果持有者</param>
        public abstract void Remove(GameObject owner);

        /// <summary>
        /// 获取效果描述（支持等级变量）
        /// </summary>
        public virtual string GetDescription(int level)
        {
            return Description;
        }
    }
}
