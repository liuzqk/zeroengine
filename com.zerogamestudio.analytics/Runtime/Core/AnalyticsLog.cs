using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace ZGS.Analytics
{
    /// <summary>
    /// 条件日志工具 - 仅在编辑器或开发构建中且 debugMode 开启时输出
    /// </summary>
    internal static class AnalyticsLog
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message)
        {
            if (!AnalyticsConfig.DebugMode) return;
            Debug.Log(message);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string message)
        {
            if (!AnalyticsConfig.DebugMode) return;
            Debug.LogWarning(message);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(string message)
        {
            if (!AnalyticsConfig.DebugMode) return;
            Debug.LogError(message);
        }
    }
}
