using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Network.Core
{
    /// <summary>
    /// 通用的命令行参数解析器
    /// 格式支持: -key value 或 -flag
    /// </summary>
    public static class NetworkArgs
    {
        private static Dictionary<string, string> _args;

        static NetworkArgs()
        {
            ParseArgs();
        }

        private static void ParseArgs()
        {
            _args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var commandLineArgs = System.Environment.GetCommandLineArgs();

            for (int i = 0; i < commandLineArgs.Length; i++)
            {
                var arg = commandLineArgs[i];
                if (arg.StartsWith("-"))
                {
                    var key = arg.Substring(1); // remove '-'
                    var value = "true";

                    // 检查下一个参数是否是值 (不以 - 开头)
                    if (i + 1 < commandLineArgs.Length && !commandLineArgs[i + 1].StartsWith("-"))
                    {
                        value = commandLineArgs[i + 1];
                        i++; // 跳过下一个
                    }
                    
                    if (_args.ContainsKey(key))
                        _args[key] = value;
                    else
                        _args.Add(key, value);
                }
            }
        }

        public static bool HasCheck(string key) => _args.ContainsKey(key);

        public static string GetString(string key, string defaultValue)
        {
            return _args.TryGetValue(key, out var val) ? val : defaultValue;
        }

        public static int GetInt(string key, int defaultValue)
        {
            if (_args.TryGetValue(key, out var val) && int.TryParse(val, out var result))
                return result;
            return defaultValue;
        }

        public static ushort GetUShort(string key, ushort defaultValue)
        {
            if (_args.TryGetValue(key, out var val) && ushort.TryParse(val, out var result))
                return result;
            return defaultValue;
        }
        
        /// <summary>
        /// 打印所有读取到的参数 (用于调试)
        /// </summary>
        public static void LogAllArgs()
        {
            Debug.Log("[NetworkArgs] Parsed Arguments:");
            foreach (var kvp in _args)
            {
                Debug.Log($"  {kvp.Key} = {kvp.Value}");
            }
        }
    }
}
