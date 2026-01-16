using UnityEngine;

namespace ZeroEngine.Utils
{
    /// <summary>
    /// ZeroEngine 统一日志系统 (v1.2.0+)
    /// 提供一致的日志格式 [ZeroEngine.{Module}]
    ///
    /// 与 DebugUtils 的区别：
    /// - ZeroLog: 始终编译，用于生产环境相关的日志
    /// - DebugUtils: 条件编译，非调试构建中完全移除
    /// </summary>
    public static class ZeroLog
    {
        #region Module Constants

        /// <summary>模块名称常量，避免字符串拼写错误</summary>
        public static class Modules
        {
            public const string Core = "Core";
            public const string Stat = "Stat";
            public const string Buff = "Buff";
            public const string Inventory = "Inventory";
            public const string Quest = "Quest";
            public const string Ability = "Ability";
            public const string Dialog = "Dialog";
            public const string Audio = "Audio";
            public const string Save = "Save";
            public const string Network = "Network";
            public const string UI = "UI";
            public const string FSM = "FSM";
            public const string Pool = "Pool";
            public const string Mod = "Mod";
            public const string Input = "Input";
            public const string Localization = "Localization";
            public const string Achievement = "Achievement";
        }

        #endregion

        #region Configuration

        /// <summary>
        /// 全局日志开关。设为 false 可禁用所有 ZeroLog 输出。
        /// 可在运行时动态控制。
        /// </summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>
        /// 最低日志级别。低于此级别的日志将被忽略。
        /// </summary>
        public static LogLevel MinLevel { get; set; } = LogLevel.Info;

        #endregion

        #region Log Levels

        /// <summary>日志级别</summary>
        public enum LogLevel
        {
            /// <summary>调试信息，通常仅在开发时使用</summary>
            Debug = 0,
            /// <summary>普通信息</summary>
            Info = 1,
            /// <summary>警告，不影响功能但需注意</summary>
            Warning = 2,
            /// <summary>错误，影响功能正常运行</summary>
            Error = 3
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// 输出信息日志
        /// </summary>
        /// <param name="module">模块名称 (使用 ZeroLog.Modules 常量)</param>
        /// <param name="message">日志内容</param>
        public static void Info(string module, string message)
        {
            if (!ShouldLog(LogLevel.Info)) return;
            Debug.Log(Format(module, message));
        }

        /// <summary>
        /// 输出信息日志 (带上下文对象)
        /// </summary>
        public static void Info(string module, string message, Object context)
        {
            if (!ShouldLog(LogLevel.Info)) return;
            Debug.Log(Format(module, message), context);
        }

        /// <summary>
        /// 输出警告日志
        /// </summary>
        public static void Warning(string module, string message)
        {
            if (!ShouldLog(LogLevel.Warning)) return;
            Debug.LogWarning(Format(module, message));
        }

        /// <summary>
        /// 输出警告日志 (带上下文对象)
        /// </summary>
        public static void Warning(string module, string message, Object context)
        {
            if (!ShouldLog(LogLevel.Warning)) return;
            Debug.LogWarning(Format(module, message), context);
        }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        public static void Error(string module, string message)
        {
            if (!ShouldLog(LogLevel.Error)) return;
            Debug.LogError(Format(module, message));
        }

        /// <summary>
        /// 输出错误日志 (带上下文对象)
        /// </summary>
        public static void Error(string module, string message, Object context)
        {
            if (!ShouldLog(LogLevel.Error)) return;
            Debug.LogError(Format(module, message), context);
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// 输出带条件的警告日志。仅当条件为 false 时输出。
        /// </summary>
        /// <param name="module">模块名称</param>
        /// <param name="condition">条件，为 false 时输出警告</param>
        /// <param name="message">警告内容</param>
        /// <returns>条件值，便于链式调用</returns>
        public static bool WarnIf(string module, bool condition, string message)
        {
            if (!condition)
            {
                Warning(module, message);
            }
            return condition;
        }

        /// <summary>
        /// 输出带条件的错误日志。仅当条件为 false 时输出。
        /// </summary>
        /// <param name="module">模块名称</param>
        /// <param name="condition">条件，为 false 时输出错误</param>
        /// <param name="message">错误内容</param>
        /// <returns>条件值，便于链式调用</returns>
        public static bool ErrorIf(string module, bool condition, string message)
        {
            if (!condition)
            {
                Error(module, message);
            }
            return condition;
        }

        /// <summary>
        /// 输出异常信息
        /// </summary>
        public static void Exception(string module, System.Exception ex, string context = null)
        {
            if (!Enabled) return;

            string msg = context != null
                ? $"{context}: {ex.Message}"
                : ex.Message;

            Debug.LogError(Format(module, msg));
            Debug.LogException(ex);
        }

        #endregion

        #region Private Helpers

        private static bool ShouldLog(LogLevel level)
        {
            return Enabled && level >= MinLevel;
        }

        private static string Format(string module, string message)
        {
            return $"[ZeroEngine.{module}] {message}";
        }

        #endregion
    }
}