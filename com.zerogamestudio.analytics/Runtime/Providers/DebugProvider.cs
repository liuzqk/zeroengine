using System.Collections.Generic;
using UnityEngine;

namespace ZGS.Analytics
{
    public class DebugProvider : IAnalyticsProvider
    {
        public void Initialize(string userId)
        {
            Debug.Log($"[DebugProvider] Init for {userId}");
        }

        public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            string paramsStr = "";
            if (parameters != null)
            {
                var list = new List<string>();
                foreach (var kvp in parameters)
                {
                    list.Add($"{kvp.Key}={kvp.Value}");
                }
                paramsStr = string.Join(", ", list);
            }
            Debug.Log($"[DebugProvider] Event: {eventName} | Params: {{{paramsStr}}}");
        }

        public void SetUserProperty(string key, object value)
        {
             Debug.Log($"[DebugProvider] UserProp: {key} = {value}");
        }

        public void TrackScreen(string screenName)
        {
             Debug.Log($"[DebugProvider] Screen: {screenName}");
        }

        public void LogError(string error, string stackTrace)
        {
            Debug.Log($"[DebugProvider] Error: {error}\n{stackTrace}");
        }
    }
}
