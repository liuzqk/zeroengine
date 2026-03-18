// PathfindingLogSettings.cs
// 寻路日志输出开关
// 默认关闭，通过外部命令显式开启

using UnityEngine;

namespace ZeroEngine.Pathfinding2D
{
    /// <summary>
    /// 寻路日志输出开关
    /// </summary>
    public static class PathfindingLogSettings
    {
        /// <summary>启用基础生成摘要日志</summary>
        public static bool EnableGenerationSummary { get; private set; }

        /// <summary>启用详细诊断日志（包含边缘节点列表等）</summary>
        public static bool EnableDetailedDiagnostics { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            EnableGenerationSummary = false;
            EnableDetailedDiagnostics = false;
        }

        /// <summary>
        /// 设置日志开关
        /// </summary>
        public static void SetLogging(bool enableSummary, bool enableDetailedDiagnostics = false)
        {
            EnableGenerationSummary = enableSummary;
            EnableDetailedDiagnostics = enableSummary && enableDetailedDiagnostics;
        }

        /// <summary>
        /// 获取当前日志状态描述
        /// </summary>
        public static string GetStatusSummary()
        {
            return $"Pathfinding logs => summary={(EnableGenerationSummary ? "ON" : "OFF")}, verbose={(EnableDetailedDiagnostics ? "ON" : "OFF")}";
        }
    }
}
