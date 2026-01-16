using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ZeroEngine.Utils
{
    /// <summary>
    /// 条件编译的调试日志工具 (v1.2.0+)
    /// 在非调试构建中，这些方法调用会被完全移除，避免字符串分配
    ///
    /// 使用方法：
    /// 1. 在 Player Settings > Scripting Define Symbols 中添加 ZEROENGINE_DEBUG
    /// 2. 或在 csc.rsp 文件中添加 -define:ZEROENGINE_DEBUG
    /// </summary>
    public static class DebugUtils
    {
        /// <summary>
        /// 条件调试日志 - 仅在定义 ZEROENGINE_DEBUG 时编译
        /// </summary>
        [Conditional("ZEROENGINE_DEBUG")]
        public static void Log(string message)
        {
            Debug.Log(message);
        }

        /// <summary>
        /// 条件调试日志 (带模块前缀)
        /// </summary>
        [Conditional("ZEROENGINE_DEBUG")]
        public static void Log(string module, string message)
        {
            Debug.Log($"[ZeroEngine.{module}] {message}");
        }

        /// <summary>
        /// 条件调试日志 (带上下文对象)
        /// </summary>
        [Conditional("ZEROENGINE_DEBUG")]
        public static void Log(string message, Object context)
        {
            Debug.Log(message, context);
        }

        /// <summary>
        /// 条件调试警告 - 仅在定义 ZEROENGINE_DEBUG 时编译
        /// </summary>
        [Conditional("ZEROENGINE_DEBUG")]
        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        /// <summary>
        /// 条件调试警告 (带模块前缀)
        /// </summary>
        [Conditional("ZEROENGINE_DEBUG")]
        public static void LogWarning(string module, string message)
        {
            Debug.LogWarning($"[ZeroEngine.{module}] {message}");
        }

        /// <summary>
        /// 断言 - 仅在定义 ZEROENGINE_DEBUG 时编译
        /// </summary>
        [Conditional("ZEROENGINE_DEBUG")]
        public static void Assert(bool condition, string message = null)
        {
            if (!condition)
            {
                Debug.LogAssertion(message ?? "Assertion failed");
            }
        }

        /// <summary>
        /// 性能日志 - 用于记录性能相关信息
        /// </summary>
        [Conditional("ZEROENGINE_DEBUG")]
        [Conditional("ZEROENGINE_PROFILING")]
        public static void LogPerf(string message)
        {
            Debug.Log($"[Perf] {message}");
        }
    }
}
