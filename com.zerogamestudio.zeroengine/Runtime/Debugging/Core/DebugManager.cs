using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Performance.Collections;

namespace ZeroEngine.Debugging
{
    /// <summary>
    /// 调试管理器 - 运行时调试系统入口
    /// </summary>
    public static class DebugManager
    {
        private static readonly Dictionary<string, IDebugModule> _modules = new Dictionary<string, IDebugModule>();
        private static bool _isEnabled = false;
        private static float _updateInterval = 0.1f;
        private static float _lastUpdateTime;

        /// <summary>调试系统是否启用</summary>
        public static bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnEnabledChanged?.Invoke(value);
                }
            }
        }

        /// <summary>更新间隔（秒）</summary>
        public static float UpdateInterval
        {
            get => _updateInterval;
            set => _updateInterval = Mathf.Max(0.016f, value);
        }

        /// <summary>已注册的模块数量</summary>
        public static int ModuleCount => _modules.Count;

        /// <summary>调试系统启用状态变化事件</summary>
        public static event Action<bool> OnEnabledChanged;

        /// <summary>模块数据更新事件</summary>
        public static event Action<IDebugModule> OnModuleUpdated;

        /// <summary>
        /// 注册调试模块
        /// </summary>
        public static void RegisterModule(IDebugModule module)
        {
            if (module == null) return;

            if (!_modules.ContainsKey(module.ModuleName))
            {
                _modules.Add(module.ModuleName, module);
                Debug.Log($"[ZeroEngine.Debugging] Module registered: {module.ModuleName}");
            }
            else
            {
                Debug.LogWarning($"[ZeroEngine.Debugging] Module already registered: {module.ModuleName}");
            }
        }

        /// <summary>
        /// 注销调试模块
        /// </summary>
        public static void UnregisterModule(string moduleName)
        {
            if (_modules.Remove(moduleName))
            {
                Debug.Log($"[ZeroEngine.Debugging] Module unregistered: {moduleName}");
            }
        }

        /// <summary>
        /// 获取调试模块
        /// </summary>
        public static IDebugModule GetModule(string moduleName)
        {
            _modules.TryGetValue(moduleName, out var module);
            return module;
        }

        /// <summary>
        /// 获取调试模块（泛型版本）
        /// </summary>
        public static T GetModule<T>(string moduleName) where T : class, IDebugModule
        {
            return GetModule(moduleName) as T;
        }

        /// <summary>
        /// 获取所有已注册模块
        /// </summary>
        public static IEnumerable<IDebugModule> GetAllModules()
        {
            return _modules.Values;
        }

        /// <summary>
        /// 更新所有模块（在游戏循环中调用）
        /// </summary>
        public static void Update()
        {
            if (!_isEnabled) return;

            float now = Time.unscaledTime;
            if (now - _lastUpdateTime < _updateInterval) return;
            _lastUpdateTime = now;

            foreach (var module in _modules.Values)
            {
                if (module.IsEnabled)
                {
                    module.Update();
                    OnModuleUpdated?.Invoke(module);
                }
            }
        }

        /// <summary>
        /// 获取所有模块的摘要信息
        /// </summary>
        public static string GetAllSummaries()
        {
            var sb = StringBuilderPool.Get();
            try
            {
                sb.AppendLine("=== Debug Summary ===");
                foreach (var module in _modules.Values)
                {
                    if (module.IsEnabled)
                    {
                        sb.AppendLine($"[{module.ModuleName}]");
                        sb.AppendLine(module.GetSummary());
                        sb.AppendLine();
                    }
                }
                return sb.ToString();
            }
            finally
            {
                StringBuilderPool.Return(sb);
            }
        }

        /// <summary>
        /// 清空所有模块数据
        /// </summary>
        public static void ClearAll()
        {
            foreach (var module in _modules.Values)
            {
                module.Clear();
            }
        }

        /// <summary>
        /// 重置调试系统
        /// </summary>
        public static void Reset()
        {
            _modules.Clear();
            _isEnabled = false;
            OnEnabledChanged = null;
            OnModuleUpdated = null;
        }
    }
}
