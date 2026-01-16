using UnityEngine;

namespace ZGS.Analytics
{
    /// <summary>
    /// 设备信息采集 - 工业级标准
    /// 参考 Firebase Analytics / Unity Analytics
    /// </summary>
    public static class DeviceInfo
    {
        #region Device Properties
        
        /// <summary>匿名设备ID (SHA256哈希)</summary>
        public static string DeviceId => GetHashedDeviceId();
        
        /// <summary>设备型号</summary>
        public static string DeviceModel => SystemInfo.deviceModel;
        
        /// <summary>设备类型 (Desktop/Handheld/Console)</summary>
        public static string DeviceType => SystemInfo.deviceType.ToString();
        
        /// <summary>CPU型号</summary>
        public static string Cpu => SystemInfo.processorType;
        
        /// <summary>CPU核心数</summary>
        public static int CpuCores => SystemInfo.processorCount;
        
        /// <summary>CPU频率 (MHz)</summary>
        public static int CpuFrequency => SystemInfo.processorFrequency;
        
        /// <summary>GPU型号</summary>
        public static string Gpu => SystemInfo.graphicsDeviceName;
        
        /// <summary>GPU厂商</summary>
        public static string GpuVendor => SystemInfo.graphicsDeviceVendor;
        
        /// <summary>显存 (MB)</summary>
        public static int GpuMemory => SystemInfo.graphicsMemorySize;
        
        /// <summary>系统内存 (MB)</summary>
        public static int RamMb => SystemInfo.systemMemorySize;
        
        #endregion
        
        #region OS Properties
        
        /// <summary>平台 (WindowsPlayer/OSXPlayer/LinuxPlayer等)</summary>
        public static string Platform => Application.platform.ToString();
        
        /// <summary>完整操作系统版本</summary>
        public static string Os => SystemInfo.operatingSystem;
        
        /// <summary>操作系统家族</summary>
        public static string OsFamily => SystemInfo.operatingSystemFamily.ToString();
        
        #endregion
        
        #region Display Properties
        
        /// <summary>屏幕宽度</summary>
        public static int ScreenWidth => Screen.width;
        
        /// <summary>屏幕高度</summary>
        public static int ScreenHeight => Screen.height;
        
        /// <summary>屏幕分辨率 (WxH)</summary>
        public static string ScreenResolution => $"{Screen.width}x{Screen.height}";
        
        /// <summary>屏幕DPI</summary>
        public static float ScreenDpi => Screen.dpi;
        
        /// <summary>是否全屏</summary>
        public static bool IsFullscreen => Screen.fullScreen;
        
        /// <summary>画质等级</summary>
        public static int QualityLevel => QualitySettings.GetQualityLevel();
        
        /// <summary>画质名称</summary>
        public static string QualityName => QualitySettings.names[QualitySettings.GetQualityLevel()];
        
        #endregion
        
        #region App Properties
        
        /// <summary>应用版本</summary>
        public static string AppVersion => Application.version;
        
        /// <summary>Unity版本</summary>
        public static string UnityVersion => Application.unityVersion;
        
        /// <summary>构建GUID</summary>
        public static string BuildGuid => Application.buildGUID;
        
        /// <summary>是否编辑器</summary>
        public static bool IsEditor => Application.isEditor;
        
        /// <summary>是否Debug构建</summary>
        public static bool IsDebug => Debug.isDebugBuild;
        
        /// <summary>安装模式</summary>
        public static string InstallMode => Application.installMode.ToString();
        
        #endregion
        
        #region User Properties
        
        /// <summary>系统语言</summary>
        public static string Language => Application.systemLanguage.ToString();
        
        /// <summary>系统区域国家代码 (ISO 3166-1 alpha-2, 如 CN/US/HK)</summary>
        public static string Country => GetCountryCode();
        
        private static string _countryCode;
        private static string GetCountryCode()
        {
            if (!string.IsNullOrEmpty(_countryCode)) return _countryCode;
            try
            {
                _countryCode = System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;
            }
            catch
            {
                _countryCode = "Unknown";
            }
            return _countryCode;
        }
        
        #endregion
        
        #region Helpers
        
        private static string _hashedDeviceId;
        
        private static string GetHashedDeviceId()
        {
            if (!string.IsNullOrEmpty(_hashedDeviceId)) return _hashedDeviceId;
            
            string raw = SystemInfo.deviceUniqueIdentifier;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(raw);
                byte[] hash = sha256.ComputeHash(bytes);
                _hashedDeviceId = System.BitConverter.ToString(hash).Replace("-", "").Substring(0, 16).ToLower();
            }
            return _hashedDeviceId;
        }
        
        /// <summary>
        /// 获取所有设备属性的字典 (用于事件附加)
        /// </summary>
        public static System.Collections.Generic.Dictionary<string, object> ToDictionary()
        {
            return new System.Collections.Generic.Dictionary<string, object>
            {
                // Device
                ["device_id"] = DeviceId,
                ["device_model"] = DeviceModel,
                ["device_type"] = DeviceType,
                ["cpu"] = Cpu,
                ["cpu_cores"] = CpuCores,
                ["cpu_freq"] = CpuFrequency,
                ["gpu"] = Gpu,
                ["gpu_vendor"] = GpuVendor,
                ["gpu_memory"] = GpuMemory,
                ["ram_mb"] = RamMb,
                
                // OS
                ["platform"] = Platform,
                ["os"] = Os,
                ["os_family"] = OsFamily,
                
                // Display
                ["screen_width"] = ScreenWidth,
                ["screen_height"] = ScreenHeight,
                ["screen_dpi"] = ScreenDpi,
                ["is_fullscreen"] = IsFullscreen,
                ["quality_level"] = QualityLevel,
                
                // App
                ["app_version"] = AppVersion,
                ["unity_version"] = UnityVersion,
                ["build_guid"] = BuildGuid,
                ["is_editor"] = IsEditor,
                ["is_debug"] = IsDebug,
                ["install_mode"] = InstallMode,
                
                // User
                ["language"] = Language,
                ["country"] = Country
            };
        }
        
        #endregion
    }
}
