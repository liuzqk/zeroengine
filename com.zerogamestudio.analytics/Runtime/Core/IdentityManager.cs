using System.Collections.Generic;
using UnityEngine;

namespace ZGS.Analytics
{
    /// <summary>
    /// 身份管理器 - 管理多平台用户身份
    /// </summary>
    public static class IdentityManager
    {
        private static readonly Dictionary<string, UserIdentity> _identities = new();
        
        /// <summary>
        /// 设置/更新平台身份
        /// </summary>
        public static void SetIdentity(UserIdentity identity)
        {
            if (identity == null || string.IsNullOrEmpty(identity.Platform))
            {
                AnalyticsLog.LogWarning("[ZGS.Analytics] SetIdentity: identity or platform is null");
                return;
            }
            
            _identities[identity.Platform] = identity;
            AnalyticsLog.Log($"[ZGS.Analytics] Identity set: {identity.Platform}={identity.PlatformUserId}");
        }
        
        /// <summary>
        /// 获取指定平台的身份
        /// </summary>
        public static UserIdentity GetIdentity(string platform)
        {
            return _identities.TryGetValue(platform, out var identity) ? identity : null;
        }
        
        /// <summary>
        /// 获取所有已设置的身份
        /// </summary>
        public static IEnumerable<UserIdentity> GetAllIdentities()
        {
            return _identities.Values;
        }
        
        /// <summary>
        /// 检查是否有任何平台身份
        /// </summary>
        public static bool HasAnyIdentity => _identities.Count > 0;
        
        /// <summary>
        /// 清除所有身份
        /// </summary>
        public static void ClearAll()
        {
            _identities.Clear();
        }
        
        /// <summary>
        /// 将所有身份转换为列表 (用于事件附加)
        /// </summary>
        public static List<Dictionary<string, object>> ToList()
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var identity in _identities.Values)
                list.Add(identity.ToDictionary());
            return list;
        }
    }
}
