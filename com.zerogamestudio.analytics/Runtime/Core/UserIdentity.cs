using System.Collections.Generic;

namespace ZGS.Analytics
{
    /// <summary>
    /// 用户身份信息 - 支持多平台
    /// </summary>
    public class UserIdentity
    {
        /// <summary>平台标识 (如 "steam", "epic", "zgs", "anonymous")</summary>
        public string Platform { get; set; }
        
        /// <summary>平台用户ID</summary>
        public string PlatformUserId { get; set; }
        
        /// <summary>显示名称/昵称 (可选)</summary>
        public string DisplayName { get; set; }
        
        /// <summary>国家代码 (可选, 如 "CN", "US")</summary>
        public string Country { get; set; }
        
        /// <summary>扩展属性 (如 build_id, is_deck 等)</summary>
        public Dictionary<string, object> CustomProperties { get; set; }

        public UserIdentity()
        {
            CustomProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// 转换为字典 (用于 JSON 序列化)
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                ["platform"] = Platform ?? "anonymous",
                ["uid"] = PlatformUserId ?? ""
            };
            
            if (!string.IsNullOrEmpty(DisplayName))
                dict["name"] = DisplayName;
            
            if (!string.IsNullOrEmpty(Country))
                dict["country"] = Country;
            
            if (CustomProperties != null)
            {
                foreach (var kv in CustomProperties)
                    dict[kv.Key] = kv.Value;
            }
            
            return dict;
        }
    }
}
