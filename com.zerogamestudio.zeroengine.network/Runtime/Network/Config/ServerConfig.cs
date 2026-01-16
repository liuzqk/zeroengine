using UnityEngine;

namespace ZeroEngine.Network.Config
{
    public enum ServerEnvironment
    {
        Local,      // 本地编辑器/开发环境
        Production  // 正式环境 (命令行驱动)
    }

    [CreateAssetMenu(fileName = "ServerConfig", menuName = "ZeroEngine/Network/ServerConfig")]
    public class ServerConfig : ScriptableObject
    {
        [Header("Environment")]
        [Tooltip("Local模式下使用配置的IP/端口，Production模式下优先使用命令行参数")]
        public ServerEnvironment Environment = ServerEnvironment.Local;

        [Header("Default Connections")]
        public string DefaultIP = "127.0.0.1";
        public ushort DefaultPort = 7777;
        public int MaxPlayers = 10;
        
        [Header("Performance")]
        public int TargetFrameRate = 60;
        public bool EnableVSync = false;
        
        [Header("Headless")]
        [Tooltip("如果检测到是Batchmode运行，自动降低帧率以节省资源")]
        public bool OptimizeForHeadless = true;
        public int HeadlessTargetFrameRate = 30;
    }
}
